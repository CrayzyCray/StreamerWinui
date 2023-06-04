extern crate ffmpeg;
use std::ptr::{null, null_mut};

use ffmpeg::*;

#[no_mangle]
pub unsafe extern fn get_peak(array:*mut f32, length:i32) -> f32 {
    let mut peak: f32 = 0.0;
    let array_ptr = array as usize;
    let mut i: usize = 0;

    while i < length as usize {
        let mut sample = *((array_ptr + i * 4) as *const f32);
        if sample < 0.0 {sample = -sample}
        if sample > peak {peak = sample}
        i += 1;
    }
    return 20.0 * peak.log10();
}

#[no_mangle]
pub unsafe extern fn get_peak_multichannel(array:*mut f32, length:i32, channels: i32, channel_index: i32) -> f32 {
    let mut peak: f32 = 0.0;
    let array_ptr = array as usize;
    let mut i: usize = channel_index as usize;
    while i < length as usize {
        let mut sample = *((array_ptr + i * 4) as *const f32);
        if sample < 0.0 {sample = -sample}
        if sample > peak {peak = sample}
        i += channels as usize;
    }
    return 20.0 * peak.log10();
}

#[no_mangle]
pub unsafe extern fn apply_volume(array_ptr:*mut f32, length:i32, volume: f32) {
    if volume == 1.0 {return}
    let array = std::slice::from_raw_parts_mut(array_ptr, length as usize);
    for sample in array.iter_mut() {
        *sample *= volume;
    }
}

#[no_mangle]
pub unsafe extern fn audio_encoder_constructor(name: *const i8, channels: i32, required_sample_rate: i32) -> EncoderParameters {
    let codec = avcodec_find_encoder_by_name(name);
    let codec_context = avcodec_alloc_context3(codec);

    (*codec_context).sample_rate = required_sample_rate;
    (*codec_context).sample_fmt = AVSampleFormat_AV_SAMPLE_FMT_FLT;

    av_channel_layout_default(&mut (*codec_context).ch_layout, channels); // as *mut AVChannelLayout
    avcodec_open2(codec_context, codec, std::ptr::null_mut());

    let codec_parameters = avcodec_parameters_alloc();

    avcodec_parameters_from_context(codec_parameters, codec_context);

    let frame_size_in_samples = (*codec_context).frame_size;
    let frame = av_frame_alloc();
    (*frame).nb_samples = frame_size_in_samples;
    av_channel_layout_default(&mut (*frame).ch_layout, channels); // as *mut AVChannelLayout
    
    (*frame).format = AVSampleFormat_AV_SAMPLE_FMT_FLT;
    let packet = av_packet_alloc();

    return EncoderParameters {
        sample_rate: (*codec_context).sample_rate,
        channels,
        frame_size_in_samples,
        frame_size_in_bytes: frame_size_in_samples * channels * 4,
        codec_context,
        packet,
        time_base: &mut (*codec_context).time_base as *mut AVRational,
        codec_parameters,
        frame,
    }
}

#[no_mangle]
pub unsafe extern fn audio_encoder_encode_buffer(parameters: EncoderParameters, buffer: *const u8, pts: i64, stream_index: i32) -> i32 {
    av_packet_unref(parameters.packet);
    (*parameters.frame).pts = pts;
    let mut ret = avcodec_fill_audio_frame(parameters.frame, parameters.channels, AVSampleFormat_AV_SAMPLE_FMT_FLT, buffer, parameters.frame_size_in_bytes, parameters.channels * 4);
    if ret < 0 {return ret};
    ret = avcodec_send_frame(parameters.codec_context, parameters.frame);
    if ret < 0 {return ret};
    ret = avcodec_receive_packet(parameters.codec_context, parameters.packet);
    if ret == 0 {
        (*parameters.packet).stream_index = stream_index};
    return ret;
}

#[no_mangle]
pub unsafe extern fn stream_writer_close_format_context(format_context: *mut AVFormatContext) {
    av_write_trailer(format_context);
    avio_closep(&mut (*format_context).pb);
    avformat_free_context(format_context);
}

#[no_mangle]
pub unsafe extern fn audio_encoder_dispose(parameters: *mut EncoderParameters) {
    av_packet_unref((*parameters).packet);
    avcodec_send_packet((*parameters).codec_context, (*parameters).packet); //flush codec_context
    avcodec_free_context(&mut (*parameters).codec_context);
    av_packet_free(&mut (*parameters).packet);
    avcodec_parameters_free(&mut (*parameters).codec_parameters)
}

