use std::time::Instant;
pub type SampleType = f32;

pub struct AudioFrame{
    data: Vec<SampleType>,
    time_captured: Instant,
}

impl AudioFrame {
    pub fn new(data: Vec<SampleType>, time_captured: Instant) -> Self{
        Self { data, time_captured}
    }
    pub fn data(&self) -> &Vec<SampleType>{
        &self.data
    }
}