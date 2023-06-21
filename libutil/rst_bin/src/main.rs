extern crate ffmpeg;
//use std::ffi::CStr;
use std::{*, time::Duration, sync::mpsc};

struct dr{
    i: i32,
}
impl Drop for dr{
    fn drop(&mut self) {
        println!("dropped");
    }
}

struct st<'a>{
    var: Vec<&'a dr>,
}

fn main() {
    let mut a = st {var: vec![&dr {i: 1}, &dr {i: 2}]};
    a.var.remove(0);
    println!("HW");
}
