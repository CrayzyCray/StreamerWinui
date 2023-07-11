#![allow(dead_code)]
use crate::audio_frame::AudioFrame;
use crate::QPCTime;
use crate::stream_writer::*;
use crate::audio_capturing_channel::*;
use std::*;
use std::sync::Arc;
use std::sync::Mutex;
use windows::Win32::Media::Audio::*;

pub struct MasterChannel{
    stream_writer: Arc<StreamWriter>,
    state: MasterChanelStates,
    audio_channels: Arc<Mutex<Vec<AudioCapturingChannel>>>,
    master_buffer: Vec<f32>,
    last_frame_time: QPCTime,
}

impl MasterChannel{
    pub fn new() -> Self{
        unsafe{
            use windows::Win32::System::Com::*;
            CoInitializeEx(None, COINIT_MULTITHREADED).unwrap();
        }
        Self{
            stream_writer: Arc::new(StreamWriter::new()),
            state: MasterChanelStates::Stopped,
            audio_channels: Arc::new(Mutex::new(vec![])),
            master_buffer: Vec::new(),
            last_frame_time: QPCTime(0),
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
    
    pub fn add_device(&mut self, device: IMMDevice, direction: EDataFlow) -> Result<(), ()>{
        match self.state {
            MasterChanelStates::Streaming => return Err(()),
            _ => {
                let audio_channels = self.audio_channels.lock();
                if audio_channels.is_err(){
                    return Err(());
                }
                let mut channel = AudioCapturingChannel::new(device, direction);
                channel.start().unwrap();
                audio_channels.unwrap().push(channel);
                return Ok(());
            }
        }
        
    }

    pub fn read(&self) -> Result<AudioFrame, ()> {
        match self.state {
            MasterChanelStates::Stopped => return Err(()),
            _ => (),
        };
        todo!();
    }

    pub fn start(&mut self) -> Result<(), ()>{
        match self.state {
            MasterChanelStates::Stopped => return start(self),
            _ => return Err(()),
        };
        fn start(s: &mut MasterChannel) -> Result<(), ()>{
            let channels = s.audio_channels.lock();
            if channels.is_err() {
                return Err(());
            }
            for channel in channels.unwrap().iter_mut() {
                channel.start().unwrap();}
            Ok(())
        }
    }

    pub fn stop(&mut self) -> Result<(), ()>{
        match self.state {
            MasterChanelStates::Streaming => return start(self),
            _ => return Err(()),
        };
        fn start(s: &mut MasterChannel) -> Result<(), ()>{
            let channels = s.audio_channels.lock();
            if channels.is_err() {
                return Err(());
            }
            for channel in channels.unwrap().iter_mut() {
                channel.stop().unwrap();
            }
            Ok(())
        }
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