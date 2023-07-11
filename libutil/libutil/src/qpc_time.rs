use windows::Win32::System::Performance::*;
use std::time::Duration;

pub struct QPCTime (pub u64); //time in 100 nanos

impl QPCTime {
    pub fn now() -> Self {
        let mut qpc = 0;
        unsafe{QueryPerformanceCounter(&mut qpc)};
        Self(qpc as u64)
    }

    pub fn elapsed(&self) -> QPCTime {
        let mut qpc = 0;
        unsafe{QueryPerformanceCounter(&mut qpc)};
        QPCTime(qpc as u64 - self.0)
    }

    pub fn as_secs_f32(&self) -> f32 {
        self.0 as f32 / 10_000_000f32 //10_000_000f32 is 100 nanosecs per sec
    }

    pub fn as_millis(&self) -> u128 {
        self.0 as u128 / 10_000
    }

    pub fn as_nanos(&self) -> u128 {
        self.0 as u128 * 100
    }

    pub fn to_duration(&self) -> Duration {
        Duration::from_nanos(self.0 * 100)
    }
}

impl std::ops::Sub for QPCTime {
    type Output = QPCTime;

    fn sub(self, rhs: Self) -> QPCTime {
        QPCTime(self.0 - rhs.0)
    }
}