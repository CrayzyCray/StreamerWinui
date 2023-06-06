#![allow(dead_code)]
extern crate ffmpeg;
use std::ffi::CString;
use std::ptr::{null, null_mut};

use ffmpeg::*;

pub struct StreamWriter{
    stream_parameters: Vec<StreamParameters>,
    stream_clients: Vec<StreamClient>,
}

impl StreamWriter{
    const DEFAULT_PORT: i32 = 10000;

    pub fn new() -> Self {
        Self{
            stream_parameters: Vec::new(),
            stream_clients: Vec::new(),
        }
    }

    pub fn add_av_stream(&mut self, codec_parameters: *mut AVCodecParameters) -> i32 {
        self.stream_parameters.push(StreamParameters { codec_parameters });
        return self.stream_parameters.len() as i32 - 1;
    }

    pub fn add_client(&mut self, ip: std::net::Ipv4Addr, port: u16){
        if self.stream_parameters.len() == 0 {
            return;
        }

        #[allow(temporary_cstring_as_ptr)]
        let output_url = CString::new(format!("rist://{}:{}", ip, port)).unwrap().as_ptr();
        let mut format_context: *mut AVFormatContext = null_mut();

        unsafe{
            #[allow(temporary_cstring_as_ptr)]
            let format_name = std::ffi::CString::new("mpegts").unwrap().as_ptr();
            avformat_alloc_output_context2(&mut format_context, null(), format_name, null());

            for stream_parameter in self.stream_parameters.iter() {
                let codec = avcodec_find_encoder((*(*stream_parameter).codec_parameters).codec_id);
                let stream = avformat_new_stream(format_context, codec);
                // Copy the codec parameters to the new stream.
                avcodec_parameters_copy((*stream).codecpar, stream_parameter.codec_parameters);
                // Set the time base for the new stream to match the stream parameter.
                //(*stream).time_base = (*stream_parameter.time_base).clone()}
            }
            
            avio_open(&mut (*format_context).pb, output_url, AVIO_FLAG_WRITE as i32);
            avformat_write_header(format_context, null_mut());

            // Update the time base for each stream parameter to match the newly created streams.
            // for i in 0..self.stream_parameters.len() as usize {
            //     let stream = *(*format_context).streams.offset(i as isize);
            //     *self.stream_parameters[i].time_base = (*stream).time_base}}
        }
        self.stream_clients.push(StreamClient { ip, port, format_context, is_file: false });
    }

    pub fn add_client_as_file(&mut self, path: &str){
        if self.stream_parameters.len() == 0 {
            return;
        }

        let mut format_context: *mut AVFormatContext = null_mut();

        unsafe{
            #[allow(temporary_cstring_as_ptr)]
            let output_url = std::ffi::CString::new(path).unwrap().as_ptr();
            avformat_alloc_output_context2(&mut format_context, null(), null(), output_url);

            for stream_parameter in self.stream_parameters.iter() {
                let codec = avcodec_find_encoder((*(*stream_parameter).codec_parameters).codec_id);
                let stream = avformat_new_stream(format_context, codec);
                // Copy the codec parameters to the new stream.
                avcodec_parameters_copy((*stream).codecpar, stream_parameter.codec_parameters);
                // Set the time base for the new stream to match the stream parameter.
                //(*stream).time_base = (*stream_parameter.time_base).clone()}
            }
            
            avio_open(&mut (*format_context).pb, output_url, AVIO_FLAG_WRITE as i32);
            avformat_write_header(format_context, null_mut());
        }

        self.stream_clients.push(StreamClient { ip: std::net::Ipv4Addr::new(0,0,0,0), port: 0, format_context, is_file: true })
    }

    pub fn write_packet(&self, packet: *mut AVPacket, time_base_packet: *const AVRational) {
        if self.stream_clients.is_empty() || packet.is_null() || time_base_packet.is_null() {
            return;
        }
        unsafe{
            let stream = **(*self.stream_clients[0].format_context).streams.offset((*packet).stream_index as isize);
            av_packet_rescale_ts(packet, *time_base_packet, stream.time_base);
            for client in self.stream_clients.iter() {
                av_write_frame(client.format_context, packet);
            }
        }
    }

    pub fn close_all_clients(&mut self){
        if self.stream_clients.is_empty(){
            return;
        }
        for client in self.stream_clients.iter() {
            unsafe{
                close_format_context(client.format_context)
            }
        }
        self.stream_clients.clear()
    }

    fn clear_stream_parameters(&mut self){
        self.stream_parameters.clear();
    }

}

impl Drop for StreamWriter{
    fn drop(&mut self) {
        self.close_all_clients();
        self.clear_stream_parameters();
    }
}

unsafe fn close_format_context(format_context: *mut AVFormatContext) {
    av_write_trailer(format_context);
    avio_closep(&mut (*format_context).pb);
    avformat_free_context(format_context);
}

#[repr(C)]
pub struct StreamClient{
    ip: std::net::Ipv4Addr,
    port: u16,
    format_context: *mut AVFormatContext,
    is_file: bool,
}

#[repr(C)]
pub struct StreamParameters{
    codec_parameters: *mut AVCodecParameters,
}
