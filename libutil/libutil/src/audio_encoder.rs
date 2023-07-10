#![allow(dead_code)]
use core::panic;
use crate::stream_writer::StreamWriter;
use ffmpeg::*;

pub struct AudioEncoder{
    stream_index: i32,
    stream_writer: StreamWriter,
    packet_time_stamp: i64,
    codec_context: *mut AVCodecContext,
    packet: *mut AVPacket,
    frame: *mut AVFrame,
    codec_parameters: *mut AVCodecParameters,
}

impl AudioEncoder{
    pub fn new(audio_codec: AudioCodecs, stream_writer: StreamWriter, channels: i32, required_sample_rate: i32) -> Self{
        let codec_id = match audio_codec {
            AudioCodecs::libopus => {
                if required_sample_rate != 48000 {panic!()}
                AVCodecID_AV_CODEC_ID_OPUS},
            _ => panic!(),
        };
        
        unsafe{
            let packet = av_packet_alloc();

            let codec = avcodec_find_encoder(codec_id);
            let codec_context = avcodec_alloc_context3(codec);
            (*codec_context).sample_rate = required_sample_rate;
            (*codec_context).sample_fmt = AVSampleFormat_AV_SAMPLE_FMT_FLT;
            (*codec_context).time_base = AVRational{ num: 1, den: required_sample_rate};
            av_channel_layout_default(&mut (*codec_context).ch_layout, channels);
            avcodec_open2(codec_context, codec, std::ptr::null_mut());

            let codec_parameters = avcodec_parameters_alloc();
            avcodec_parameters_from_context(codec_parameters, codec_context);
            
            let frame = av_frame_alloc();
            (*frame).nb_samples = (*codec_context).frame_size;
            (*frame).format = AVSampleFormat_AV_SAMPLE_FMT_FLT;
            av_channel_layout_default(&mut (*frame).ch_layout, channels);

            Self { 
                stream_index: -1, 
                stream_writer, 
                packet_time_stamp: 0, 
                codec_context, 
                packet, 
                frame, 
                codec_parameters}
        }
    }
    unsafe fn encode_buffer(&self, buffer: &[f32]) -> i32{
        av_packet_unref(self.packet);
        (*self.frame).pts = self.packet_time_stamp;
        let mut ret = avcodec_fill_audio_frame(self.frame, self.channels(), AVSampleFormat_AV_SAMPLE_FMT_FLT, buffer.as_ptr().cast(), self.frame_size_in_bytes(), self.channels() * 4);
        if ret < 0 {return ret};
        ret = avcodec_send_frame(self.codec_context, self.frame);
        if ret < 0 {return ret};
        ret = avcodec_receive_packet(self.codec_context, self.packet);
        if ret == 0 {
            (*self.packet).stream_index = self.stream_index};
        return ret;
    }

    pub fn register_avstream(&mut self, stream_writer: StreamWriter){
        self.stream_writer = stream_writer;
        self.stream_index = self.stream_writer.add_av_stream(self.codec_parameters)
    }

    pub fn channels(&self) -> i32{
        if self.codec_context.is_null() {
            return 0;
        }
        unsafe {
            (*self.codec_context).channels
        }
    }

    pub fn frame_size_in_bytes(&self) -> i32{
        if self.codec_context.is_null() {
            return 0;
        }
        unsafe {
            (*self.codec_context).frame_size * 4
        }
    }

    pub fn stream_index(&self) -> i32{
        self.stream_index
    }
}

impl Drop for AudioEncoder{
    fn drop(&mut self) {
        unsafe{
            av_packet_unref(self.packet);
            avcodec_send_packet(self.codec_context, self.packet); //flush codec_context
            avcodec_free_context(&mut self.codec_context);
            av_packet_free(&mut self.packet);
            avcodec_parameters_free(&mut self.codec_parameters)
        }
        
    }
}

pub enum AudioCodecs{
    libopus,
}