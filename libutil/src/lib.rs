#[no_mangle]
pub unsafe extern fn get_peak(array:*mut f32, length:i32) -> f32 {
    let mut peak: f32 = 0.0;
    let array_ptr = array as usize;
    let mut i: usize = 0;
    while i < length as usize{
        let mut sample = *((array_ptr + i * 4) as *const f32);
        if sample < 0.0 {sample = -sample}
        if sample > peak {peak = sample}
        i += 1;
    }
    return 20.0 * peak.log10();
}

#[no_mangle]
pub unsafe extern fn get_peak_multichannel(array:*mut f32, length:i32, channels: i32, channel_index: i32) -> f32 {
    let mut peak: f32 = 0.0;
    let array_ptr = array as usize;
    let mut i: usize = channel_index as usize;
    while i < length as usize{
        let mut sample = *((array_ptr + i * 4) as *const f32);
        if sample < 0.0 {sample = -sample}
        if sample > peak {peak = sample}
        i += channels as usize;
    }
    return 20.0 * peak.log10();
}

#[no_mangle]
pub unsafe extern fn apply_volume(array:*mut f32, length:i32, volume: f32) {
    if volume > 1.0 {return}
    let array_ptr = array as usize;
    let mut i: usize = 0;
    if volume > 0.0{
        while i < length as usize{
            *((array_ptr + i * 4) as *mut f32) *= volume;
            i += 1;
        }
    }else {
        while i < length as usize{
            *((array_ptr + i * 4) as *mut f32) = 0.0;
            i += 1;
        }
    }
}