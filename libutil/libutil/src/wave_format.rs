
pub struct WaveFormat {
    channels: u16,
    sample_type: SampleType,
    sample_rate: u32,
}

impl WaveFormat {
    pub fn new(channels: u16, sample_type: SampleType, sample_rate: u32) -> Self {
        Self { channels, sample_type, sample_rate }
    }

    pub fn bytes_per_frame(&self) -> u16 {
        self.channels * self.sample_type.size()
    }

    pub fn channels(&self) -> u16 {
        self.channels
    }

    pub fn sample_type(&self) -> SampleType {
        self.sample_type.clone()
    }

    pub fn sample_rate(&self) -> u32 {
        self.sample_rate
    }
}

#[derive(Clone)]
pub enum SampleType {
    F32,
}

impl SampleType {
    pub fn size(&self) -> u16 {
        match self {
            Self::F32 => 4,
        }
    }
}