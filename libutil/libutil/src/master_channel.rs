use wasapi::Device;

//use wasapi::*;
use crate::stream_writer::*;
use crate::audio_capturing_channel::*;

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
    pub fn get_devices(direction: &wasapi::Direction) -> Option<wasapi::DeviceCollection>{
        let collection = wasapi::DeviceCollection::new(direction);
        //let dev = collection.unwrap().get_device_at_index(0).unwrap();
        return Option::from(collection.unwrap())
    }
    pub fn add_device(&mut self, device: Device){
        self.audio_channels.push(AudioCapturingChannel::new(device))
    }
}

pub enum MasterChanelStates{
    Stopped,
    Monitoring,
    Streaming,
}