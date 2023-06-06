extern crate ffmpeg;
use std::ffi::CStr;

use ffmpeg::*;

fn main() {
    unsafe{
        let codec = avcodec_find_encoder(AVCodecID_AV_CODEC_ID_OPUS);
        let s = std::ffi::CStr::from_ptr((*codec).wrapper_name);
        println!("{:?}", s);
    }
    println!("Hello, world!");
}
