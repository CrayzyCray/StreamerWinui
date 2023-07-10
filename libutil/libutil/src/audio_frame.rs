//use std::time::Instant;
//pub type SampleType = f32;

pub struct AudioFrame (pub Vec<u8>, pub u64);
// pub struct AudioFrame{
//     data: Vec<u8>,
//     time_captured: u64,
// }

// impl AudioFrame {
//     pub fn new(data: Vec<u8>, time_captured: u64) -> Self{
//         Self { data, time_captured}
//     }
//     pub fn data(&self) -> &Vec<u8>{
//         &self.data
//     }
// }