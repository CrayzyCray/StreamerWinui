//use std::time::Instant;
//pub type SampleType = f32;

use crate::QPCTime;
use crate::WaveFormat;

//pub struct AudioFrame (pub Vec<u8>, pub QPCTime);
pub struct AudioPacket {
    data: Vec<u8>,
    time: QPCTime,
    readed: usize,
}

impl AudioPacket {
    pub fn new(data: Vec<u8>, time: QPCTime) -> Self {
        Self { data, time, readed: 0 }
    }

    pub fn empty() -> Self {
        Self { data: vec![], time: QPCTime::zero(), readed: 0 }
    }

    pub fn read(&mut self, format: &WaveFormat, frames_to_read: usize) -> Option<&[u8]> {
        if self.readed + frames_to_read * format.bytes_per_frame() as usize > self.data.len() {
            return None;
        }

        let start = self.readed;
        let end = start + frames_to_read * format.bytes_per_frame() as usize;
        let buffer = &self.data.as_slice()[start..end];
        self.time.add_secs_f32(frames_to_read as f32 / format.sample_rate() as f32);
        return Some(buffer);
    }

    pub fn available_bytes(&self) -> usize {
        self.data.len() - self.readed
    }

    pub fn available_frames(&self, format: &WaveFormat) -> usize {
        (self.data.len() - self.readed) / format.bytes_per_frame() as usize
    }

    pub fn data(&self) -> &[u8] {
        &self.data
    }

    pub fn time(&self) -> &QPCTime {
        &self.time
    }
}