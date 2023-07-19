#![allow(dead_code)]
use crate::audio_packet::AudioPacket;
use crate::QPCTime;
use crate::stream_writer::*;
use crate::audio_capturing_channel::*;
use std::*;
use std::sync::Arc;
use std::sync::Mutex;
use windows::Win32::Media::Audio::*;
use crate::WaveFormat;

pub struct MasterChannel {
    stream_writer: Arc<StreamWriter>,
    is_capturing: bool,
    audio_channels: Vec<AudioCapturingChannel>,
    master_buffer: Vec<u8>,
    last_frame_time: QPCTime,
    packet_size: u16,
    wave_format: WaveFormat,
}

impl MasterChannel {
    pub fn new(packet_size: u16, wave_format: WaveFormat) -> Self{
        unsafe{
            use windows::Win32::System::Com::*;
            CoInitializeEx(None, COINIT_MULTITHREADED).unwrap();
        }
        Self{
            stream_writer: Arc::new(StreamWriter::new()),
            is_capturing: false,
            audio_channels: vec![],
            master_buffer: Vec::new(),
            last_frame_time: QPCTime(0),
            packet_size,
            wave_format,
        }
    }

    pub fn available_devices(&self, direction: EDataFlow) -> Result<Vec<IMMDevice>, ()>{
        use windows::Win32::System::Com::*;
        unsafe{
            let enumerator = CoCreateInstance(&MMDeviceEnumerator, None, CLSCTX_ALL);
            if enumerator.is_err() {return Err(());}
            let enumerator: IMMDeviceEnumerator = enumerator.unwrap();

            let collection = enumerator.EnumAudioEndpoints(direction, DEVICE_STATE_ACTIVE);
            if collection.is_err() {return Err(());}
            let collection = collection.unwrap();

            let count = collection.GetCount().unwrap();
            let mut devices = Vec::with_capacity(count as usize);
            for i in 0..count{
                devices.push(collection.Item(i).unwrap());
            }
            return Ok(devices);
        }

    }
    
    pub fn add_device(&mut self, device: IMMDevice, data_flow: EDataFlow) -> Result<(), ()>{
        let mut channel = AudioCapturingChannel::new(device, data_flow);
        if self.is_capturing { channel.start().unwrap() }
        self.audio_channels.push(channel);
        Ok(())
    }

    pub fn read(&mut self) -> Result<AudioPacket, ()> {
        if !self.is_capturing { return Err(()) }

        //let buffer = vec![0u8; self.wave_format.bytes_per_frame() as usize];
        for channel in self.audio_channels.iter_mut() {
            channel.read2();
        }
        todo!();
    }

    pub fn start(&mut self) -> Result<(), ()>{
        if self.is_capturing { return Err(());}
        for channel in self.audio_channels.iter_mut() {
            channel.start().unwrap()
        }
        Ok(())
    }

    pub fn stop(&mut self) -> Result<(), ()>{
        if !self.is_capturing { return Err(());}
        for channel in self.audio_channels.iter_mut() {
            channel.stop().unwrap()
        }
        Ok(())
    }

    pub fn get_default_device(&self, dataflow: EDataFlow, role: ERole) -> Result<IMMDevice, ()>{
        use windows::Win32::System::Com::*;
        unsafe{
            let enumerator = CoCreateInstance(&MMDeviceEnumerator, None, CLSCTX_ALL);
            if enumerator.is_err() {return Err(());}
            let enumerator: IMMDeviceEnumerator = enumerator.unwrap();

            let device = enumerator.GetDefaultAudioEndpoint(dataflow, role);
            if device.is_err() {return Err(());}
            let device = device.unwrap();
            return Ok(device);
        }
    }
}

pub enum MasterChanelStates{
    Stopped,
    Monitoring,
    Streaming,
}

// fn mixer_loop(audio_channels: Arc<Mutex<Vec<AudioCapturingChannel>>>, stream_writer: &Arc<StreamWriter>, frame_size: usize){
//     let mut master_buffer: Vec<f32>;
//     let mut stop_flag = false;

//     loop {
//         let audio_channels = audio_channels.lock();

//         if audio_channels.is_err(){
//             continue;
//         }

//         let audio_channels = audio_channels.unwrap();

//         for channel in audio_channels.iter() {
//             if !channel.is_capturing() {
//                 continue;
//             }

//             let buffer = channel.read();
//             // if buffer.is_err() {
//             //     continue;
//             // }
//             // let buffer = buffer.unwrap();
            
//         }
//     }
// }