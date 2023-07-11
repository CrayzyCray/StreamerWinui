#![allow(dead_code)]
use std::sync::{Arc, mpsc::Receiver, mpsc::SyncSender, mpsc};
use std::time::Duration;
use std::*;
use windows::{Win32::{Media::Audio::*, Devices::Properties::{DEVPKEY_Device_FriendlyName, DEVPROPKEY}}, core::PWSTR};
use crate::audio_frame::*;
use crate::QPCTime;

pub struct AudioCapturingChannel{
    receiver: Receiver<Arc<AudioFrame>>,
    sync_sender: Arc<SyncSender<Arc<AudioFrame>>>,
    device: IMMDevice,
    data_flow: EDataFlow,
    is_capturing: bool,
    audio_client: IAudioClient3,
    mix_format: *const WAVEFORMATEX,
    samples_per_buffer: u32,
    capture_client: IAudioCaptureClient,
    name: PWSTR,
    buffer_duration: i64,
}

impl AudioCapturingChannel{
    pub fn new(device: IMMDevice, data_flow: EDataFlow) -> Self{
        let (sync_sender, receiver) = mpsc::sync_channel(4);
        let sync_sender = Arc::new(sync_sender);
        let audio_client: IAudioClient3;
        let mix_format;
        let samples_per_buffer = 960;
        let buffer_duration: i64;
        let capture_client: IAudioCaptureClient;
        let name;
        
        unsafe{
            use windows::Win32::System::Com::*;
            use windows::Win32::UI::Shell::PropertiesSystem::*;

            let property_store = device.OpenPropertyStore(STGM_READ).unwrap();
            let prop = property_store.GetValue(&DEVPKEY_Device_FriendlyName as *const DEVPROPKEY as *const PROPERTYKEY).unwrap();
            name = PropVariantToStringAlloc(&prop).unwrap();

            audio_client = device.Activate(CLSCTX_ALL, None).unwrap();
            mix_format = audio_client.GetMixFormat().unwrap();
            buffer_duration = Duration::from_secs(1).as_nanos() as i64 / ((*mix_format).nSamplesPerSec / samples_per_buffer) as i64;
            let mut streamflags = 0;
            //streamflags |= AUDCLNT_STREAMFLAGS_EVENTCALLBACK;
            if data_flow == eRender {
                streamflags |= AUDCLNT_STREAMFLAGS_LOOPBACK;
            }
            audio_client.Initialize(AUDCLNT_SHAREMODE_SHARED, streamflags, buffer_duration, 0, mix_format, None).unwrap();
            capture_client = audio_client.GetService().unwrap();
        }

        Self{
            receiver,
            sync_sender,
            device,
            data_flow,
            is_capturing: false,
            audio_client,
            mix_format,
            samples_per_buffer,
            capture_client,
            name,
            buffer_duration,
        }
    }

    pub fn have_unreaded_frames(&self) -> bool {
        unsafe{
            self.capture_client.GetNextPacketSize().unwrap() > 0
        }
    }

    pub fn buffer_duration(&self) -> i64 {
        self.buffer_duration
    }

    pub fn stream_latency(&self) -> i64 {
        unsafe{
            self.audio_client.GetStreamLatency().unwrap()
        }
    }

    pub fn start(&mut self) -> Result<(), ()>{
        if self.is_capturing {
            return Err(());
        }
        unsafe{
            if self.audio_client.Start().is_err() {
                return Err(());
            }
        }
        self.is_capturing = true;
        return Ok(());
    }

    pub fn stop(&mut self) -> Result<(), ()>{
        if !self.is_capturing {
            return Err(());
        }
        unsafe{
            if self.audio_client.Stop().is_err() {
                return Err(());
            }
        }
        self.is_capturing = false;
        return Ok(());
    }

    pub fn read(&self) -> Option<AudioFrame>{
        if !self.is_capturing {
            return None
        }

        use std::ptr::null_mut;
        
        let mut p_buf = null_mut();
        let mut frames_stored = 0;
        let mut flags = 0;
        let mut qpc_position = 0;
        let buffer: Vec<u8>;

        unsafe{
            if self.capture_client.GetNextPacketSize().unwrap() == 0 {
                return None;
            }
            self.capture_client.GetBuffer(&mut p_buf, &mut frames_stored, &mut flags, None, Some(&mut qpc_position))
            .unwrap();
            let buffer_size = frames_stored * (*self.mix_format).nBlockAlign as u32;
            buffer = Vec::from(slice::from_raw_parts(p_buf, buffer_size as usize));
            self.capture_client.ReleaseBuffer(frames_stored).unwrap();
        }
        return Some(AudioFrame{data: buffer, time: QPCTime(qpc_position)})
    }

    pub fn device(&self) -> &IMMDevice{
        &self.device
    }

    pub fn is_capturing(&self) -> bool {
        self.is_capturing
    }

    pub fn device_name(&self) -> String {
        unsafe{self.name.to_string().unwrap()}
    }
}

impl Drop for AudioCapturingChannel{
    fn drop(&mut self) {
        unsafe{
            windows::Win32::System::Com::CoTaskMemFree(Some(self.mix_format as *const std::os::raw::c_void))
        }
    }
}