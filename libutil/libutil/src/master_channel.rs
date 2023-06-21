use std::collections::btree_map::Iter;

use crate::stream_writer::*;
use crate::audio_capturing_channel::*;
use cpal::{Device, Devices, OutputDevices, DevicesError, traits::{DeviceTrait, HostTrait, StreamTrait}};

pub struct MasterChannel{
    stream_writer: StreamWriter,
    state: MasterChanelStates,
    audio_channels: Vec<AudioCapturingChannel>,
    master_buffer: Vec<f32>,
}

impl MasterChannel{
    pub fn new() -> Self{
        Self{
            stream_writer: StreamWriter::new(),
            state: MasterChanelStates::Stopped,
            audio_channels: Vec::new(),
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
                self.audio_channels.push(channel);
                return Ok(());
            }
        }
        
    }

    pub fn start_streaming(&mut self) -> Result<(), ()>{
        match self.state {
            MasterChanelStates::Stopped => {
                for channel in self.audio_channels.iter_mut() {
                    channel.start();}
                Ok(())
            },
            _ => Err(())
        }
    }

    pub fn stop(&mut self){
        for channel in self.audio_channels.iter_mut() {
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

fn mixer_loop(){
    
}