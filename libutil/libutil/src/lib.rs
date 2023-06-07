extern crate ffmpeg;
use std::slice;
use std::pin::Pin;
mod stream_writer;
mod audio_encoder;
mod stream_controller;
mod master_channel;
mod audio_capturing_channel;

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
pub extern fn get_peak_multichannel(array_ptr:*const f32, length:i32, channels: i32, channel_index: i32) -> f32 {
    let mut peak: f32 = 0.0;
    let array = unsafe{slice::from_raw_parts(array_ptr, length as usize)};
    
    let mut i: usize = channel_index as usize;
    while i < length as usize {
        let mut sample = array[i];
        if sample < 0.0 {sample = -sample}
        if sample > peak {peak = sample}
        i += channels as usize;
    }
    return 20.0 * peak.log10();
}

#[no_mangle]
pub extern fn apply_volume(array_ptr:*mut f32, length:i32, volume: f32) {
    if volume == 1.0 {return}
    let array = unsafe{slice::from_raw_parts_mut(array_ptr, length as usize)};
    for sample in array.iter_mut() {
        *sample *= volume;
    }
}

#[no_mangle]
pub extern fn stream_writer_add_client(stream_writer: *mut stream_writer::StreamWriter, ip: [u8; 4], port: u16) {
    if stream_writer.is_null() {
        return;
    }
    unsafe{
        (*stream_writer).add_client(std::net::Ipv4Addr::from(ip), port);
    }
}

#[no_mangle]
pub extern fn stream_writer_add_client_as_file(stream_writer: *mut stream_writer::StreamWriter, path_ptr: *const i8) {
    if stream_writer.is_null() || path_ptr.is_null() {
        return;
    }
    unsafe{
        let path = std::ffi::CStr::from_ptr(path_ptr).to_str().unwrap();
        (*stream_writer).add_client_as_file(path);
    }
}

#[no_mangle]
pub extern fn start_record_test() -> Pin<Box<stream_controller::StreamController>> {
    let mut stream_controller = stream_controller::StreamController::new();
    stream_controller.start_streaming();
    return Pin::new(Box::new(stream_controller));
}

#[no_mangle]
pub extern fn stop_record_test(mut stream_controller: Pin<Box<stream_controller::StreamController>>) {
    stream_controller.stop_streaming();
}

#[test]
fn audio_encoder_test(){
    let audio_codec = audio_encoder::AudioCodecs::libopus;
    let stream_writer = stream_writer::StreamWriter::new();
    let channels = 2;
    let required_sample_rate = 48000;
    let encoder = audio_encoder::AudioEncoder::new(audio_codec, stream_writer, channels, required_sample_rate);
    println!("frame_size_in_bytes: {:?}", encoder.frame_size_in_bytes());
}