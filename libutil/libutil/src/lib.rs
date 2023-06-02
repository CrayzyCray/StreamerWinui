extern crate ffmpeg;

use ffmpeg::*;

#[no_mangle]
pub unsafe extern fn test(){
    let a = ffmpeg::av_frame_alloc();
    println!("av_frame_alloc() = {:?}", a);
}

#[no_mangle]
pub unsafe extern fn get_peak(array:*mut f32, length:i32) -> f32 {
    let mut peak: f32 = 0.0;
    let array_ptr = array as usize;
    let mut i: usize = 0;
    while i < length as usize{
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
    while i < length as usize{
        let mut sample = *((array_ptr + i * 4) as *const f32);
        if sample < 0.0 {sample = -sample}
        if sample > peak {peak = sample}
        i += channels as usize;
    }
    return 20.0 * peak.log10();
}

#[no_mangle]
pub unsafe extern fn apply_volume(array:*mut f32, length:i32, volume: f32) {
    if volume > 1.0 {return}
    let array_ptr = array as usize;
    let mut i: usize = 0;
    if volume > 0.0{
        while i < length as usize{
            *((array_ptr + i * 4) as *mut f32) *= volume;
            i += 1;
        }
    }else {
        while i < length as usize{
            *((array_ptr + i * 4) as *mut f32) = 0.0;
            i += 1;
        }
    }
}

#[no_mangle]
pub unsafe extern fn audio_encoder_constructor(name: *const i8, channels: i32, required_sample_rate: i32) -> EncoderParameters {
    let codec = avcodec_find_encoder_by_name(name);
    let codec_context = avcodec_alloc_context3(codec);

    (*codec_context).sample_rate = required_sample_rate;
    (*codec_context).sample_fmt = AVSampleFormat_AV_SAMPLE_FMT_FLT;

    av_channel_layout_default(&mut (*codec_context).ch_layout as *mut AVChannelLayout, channels);
    avcodec_open2(codec_context, codec, std::ptr::null_mut());

    let codec_parameters = avcodec_parameters_alloc();

    avcodec_parameters_from_context(codec_parameters, codec_context);

    let frame_size_in_samples = (*codec_context).frame_size;
    let frame = av_frame_alloc();
    (*frame).nb_samples = frame_size_in_samples;
    av_channel_layout_default(&mut (*frame).ch_layout as *mut AVChannelLayout, channels);
    
    (*frame).format = AVSampleFormat_AV_SAMPLE_FMT_FLT;
    let packet = av_packet_alloc();

    return EncoderParameters {
        sample_rate: (*codec_context).sample_rate,
        channels,
        frame_size_in_samples,
        codec_context,
        packet,
        time_base: &mut (*codec_context).time_base as *mut AVRational,
        codec_parameters,
        frame,
    }
}

#[repr(C)]
pub struct EncoderParameters
{
    sample_rate: i32,
    channels: i32,
    frame_size_in_samples: i32,
    codec_context: *mut AVCodecContext,
    packet: *mut AVPacket,
    time_base: *mut AVRational,
    codec_parameters: *mut AVCodecParameters,
    frame: *mut AVFrame,
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