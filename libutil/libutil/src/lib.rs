extern crate ffmpeg;
use std::io::{Write, Bytes};
use std::ptr::null;
use std::slice;
use std::pin::Pin;
use std::time::Duration;
mod stream_writer;
mod audio_encoder;
mod stream_controller;
mod master_channel;
mod audio_capturing_channel;
mod audio_frame;

use master_channel::MasterChannel;
use audio_capturing_channel::AudioCapturingChannel;

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
    
    let mut i = channel_index as usize;
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

// #[no_mangle]
// pub extern fn start_record_test() -> Pin<Box<stream_controller::StreamController>> {
//     let mut stream_controller = stream_controller::StreamController::new();
//     stream_controller.start_streaming();
//     return Pin::new(Box::new(stream_controller));
// }

#[no_mangle]
#[test]
pub extern fn start_master_channel_test(){
    let is_loopback = true;
    let mut master_channel = MasterChannel::new();
    let device = MasterChannel::get_default_device(is_loopback).unwrap();
    master_channel.add_device(device, is_loopback);
    //println!("{:?}", mas);
    master_channel.start_streaming();

    std::thread::sleep(Duration::from_secs(5));

    master_channel.stop();
}

#[no_mangle]
#[test]
pub extern fn start_acc_test(){
    let is_loopback = true;
    let device = MasterChannel::get_default_device(is_loopback).unwrap();
    let mut acc = AudioCapturingChannel::new(device, true, 4);
    println!("{:?}", acc.device_name());

    let path = std::path::Path::new("C:\\Users\\Cray\\Desktop\\St\\rec.raw");
    let mut file = std::fs::File::create(path).unwrap();

    match acc.start() {
        Ok(_) => (),
        Err(e) => panic!("{}", e),
    }
    let t = std::time::SystemTime::now();
    let mut counter = 0;
    loop {
        let audio_frame = match acc.read_next_frame() {
            Ok(data) => data,
            Err(_) => {
                println!("read_next_buffer error"); 
                break;},
        };
        write_to_file(audio_frame.data(), &mut file);
        counter += 1;
        //if counter > (48000 / 960) * 2 {break;}
        if t.elapsed().unwrap() >= Duration::from_secs(4){
            break;}
    }
    acc.stop().unwrap();
    fn write_to_file(buf: &Vec<f32>, file: &mut std::fs::File){
        let buffer;
        unsafe{
            buffer = std::slice::from_raw_parts(buf.as_ptr() as *const u8, buf.len() * 4)
        }
        file.write(buffer);
    }
}

// #[no_mangle]
// pub extern fn stop_record_test(mut stream_controller: Pin<Box<stream_controller::StreamController>>) {
//     stream_controller.stop_streaming();
// }

#[test]
fn audio_encoder_test(){
    let audio_codec = audio_encoder::AudioCodecs::libopus;
    let stream_writer = stream_writer::StreamWriter::new();
    let channels = 2;
    let required_sample_rate = 48000;
    let encoder = audio_encoder::AudioEncoder::new(audio_codec, stream_writer, channels, required_sample_rate);
    println!("frame_size_in_bytes: {:?}", encoder.frame_size_in_bytes());
}