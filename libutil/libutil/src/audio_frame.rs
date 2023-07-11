//use std::time::Instant;
//pub type SampleType = f32;

use std::time::Duration;

pub struct QPCTime (pub u64); //time in 100 nanos

impl QPCTime {
    pub fn as_secs_f32(&self) -> f32 {
        self.0 as f32 / 10_000_000f32 //10_000_000f32 is 100 nanosecs per sec
    }

    pub fn as_millis(&self) -> u128 {
        self.0 as u128 / 10_000
    }

    pub fn as_nanos(&self) -> u128 {
        self.0 as u128 * 100
    }

    pub fn to_duration(&self) -> Duration {
        Duration::from_nanos(self.0 * 100)
    }
}
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