#![allow(dead_code)]
use std::sync::{Arc, mpsc::Receiver, mpsc::SyncSender, mpsc};
use std::time::Duration;
use std::*;
use windows::{Win32::{Media::Audio::*, Devices::Properties::{DEVPKEY_Device_FriendlyName, DEVPROPKEY}}, core::PWSTR};
use crate::audio_packet::*;
use crate::QPCTime;
use crate::WaveFormat;

pub struct AudioCapturingChannel {
    device: IMMDevice,
    data_flow: EDataFlow,
    is_capturing: bool,
    audio_client: IAudioClient3,
    mix_format: WaveFormat,
    frames_per_packet: u32,
    capture_client: IAudioCaptureClient,
    name: PWSTR,
    buffer_duration: i64,
    cached_packet: AudioPacket,
}

impl AudioCapturingChannel {
    pub fn new(device: IMMDevice, data_flow: EDataFlow) -> Self{
        let audio_client: IAudioClient3;
        let wave_format_ex;
        let samples_per_buffer = 960;
        let buffer_duration: i64;
        let capture_client: IAudioCaptureClient;
        let name;
        let mix_format;
        
        unsafe{
            use windows::Win32::System::Com::*;
            use windows::Win32::UI::Shell::PropertiesSystem::*;

            let property_store = device.OpenPropertyStore(STGM_READ).unwrap();
            let prop = property_store.GetValue(&DEVPKEY_Device_FriendlyName as *const DEVPROPKEY as *const PROPERTYKEY).unwrap();
            name = PropVariantToStringAlloc(&prop).unwrap();

            audio_client = device.Activate(CLSCTX_ALL, None).unwrap();
            wave_format_ex = audio_client.GetMixFormat().unwrap();
            buffer_duration = Duration::from_secs(1).as_nanos() as i64 / ((*wave_format_ex).nSamplesPerSec / samples_per_buffer) as i64;
            let mut streamflags = 0;
            //streamflags |= AUDCLNT_STREAMFLAGS_EVENTCALLBACK;
            if data_flow == eRender {
                streamflags |= AUDCLNT_STREAMFLAGS_LOOPBACK;
            }
            audio_client.Initialize(AUDCLNT_SHAREMODE_SHARED, streamflags, buffer_duration, 0, wave_format_ex, None).unwrap();
            capture_client = audio_client.GetService().unwrap();
            mix_format = WaveFormat::new((*wave_format_ex).nChannels, crate::wave_format::SampleType::F32, (*wave_format_ex).nSamplesPerSec);
            windows::Win32::System::Com::CoTaskMemFree(Some(wave_format_ex as *const std::os::raw::c_void));
        }

        Self{
            device,
            data_flow,
            is_capturing: false,
            audio_client,
            mix_format,
            frames_per_packet: samples_per_buffer,
            capture_client,
            name,
            buffer_duration,
            cached_packet: AudioPacket::empty(),
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

    pub fn read2(&self) -> Option<AudioPacket>{
        if !self.is_capturing {
            return None
        }

        use std::ptr::null_mut;
        
        let mut p_buf = null_mut();
        let mut frames_stored = 0;
        let mut flags = 0;
        let mut qpc_position = 0;
        let mut device_position = 0;
        let buffer: Vec<u8>;

        unsafe{
            if self.capture_client.GetNextPacketSize().unwrap() == 0 {
                return None;
            }
            self.capture_client.GetBuffer(&mut p_buf, &mut frames_stored, &mut flags, Some(&mut device_position), Some(&mut qpc_position))
            .unwrap();
            let buffer_size = frames_stored * self.mix_format.bytes_per_frame() as u32;
            buffer = Vec::from(slice::from_raw_parts(p_buf, buffer_size as usize).clone());
            self.capture_client.ReleaseBuffer(frames_stored).unwrap();
        }
        return Some(AudioPacket::new(buffer, QPCTime(qpc_position)));
    }

    // pub fn read(&self, count: Option<u16>) -> Option<AudioPacket> {
    //     if !self.is_capturing {
    //         return None
    //     }

    //     match count {
    //         Some(count) => self.read_fixed_size_packet(count),
    //         None => self.read_one_packet()
    //     }
    // }

    pub fn read_fixed_size_packet(&mut self, frames_count: usize) -> Option<AudioPacket> {
        let mut packet_data: Vec<u8> = vec![0u8; self.mix_format.bytes_per_frame() as usize * frames_count as usize];
        let mut frames_stored = 0;
        let mut time = None;
        
        if self.cached_packet.available_bytes() != 0 {
            let frames_to_read = self.cached_packet.available_frames(&self.mix_format).min(frames_count);
            let buffer = self.cached_packet.read(&self.mix_format, frames_to_read).unwrap();
            unsafe{ ptr::copy_nonoverlapping(buffer.as_ptr(), packet_data.as_mut_slice().as_mut_ptr(), frames_to_read * self.mix_format.bytes_per_frame() as usize) }
            frames_stored = frames_to_read;
            time = Some(self.cached_packet.time().0);
        }
        
        while frames_stored < frames_count {
            let mut buffer_pointer = std::ptr::null_mut();
            let mut frames_captured = 0;
            let mut flags = 0;
            let mut qpc_position = 0;
            let mut device_position = 0;

            unsafe{
                if self.capture_client.GetNextPacketSize().unwrap() == 0 {
                    //sleep half of buffer
                    thread::sleep(Duration::from_secs_f32((frames_count - frames_stored) as f32 / self.mix_format.sample_rate() as f32 / 2f32));
                    continue;
                }
                self.capture_client.GetBuffer(&mut buffer_pointer, &mut frames_captured, &mut flags, Some(&mut device_position), Some(&mut qpc_position)).unwrap();
                let buffer_wasapi = slice::from_raw_parts(buffer_pointer, frames_captured as usize * self.mix_format.bytes_per_frame() as usize);

                let frames_to_copy = (frames_captured as usize).min(frames_count - frames_stored);
                let bytes_to_copy = frames_to_copy * self.mix_format.bytes_per_frame() as usize;
                let bytes_stored = frames_stored * self.mix_format.bytes_per_frame() as usize;

                ptr::copy_nonoverlapping(
                    buffer_wasapi.as_ptr(), 
                    packet_data.as_mut_slice().as_mut_ptr().offset(bytes_stored as isize), 
                    bytes_to_copy
                );

                frames_stored += frames_to_copy;

                if frames_to_copy < frames_captured as usize {
                    let data = buffer_wasapi[bytes_to_copy..].to_vec();
                    let mut time = QPCTime(qpc_position);
                    time.add_secs_f32(frames_to_copy as f32 / self.mix_format.sample_rate() as f32);
                    self.cached_packet = AudioPacket::new(data, time);
                }

                if time.is_none() {
                    time = Some(qpc_position);
                }

                self.capture_client.ReleaseBuffer(frames_captured).unwrap();
            }
        }
        
        return Some(AudioPacket::new(packet_data, QPCTime(time.unwrap())));
    }

    // fn read_one_packet(&self) -> Option<AudioPacket> {
    //     use std::ptr::null_mut;
        
    //     let mut p_buf = null_mut();
    //     let mut frames_stored = 0;
    //     let mut flags = 0;
    //     let mut qpc_position = 0;
    //     let mut device_position = 0;
    //     let buffer: Vec<u8>;

    //     unsafe{
    //         if self.capture_client.GetNextPacketSize().unwrap() == 0 {
    //             return None;
    //         }
    //         self.capture_client.GetBuffer(&mut p_buf, &mut frames_stored, &mut flags, Some(&mut device_position), Some(&mut qpc_position))
    //         .unwrap();
    //         let buffer_size = frames_stored * (*self.mix_format).nBlockAlign as u32;
    //         buffer = Vec::from(slice::from_raw_parts(p_buf, buffer_size as usize));
    //         self.capture_client.ReleaseBuffer(frames_stored).unwrap();
    //     }
    //     return Some(AudioPacket::new(data: buffer, time: QPCTime(qpc_position)))
    // }

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
        if self.is_capturing {
            unsafe{ self.audio_client.Stop().unwrap() }
        }
    }
}