use crate::audio_encoder::*;
use crate::stream_writer::*;
use crate::master_channel::*;

pub struct StreamController{
    stream_is_active: bool,
    audio_capturing: bool,
    stream_writer: StreamWriter,
    master_channel: MasterChannel,
}

impl StreamController{
    pub fn stream_is_active(&self) -> bool {self.stream_is_active}
    pub fn audio_capturing(&self) -> bool {self.audio_capturing}
    pub fn audio_capturing_set(&mut self, bool: bool) {self.audio_capturing = bool}
    pub fn new() -> Self{
        Self { 
            stream_is_active: false, 
            audio_capturing: true, 
            stream_writer: StreamWriter::new(),
            master_channel: MasterChannel::new(),
        }
    }
    pub fn start_streaming(&mut self){
        todo!()
    }
    pub fn stop_streaming(&mut self){
        todo!()
    }
}

impl Drop for StreamController{
    fn drop(&mut self) {
        self.stop_streaming()
    }
}