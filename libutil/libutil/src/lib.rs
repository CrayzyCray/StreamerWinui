#![allow(dead_code)]
extern crate ffmpeg;
use std::*;
use slice;
use pin::Pin;
use time::{Duration, Instant};
mod stream_writer;
mod audio_encoder;
mod stream_controller;
mod master_channel;
mod audio_capturing_channel;
mod audio_frame;
mod recorder;

use master_channel::MasterChannel;
use audio_capturing_channel::AudioCapturingChannel;
use windows::Win32::Media::Audio::{eRender, eMultimedia, eCommunications};

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
#[test]
fn audio_capturing_channel_test() {
    let master_channel = MasterChannel::new();
    let dev1 = master_channel.get_default_device(eRender, eMultimedia).unwrap();
    //let dev2 = master_channel.get_default_device(eRender, eCommunications).unwrap();
    let mut channels = vec![];
    channels.push(AudioCapturingChannel::new(dev1, eRender));
    //channels.push(AudioCapturingChannel::new(dev2, eRender));
    let buffer_duration = Duration::from_nanos(channels[0].buffer_duration() as u64);
    for ele in channels.iter_mut() {
        ele.start().unwrap();
        println!("{} latancy: {}", ele.device_name(), Duration::from_nanos(ele.stream_latency() as u64 * 100).as_secs_f32());
    }
    
    let start_time = Instant::now();
    loop {
        std::thread::sleep(buffer_duration / 2);
        if start_time.elapsed() > Duration::from_secs(6) {
            break;
        }
        for chn in channels.iter_mut() {
            loop {
                match chn.read() {
                    Some(audio_frame) => {
                        println!("bytes: {}, time: {}, st: {}", audio_frame.data.len(), audio_frame.time.as_secs_f32(), start_time.elapsed().as_secs_f32());
                    },
                    None => break,
                }
            }
        }
    }

    for ele in channels.iter_mut() {
        ele.stop().unwrap();
    }
}

#[test]
fn performance_time() {
    let start_time = Instant::now();
    let mut time_qpc = 0;
    let mut time_instant: Duration;
    for i in 0..10_000_000 {
        //unsafe{windows::Win32::System::Performance::QueryPerformanceCounter(&mut time_qpc);}
        time_instant = start_time.elapsed();
    }
    println!("{}", start_time.elapsed().as_secs_f32());
}

#[test]
fn audio_capturing_channel_test2() {
    let mut master_channel = MasterChannel::new();
    let dev1 = master_channel.get_default_device(eRender, eMultimedia).unwrap();
    let dev2 = master_channel.get_default_device(eRender, eCommunications).unwrap();
    master_channel.add_device(dev1, eRender).unwrap();
    //master_channel.add_device(dev2, eRender);
    master_channel.start().unwrap();
    thread::sleep(Duration::from_secs(6));
    master_channel.stop().unwrap();
}

// #[no_mangle]
// #[test]
// pub extern fn start_acc_test() {
//     let is_loopback = true;
//     let device = MasterChannel::get_default_device(is_loopback).unwrap();
//     let mut acc = AudioCapturingChannel::new(device, true, 4);
//     println!("{:?}", acc.device_name());

//     let path = std::path::Path::new("C:\\Users\\Cray\\Desktop\\St\\rec.raw");
//     let mut file = std::fs::File::create(path).unwrap();

//     match acc.start() {
//         Ok(_) => (),
//         Err(e) => panic!("{}", e),
//     }
//     let t = std::time::SystemTime::now();
//     let mut counter = 0;
//     loop {
//         let audio_frame = match acc.read_next_frame() {
//             Ok(data) => data,
//             Err(_) => {
//                 println!("read_next_buffer error"); 
//                 break;},
//         };
//         write_to_file(audio_frame.data(), &mut file);
//         counter += 1;
//         //if counter > (48000 / 960) * 2 {break;}
//         if t.elapsed().unwrap() >= Duration::from_secs(4){
//             break;}
//     }
//     acc.stop().unwrap();
//     fn write_to_file(buf: &Vec<f32>, file: &mut std::fs::File){
//         let buffer;
//         unsafe{
//             buffer = std::slice::from_raw_parts(buf.as_ptr() as *const u8, buf.len() * 4)
//         }
//         file.write(buffer);
//     }
// }

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