#[no_mangle]
pub unsafe extern fn stream_writer_add_client(output_url: *const i8, stream_parameters_array: *mut StreamParameters, array_length: i32) -> *mut AVFormatContext {
    #[allow(temporary_cstring_as_ptr)]
    let format_name = std::ffi::CString::new("mpegts").unwrap().as_ptr();
    let mut format_context: *mut AVFormatContext = null_mut();
    avformat_alloc_output_context2(&mut format_context, null(), format_name, null());

    let array = std::slice::from_raw_parts_mut(stream_parameters_array, array_length as usize);//

    for stream_parameter in array.iter() {
        let codec = avcodec_find_encoder((*(*stream_parameter).codec_parameters).codec_id);
        let stream = avformat_new_stream(format_context, codec);
        avcodec_parameters_copy((*stream).codecpar, stream_parameter.codec_parameters);
        (*stream).time_base = (*stream_parameter.time_base).clone();
    }
    
    avio_open(&mut (*format_context).pb, output_url, AVIO_FLAG_WRITE as i32);
    avformat_write_header(format_context, null_mut());

    let mut i = 0;
    while i < array_length as usize {
        let stream = *(*format_context).streams.offset(i as isize);
        array[i].time_base = &mut (*stream).time_base;
        i += 1;
    }
    
    return format_context;
}

#[no_mangle]
pub unsafe extern fn stream_writer_add_client_as_file(path: *const i8, stream_parameters_array: *mut StreamParameters, array_length: i32) -> *mut AVFormatContext {
    let mut format_context: *mut AVFormatContext = null_mut();
    avformat_alloc_output_context2(&mut format_context, null(), null(), path);

    let array = std::slice::from_raw_parts_mut(stream_parameters_array, array_length as usize);//

    for stream_parameter in array.iter() {
        let codec = avcodec_find_encoder((*(*stream_parameter).codec_parameters).codec_id);
        let stream = avformat_new_stream(format_context, codec);
        avcodec_parameters_copy((*stream).codecpar, stream_parameter.codec_parameters);
        (*stream).time_base = (*stream_parameter.time_base).clone();
    }
    
    avio_open(&mut (*format_context).pb, path, AVIO_FLAG_WRITE as i32);
    avformat_write_header(format_context, null_mut());

    let mut i = 0;
    while i < array_length as usize {
        let stream = *(*format_context).streams.offset(i as isize);
        array[i].time_base = &mut (*stream).time_base;
        i += 1;
    }
    
    return format_context;
}

#[no_mangle]
pub unsafe extern fn stream_writer_write_packet(packet: *mut AVPacket, time_base_packet: *const AVRational, stream_clients_array_ptr: *mut StreamClient, array_length: i32) -> i32 {
    let array = std::slice::from_raw_parts_mut(stream_clients_array_ptr, array_length as usize);
    let stream = **(*array[0].format_context).streams.offset((*packet).stream_index as isize);
    av_packet_rescale_ts(packet, *time_base_packet, stream.time_base);
    let mut ret: i32 = 0;
    for client in array {
        ret = av_write_frame(client.format_context, packet);
    }
    return ret;
}

#[repr(C)]
pub struct EncoderParameters{
    sample_rate: i32,
    channels: i32,
    frame_size_in_samples: i32,
    frame_size_in_bytes: i32,
    codec_context: *mut AVCodecContext,
    packet: *mut AVPacket,
    time_base: *mut AVRational,
    codec_parameters: *mut AVCodecParameters,
    frame: *mut AVFrame,
}

#[repr(C)]
pub struct StreamClient{
    //ip: String,
    port: i32,
    format_context: *mut AVFormatContext,
    is_file: bool,
}

#[repr(C)]
pub struct StreamParameters{
    codec_parameters: *mut AVCodecParameters,
    time_base: *mut AVRational,
}

#[test]
fn test_audio_encoder_constructor(){
    unsafe{
        let name = std::ffi::CString::new("libopus").unwrap();
        let params = audio_encoder_constructor(name.as_ptr(), 2, 48000);
        let s = (*(*params.codec_context).codec).long_name;
        let str = std::ffi::CStr::from_ptr(s);
        println!("{:?}", str);
    }
}