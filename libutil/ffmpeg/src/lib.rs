#![allow(warnings)]
include!("../bindings/bindings.rs");

fn main() {
    unsafe {
        let mut f = av_frame_alloc();
        println!("{:?}", f);
    }
}