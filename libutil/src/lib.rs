#[no_mangle]
pub extern fn get_peak(array:*mut f32, length:i32) -> f32 {
    let mut peak: f32 = 0.0;
    unsafe {
        let array_ptr = array as usize;
        let mut i: usize = 0;
        while i < length as usize{
            let mut sample = *((array_ptr + i * 4) as *const f32);
            if sample < 0.0 {sample = -sample}
            if sample > peak {peak = sample}
            i += 1;
        }
    }
    return 20.0 * peak.log10();
}