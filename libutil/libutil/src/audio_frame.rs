//use std::time::Instant;
//pub type SampleType = f32;

use crate::QPCTime;

//pub struct AudioFrame (pub Vec<u8>, pub QPCTime);
pub struct AudioFrame{
    pub data: Vec<u8>,
    pub time: QPCTime,
}

// impl AudioFrame {
//     pub fn new(data: Vec<u8>, time_captured: u64) -> Self{
//         Self { data, time_captured}
//     }
//     pub fn data(&self) -> &Vec<u8>{
//         &self.data
//     }
// }