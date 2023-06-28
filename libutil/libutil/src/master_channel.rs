use crate::stream_writer::*;
use crate::audio_capturing_channel::*;
use std::*;
use std::sync::Arc;
use cpal::BufferSize;
use cpal::{Device, Devices, OutputDevices, DevicesError, traits::{DeviceTrait, HostTrait, StreamTrait}};

pub struct MasterChannel{
    stream_writer: Arc<StreamWriter>,
    state: MasterChanelStates,
    audio_channels: Arc<Vec<AudioCapturingChannel>>,
    master_buffer: Vec<f32>,
}

impl MasterChannel{
    pub fn new() -> Self{
        Self{
            stream_writer: Arc::new(StreamWriter::new()),
            state: MasterChanelStates::Stopped,
            audio_channels: Arc::new(vec![]),
            master_buffer: Vec::new(),
        }
    }
    pub fn available_devices(&self, is_loopback: bool) -> Result<Vec<Device>, ()>{
        ///todo
        let all_devices = match is_loopback {
            true => cpal::default_host().output_devices(),
            false => cpal::default_host().input_devices(),
        };

        if all_devices.is_err() {return Err(())}
        let mut available_devices: Vec<Device> = Vec::new();
        
        for device in all_devices.unwrap() {
            let mut is_used = false;
            let device_name = device.name().unwrap();
            for channel in self.audio_channels.iter() {
                if device_name == channel.device().name().unwrap() {
                    is_used = true;
                    break;
                }
            }

            if is_used {
                available_devices.push(device);
            }
        }

        return Ok(available_devices);
    }
    
    pub fn add_device(&mut self, device: Device, is_loopback: bool) -> Result<(), ()>{
        match self.state {
            MasterChanelStates::Streaming => return Err(()),
            _ => {
                let channel = AudioCapturingChannel::new(device, is_loopback, 4);
                Arc::get_mut(&mut self.audio_channels).unwrap().push(channel);
                return Ok(());
            }
        }
        
    }

    pub fn start_streaming(&mut self) -> Result<(), ()>{
        match self.state {
            MasterChanelStates::Stopped => {
                let channels = Arc::get_mut(&mut self.audio_channels).unwrap();
                for channel in channels.iter_mut() {
                    channel.start();}
                Ok(())
            },
            _ => Err(())
        }
    }

    pub fn stop(&mut self){
        let channels = Arc::get_mut(&mut self.audio_channels).unwrap();
        for channel in channels.iter_mut() {
            channel.stop();
        }
    }

    pub fn get_default_device(is_loopback: bool) -> Option<Device>{
        match is_loopback {
            true => cpal::default_host().default_output_device(),
            false => cpal::default_host().default_input_device(),
        }
    }
}

pub enum MasterChanelStates{
    Stopped,
    Monitoring,
    Streaming,
}

fn mixer_loop(audio_channels: &Arc<Vec<AudioCapturingChannel>>, stream_writer: &Arc<StreamWriter>, frame_size: usize){
    let mut master_buffer: Vec<f32>;
    let mut stop_flag;

    loop {
        for channel in audio_channels.iter() {
            let buffer = channel.read_next_frame();
            if buffer.is_err() {stop_flag = true}
            let buffer = buffer.unwrap();
            
        }
    }
}