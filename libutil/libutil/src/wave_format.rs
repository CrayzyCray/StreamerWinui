
pub struct WaveFormat {
    channels: u8,
    sample_type: SampleType,
}

impl WaveFormat {
    pub fn new(channels: u8, sample_type: SampleType) -> Self {
        Self { channels, sample_type }
    }

    pub fn bytes_per_frame(&self) -> u16 {
        (self.channels * self.sample_type.size()) as u16
    }

    pub fn channels(&self) -> u8 {
        self.channels
    }

    pub fn sample_type(&self) -> SampleType {
        self.sample_type.clone()
    }
}

#[derive(Clone)]
pub enum SampleType {
    F32,
}

impl SampleType {
    pub fn size(&self) -> u8 {
        match self {
            Self::F32 => 4,
        }
    }
}