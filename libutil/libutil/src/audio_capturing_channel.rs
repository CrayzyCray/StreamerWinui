use std::collections::VecDeque;

use wasapi::*;

pub struct AudioCapturingChannel{
    buffers_queue: VecDeque<Vec<f32>>,
    device: Device,
    //capturing_thread: 
}

impl AudioCapturingChannel{
    pub fn new(device: Device) -> Self{
        match wasapi::initialize_mta() {
            Ok(_) => (),
            Err(e) => println!("initialize_mta error: {}", e),
        }

        Self { 
            buffers_queue: VecDeque::new(),
            device,
        }
    }
}