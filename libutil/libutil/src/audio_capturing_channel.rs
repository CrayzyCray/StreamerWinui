use std::{error, sync::Mutex, rc::Rc, sync::{Arc, mpsc::RecvError, mpsc::Receiver, mpsc::SyncSender, mpsc}, time::Instant};
use cpal::*;
use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use std::*;
use crate::audio_frame::*;




pub struct AudioCapturingChannel{
    receiver: Receiver<Arc<AudioFrame>>,
    sync_sender: Arc<SyncSender<Arc<AudioFrame>>>,
    device: Device,
    is_loopback: bool,
    stream: Option<Stream>,
    config: Option<StreamConfig>,
    is_capturing: bool,
}

impl AudioCapturingChannel{
    pub fn new(device: Device, is_loopback: bool, queue_size: usize) -> Self{
        let (sync_sender, receiver) = mpsc::sync_channel(queue_size);
        let sync_sender = Arc::new(sync_sender);
        Self { 
            receiver,
            sync_sender,
            device,
            is_loopback,
            stream: None,
            config: None,
            is_capturing: false,
        }
    }
    pub fn start(&mut self) -> Result<(), String>{
        if self.is_capturing {return Err("already capturing".to_string())}

        let config = match self.is_loopback {
            true => self.device.default_output_config().unwrap(),
            false => self.device.default_input_config().unwrap(),
        };
        println!("{:?}", config);
        let sample_fmt = config.sample_format();
        if sample_fmt != SampleFormat::F32 {
            return Err("sample format is not f32".to_string());
        }
        let stream_config = config.config();

        
        let sender = self.sync_sender.clone();

        let stream = match self.device.build_input_stream(&stream_config, move |data: &[f32], err: &cpal::InputCallbackInfo| data_callback(data, err, &sender), error_callback, None) {
                    Ok(s) => s,
                    Err(e) => return Err(e.to_string()),
                };
        stream.play().unwrap();
        self.stream = Some(stream);
        self.config = Some(config.config());
        self.is_capturing = true;
        return Ok(());
    }

    pub fn stop(&mut self) -> Result<(), ()>{
        if !self.is_capturing {
            return Err(())}
        self.stream.as_mut().unwrap().pause().unwrap();
        self.stream = None;
        //self.receiver = None;
        self.config = None;
        self.is_capturing = false;
        return Ok(());
    }

    pub fn read_next_frame(&self) -> Result<Arc<AudioFrame>, ()>{
        if !self.is_capturing {
            return Err(())}
        
        match self.receiver.recv(){
            Ok(data) => Ok(data),
            Err(_) => Err(()),
        }
    }

    pub fn device(&self) -> &Device{
        &self.device
    }

    pub fn is_capturing(&self) -> bool {
        self.is_capturing
    }

    pub fn device_name(&self) -> String {
        self.device.name().unwrap()
    }
}

fn data_callback(data: &[SampleType], _: &cpal::InputCallbackInfo, sender: &Arc<mpsc::SyncSender<Arc<AudioFrame>>>) {
    println!("data_callback. size: {}", data.len());
    let time_captured = time::Instant::now();
    let data = Vec::from(data);
    let audio_frame = Arc::from(AudioFrame::new(data, time_captured));
    match sender.send(audio_frame) {
        Ok(_) => (),
        Err(e) => panic!("{}", e.to_string()),
    }
}

fn error_callback(err: cpal::StreamError) {
    println!("error_callback. error: {:?}", err)
}