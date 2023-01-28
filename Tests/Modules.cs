using System.Diagnostics;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;

namespace StreamerLib
{
    public unsafe struct Ddagrab : IDisposable
    {
        public AVInputFormat* inputFormat;
        public AVFormatContext* formatContext;
        public AVCodecParameters* codecParameters;
        public AVCodec* codec;
        public AVCodecContext* codecContext;
        public AVPacket* packet;
        public AVFrame* hwFrame;
        public AVRational timebaseMin;

        public Ddagrab(string ddagrabParameters = "")
        {
            ffmpeg.avdevice_register_all();
            inputFormat = ffmpeg.av_find_input_format("lavfi");
            formatContext = ffmpeg.avformat_alloc_context();
            codecParameters = null;
            codec = null;
            codecContext = null;
            packet = ffmpeg.av_packet_alloc();
            hwFrame = ffmpeg.av_frame_alloc();
            
            fixed(AVFormatContext** fc = &formatContext)
                ffmpeg.avformat_open_input(fc, $"ddagrab={ddagrabParameters}", inputFormat, null);
            ffmpeg.avformat_find_stream_info(formatContext, null);
            codecParameters = formatContext->streams[0]->codecpar;
            codec = ffmpeg.avcodec_find_decoder(codecParameters->codec_id);
            codecContext = ffmpeg.avcodec_alloc_context3(codec);
            ffmpeg.avcodec_parameters_to_context(codecContext, codecParameters);
            ffmpeg.avcodec_open2(codecContext, codec, null);

            ffmpeg.av_read_frame(formatContext, packet);
            ffmpeg.avcodec_send_packet(codecContext, packet);
            ffmpeg.avcodec_receive_frame(codecContext, hwFrame);
            timebaseMin.num = formatContext->streams[0]->avg_frame_rate.den;
            timebaseMin.den = formatContext->streams[0]->avg_frame_rate.num;
        }
        
        /// <summary>
        /// read, decode and write AVFrame to this.hwFrame
        /// </summary>
        /// <returns>this.hwFrame</returns>
        public AVFrame* ReadAvFrame()
        {
            ffmpeg.av_read_frame(formatContext, packet);
            ffmpeg.avcodec_send_packet(codecContext, packet);
            ffmpeg.avcodec_receive_frame(codecContext, hwFrame);
            
            return hwFrame;
        }

        public void Dispose()
        {
            if (formatContext != null)
                ffmpeg.avformat_free_context(formatContext);
            
            if (codecParameters != null)
                fixed(AVCodecParameters** p = &codecParameters)
                    ffmpeg.avcodec_parameters_free(p);
            
            if (packet != null || codecContext != null)
            {
                ffmpeg.av_packet_unref(packet);
                ffmpeg.avcodec_send_packet(codecContext, packet);//flush codecContext
            }
            
            if (codecContext != null)
                fixed (AVCodecContext** p = &codecContext)
                    ffmpeg.avcodec_free_context(p);
            
            if (packet != null)
                fixed(AVPacket** p = &packet)
                    ffmpeg.av_packet_free(p);
            
            if (hwFrame != null)
                fixed(AVFrame** p = &hwFrame)
                    ffmpeg.av_frame_free(p);
        }
    }
    
    public class AudioBufferSlicer
    {
        public byte[] Buffer => _buffer.InternalArray;
        public byte[] BufferNormal => _bufferNormal;
        public bool BufferIsFull => _buffer.IsFull;
        public int BufferedCount => _bufferSecond.Count;
        public List<int> SliceIndexes => _sliceIndexes;
        public int SliceSizeInBytes => _sliceSizeInBytes;
        
        private ByteBuffer _buffer;
        private ByteBuffer _bufferSecond;
        private int _sliceSizeInBytes;
        private List<int> _sliceIndexes = new();
        private byte[] _bufferNormal;
        
        public AudioBufferSlicer(int SliceSizeInSamples, int SampleSizeInBytes, int Channels)
        {
            _buffer = new(SliceSizeInSamples * SampleSizeInBytes * Channels);
            _bufferSecond = new(SliceSizeInSamples * SampleSizeInBytes * Channels);
            _sliceSizeInBytes = SliceSizeInSamples * SampleSizeInBytes * Channels;
        }
        
        public AudioBufferSlicer(int sizeInBytes)
        {
            _buffer = new(sizeInBytes);
            _bufferSecond = new(sizeInBytes);
            _sliceSizeInBytes = sizeInBytes;
        }
        
        /// will be deleted
        /// buffers samples that do not fit the SliceSize
        /// <returns>Array of indexes in Buffer</returns>
        public void SendBuffer(byte[] buffer, int bufferLength) =>
            SliceBuffer(buffer, bufferLength);
        
        /// will be deleted
        public AudioBufferSliced SliceBuffer(byte[] buffer, int bufferLength)
        {
            _bufferNormal = buffer;
            int bytesWritedInBufferMode = 0;
            
            if (_buffer.IsFull)
                _buffer.clear();
            
            //swap buffers
            (_buffer, _bufferSecond) = (_bufferSecond, _buffer);
            
            if (_buffer.NotEmpty)
            {
                bytesWritedInBufferMode = _buffer.SizeRemain;
                _buffer.FillToEnd(buffer, bufferLength);
            }

            _sliceIndexes = new((bufferLength - bytesWritedInBufferMode) / _sliceSizeInBytes);
            
            for (int i = bytesWritedInBufferMode; i + _sliceSizeInBytes <= bufferLength; i += _sliceSizeInBytes)
                _sliceIndexes.Add(i);
            
            int index = bufferLength - (bufferLength - bytesWritedInBufferMode) % _sliceSizeInBytes;
            _bufferSecond.Fill(buffer, bufferLength, index, bufferLength - index);
            
            return new AudioBufferSliced(_buffer.InternalArray, buffer, _sliceIndexes);
        }
        
        public List<ArraySegment<byte>> SliceBufferToArraySegments(byte[] buffer, int bufferLength)
        {
            _bufferNormal = buffer;
            int bytesWritedInBufferMode = 0;
            
            if (_buffer.IsFull)
                _buffer.clear();

            int retSize = (bufferLength - bytesWritedInBufferMode) / _sliceSizeInBytes +
                          Convert.ToInt32(_buffer.IsFull);
            List<ArraySegment<byte>> buffersList = new List<ArraySegment<byte>>(retSize);
            
            //swap buffers
            (_buffer, _bufferSecond) = (_bufferSecond, _buffer);
            
            if (_buffer.NotEmpty)
            {
                bytesWritedInBufferMode = _buffer.SizeRemain;
                _buffer.FillToEnd(buffer, bufferLength);
                buffersList.Add(new ArraySegment<byte>(_buffer.InternalArray));
            }

            for (int i = bytesWritedInBufferMode; i + _sliceSizeInBytes <= bufferLength; i += _sliceSizeInBytes)
                buffersList.Add(new ArraySegment<byte>(buffer, i, _sliceSizeInBytes));
            
            int index = bufferLength - (bufferLength - bytesWritedInBufferMode) % _sliceSizeInBytes;
            _bufferSecond.Fill(buffer, bufferLength, index, bufferLength - index);

            return buffersList;
        }
    }

    public class AudioBufferSliced
    {
        public AudioBufferSliced(byte[] buffer, byte[] bufferNormal, List<int> sliceIndexes) =>
            (Buffer, BufferNormal, SliceIndexes) = (buffer, bufferNormal, sliceIndexes);
        
        public byte[] Buffer;
        public List<int> SliceIndexes;
        public byte[] BufferNormal;
    }
    
    public class Buffer<T>
    {
        public T[] InternalArray => _buffer;
        public int Count => _nextElementPointer;
        public int Size => _buffer.Length;
        public int SizeRemain => Size - Count;
        public bool IsEmpty => Count == 0;
        public bool NotEmpty => Count != 0;
        public bool IsFull => Count == Size;
        public void clear() => _nextElementPointer = 0;

        public ref T this[int Index] => ref _buffer[Index];
            
        private T[] _buffer;
        private int _nextElementPointer = 0;
            
        public void Append(in T value)
        {
            if (_nextElementPointer >= _buffer.Length)
                throw new Exception("buffer overflowed");
            _buffer[_nextElementPointer] = value;
            _nextElementPointer++;
        }
        
        public void FillToEnd(T[] Buffer, int BufferLength)
        {
            if (BufferLength < SizeRemain)
                throw new ArgumentException("BufferLength less than remain size");
            Array.Copy(Buffer, 0, _buffer, _nextElementPointer, SizeRemain);
            _nextElementPointer = _buffer.Length;
        }

        public void Fill(T[] Buffer, int BufferLength, int Index, int Length)
        {
            if (Index + Length > BufferLength || Length > SizeRemain)
                throw new ArgumentOutOfRangeException();
            Array.Copy(Buffer, Index, _buffer, _nextElementPointer, Length);
            _nextElementPointer += Length;
        }
        
        public Buffer(int size) => _buffer = new T[size];
    }
    
    public class ByteBuffer
    {
        public byte[] InternalArray => _buffer;
        public int Count => _nextElementPointer;
        public int Size => _buffer.Length;
        public int SizeRemain => Size - Count;
        public bool IsEmpty => Count == 0;
        public bool NotEmpty => Count != 0;
        public bool IsFull => Count == Size;
        public void clear() => _nextElementPointer = 0;

        public ref byte this[int Index] => ref _buffer[Index];
            
        private byte[] _buffer;
        private int _nextElementPointer = 0;
            
        public void Append(in byte value)
        {
            if (_nextElementPointer >= _buffer.Length)
                throw new Exception("buffer overflowed");
            _buffer[_nextElementPointer] = value;
            _nextElementPointer++;
        }
        
        public void FillToEnd(byte[] Buffer, int BufferLength)
        {
            if (BufferLength < SizeRemain)
                throw new ArgumentException("BufferLength less than remain size");
            Array.Copy(Buffer, 0, _buffer, _nextElementPointer, SizeRemain);
            _nextElementPointer = _buffer.Length;
        }

        public void Fill(byte[] Buffer, int BufferLength, int Index, int Length)
        {
            if (Index + Length > BufferLength || Length > SizeRemain)
                throw new ArgumentOutOfRangeException();
            Array.Copy(Buffer, Index, _buffer, _nextElementPointer, Length);
            _nextElementPointer += Length;
        }
        
        public ByteBuffer(int size) => _buffer = new byte[size];
    }

    public unsafe struct HardwareEncoder : IDisposable
    {
        public AVCodec* codec;
        public AVCodecParameters* codecParameters;
        public AVCodecContext* codecContext;
        public AVPacket* packet;
        public int StreamIndex;
        private Streamer _streamer;
        private AVRational _timebase;

        public HardwareEncoder(AVFormatContext* formatContext, AVBufferRef* hwFramesContext, string hardwareEncoderName, Streamer streamer)
        {
            _streamer = streamer;
            codec = null;
            codecParameters = ffmpeg.avcodec_parameters_alloc();
            codecContext = null;
            packet = ffmpeg.av_packet_alloc();
            
            codec = ffmpeg.avcodec_find_encoder_by_name(hardwareEncoderName);
            codecContext = ffmpeg.avcodec_alloc_context3(codec);
            _timebase = formatContext->streams[0]->time_base;
            codecContext->time_base = _timebase;
            codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_D3D11;
            codecContext->width = formatContext->streams[0]->codecpar->width;
            codecContext->height = formatContext->streams[0]->codecpar->height;
            codecContext->max_b_frames = 0;
            codecContext->framerate = ffmpeg.av_guess_frame_rate(null, formatContext->streams[0], null);
            codecContext->hw_frames_ctx = ffmpeg.av_buffer_ref(hwFramesContext);
            ffmpeg.avcodec_open2(codecContext, codec, null);
            ffmpeg.avcodec_parameters_from_context(codecParameters, codecContext);
        }

        public void EncodeAndWriteFrame(AVFrame* hwFrame)
        {
            ffmpeg.avcodec_send_frame(codecContext, hwFrame);
            if (ffmpeg.avcodec_receive_packet(codecContext, packet) != 0)
                return;
            ffmpeg.av_packet_rescale_ts(packet, codecContext->time_base, _streamer.StreamParametersList[StreamIndex].Timebase);
            _streamer.WriteFrame(packet, _timebase);
            Console.WriteLine("frame " + codecContext->frame_number + " writed");
        }

        public void Dispose()
        {
            fixed(AVCodecParameters** p = &codecParameters)
                ffmpeg.avcodec_parameters_free(p);
            ffmpeg.av_packet_unref(packet);
            ffmpeg.avcodec_send_packet(codecContext, packet); //flush codecContext
            fixed (AVCodecContext** p = &codecContext)
                ffmpeg.avcodec_free_context(p);
            fixed(AVPacket** p = &packet)
                ffmpeg.av_packet_free(p);
        }
    }
    
    public unsafe class AudioEncoder : IDisposable
    {
        public int SampleSizeInBytes { get; }
        public int FrameSizeInSamples { get; }
        public int FrameSizeInBytes { get; }
        public int Channels { get; }
        public int StreamIndex { get; }
        public int SampleRate => _sampleRate;
        public AVSampleFormat SampleFormat => _sampleFormat;
        
        private AVFrame* _avFrame;
        private AVCodecContext* _codecContext;
        private AVPacket* _packet;
        private Streamer _streamer;
        private long _pts;
        private AVRational _timebase;
        private AVCodecParameters* _codecParameters;
        private AVSampleFormat _sampleFormat;
        private int _sampleRate;

        public AudioEncoder(Streamer streamer, Encoders encoder, int channels = 2)
        {
            string encoderName = encoder switch
            {
                Encoders.LibOpus => "libopus",
                _ => "libopus"
            };
            
            SampleSizeInBytes = 4; //recorder specified
            _sampleRate = 48000; //encoder specified
            Channels = channels;
            _sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_FLT;
            
            AVCodec* codec = ffmpeg.avcodec_find_encoder_by_name(encoderName);
            _codecContext = ffmpeg.avcodec_alloc_context3(codec);
            _codecContext->sample_rate = _sampleRate;
            _codecContext->sample_fmt = _sampleFormat;
            ffmpeg.av_channel_layout_default(&_codecContext->ch_layout, channels);
            ffmpeg.avcodec_open2(_codecContext, codec, null);
            
            FrameSizeInSamples = _codecContext->frame_size;
            FrameSizeInBytes = FrameSizeInSamples * channels * SampleSizeInBytes;
            
            _packet = ffmpeg.av_packet_alloc();
            _timebase = new AVRational() { num = 1, den = _sampleRate };
            
            _codecParameters = ffmpeg.avcodec_parameters_alloc();
            ffmpeg.avcodec_parameters_from_context(_codecParameters, _codecContext);
            
            
            _avFrame = ffmpeg.av_frame_alloc();
            _avFrame->nb_samples = FrameSizeInSamples;
            ffmpeg.av_channel_layout_default(&_avFrame->ch_layout, channels);
            _avFrame->format = (int)SampleFormat;
            
            _streamer = streamer;
            StreamIndex = _streamer.AddAvStream(_codecParameters, _timebase);
        }

        public void EncodeAndWriteFrame(ArraySegment<byte> buffer)
        {
            fixed(byte* buf = &buffer.Array[buffer.Offset])
                ffmpeg.avcodec_fill_audio_frame(_avFrame, Channels, SampleFormat, buf, FrameSizeInBytes, 1);
            _avFrame->pts = _pts;
            ffmpeg.avcodec_send_frame(_codecContext, _avFrame);
            
            if (ffmpeg.avcodec_receive_packet(_codecContext, _packet) == 0)
            {
                _packet->stream_index = StreamIndex;
                _streamer.WriteFrame(_packet, _timebase);
                ffmpeg.av_packet_unref(_packet);
            }

            _pts += _codecContext->frame_size;
        }

        public void Dispose()
        {
            ffmpeg.av_packet_unref(_packet);
            ffmpeg.avcodec_send_packet(_codecContext, _packet);//flush codecContext
            fixed (AVCodecContext** p = &_codecContext)
                ffmpeg.avcodec_free_context(p);
            fixed(AVPacket** p = &_packet)
                ffmpeg.av_packet_free(p);
        }
    }

    /// Not implemented
    public unsafe struct PixelFormatConverter
    {
        public AVFilterGraph* filterGraph;
        public AVFilterContext* bufferSrcContext;
        public AVFilterContext* bufferSinkContext;
        public AVFilter* bufferSrc;
        public AVFilter* bufferSink;
        public AVFilterInOut* outputs;
        public AVFilterInOut* inputs;

        public PixelFormatConverter()
        {
            throw new NotImplementedException();
            filterGraph = ffmpeg.avfilter_graph_alloc();
            bufferSrcContext = null;
            bufferSinkContext = null;
            bufferSrc = ffmpeg.avfilter_get_by_name("buffer");
            bufferSink = ffmpeg.avfilter_get_by_name("buffersink");
            outputs = ffmpeg.avfilter_inout_alloc();
            inputs = ffmpeg.avfilter_inout_alloc();
        }

        /// <summary>
        /// не реализован
        /// </summary>
        public void Init()
        {
            throw new NotImplementedException();
            //filtering
            //string arg = $"video_size={ddagrab->codecContext->width}x{ddagrab->codecContext->height}:pix_fmt={(long)ddagrab->codecContext->pix_fmt}:time_base={ddagrab->formatContext->streams[0]->time_base.num}/{ddagrab->formatContext->streams[0]->time_base.den}:pixel_aspect={ddagrab->codecContext->sample_aspect_ratio.num}/{ddagrab->codecContext->sample_aspect_ratio.den}";
            //response = ffmpeg.avfilter_graph_create_filter(&pixFmtConv->bufferSrcContext, pixFmtConv->bufferSrc, "in", arg, null, pixFmtConv->filterGraph);
            //response = ffmpeg.avfilter_graph_create_filter(&pixFmtConv->bufferSinkContext, pixFmtConv->bufferSink, "out", null, null, pixFmtConv->filterGraph);
            //pixFmtConv->outputs->name = ffmpeg.av_strdup("in");
            //pixFmtConv->outputs->filter_ctx = pixFmtConv->bufferSrcContext;
            //pixFmtConv->outputs->pad_idx = 0;
            //pixFmtConv->outputs->next = null;
            //pixFmtConv->inputs->name = ffmpeg.av_strdup("out");
            //pixFmtConv->inputs->filter_ctx = pixFmtConv->bufferSinkContext;
            //pixFmtConv->inputs->pad_idx = 0;
            //pixFmtConv->inputs->next = null;
            //response = ffmpeg.av_opt_set_bin(pixFmtConv->bufferSinkContext, "pix_fmts", (byte*)&encoder->codecContext->pix_fmt, sizeof(AVPixelFormat), 1 << 0);
            //response = ffmpeg.avfilter_graph_parse_ptr(pixFmtConv->filterGraph, "format=rgba", &pixFmtConv->inputs, &pixFmtConv->outputs, null);
            //response = ffmpeg.avfilter_graph_config(pixFmtConv->filterGraph, null);
        }
    }

    public struct Codec
    {
        public string UserFriendlyName { get; }
        public string Name { get; }
        public Encoders Encoder { get; }
        public MediaTypes MediaType { get; }

        public Codec(string UserFriendlyName,
            string Name,
            Encoders Encoder,
            MediaTypes MediaType)
        {
            this.UserFriendlyName = UserFriendlyName;
            this.Name = Name;
            this.Encoder = Encoder;
            this.MediaType = MediaType;
        }
    }

    public enum Encoders
    {
        HevcNvenc,
        HevcAmf,
        H264Nvenc,
        H264Amf,
        Av1Nvenc,
        LibOpus
    }

    public enum MediaTypes
    {
        Audio,
        Video
    }
}

//hwframes
//AVBufferRef* hwDeviceContext = null;
//ffmpeg.av_hwdevice_ctx_create(&hwDeviceContext, AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA, null, null, 0);
//AVBufferRef* hwFramesRef = ffmpeg.av_hwframe_ctx_alloc(hwDeviceContext);
//AVHWFramesContext* hwFramesContext = (AVHWFramesContext*)(hwFramesRef->data);
//hwFramesContext->format = AVPixelFormat.AV_PIX_FMT_D3D11;
//hwFramesContext->sw_format = AVPixelFormat.AV_PIX_FMT_BGRA;
//hwFramesContext->width = codecContext->width;
//hwFramesContext->height = codecContext->height;
//ffmpeg.av_hwframe_ctx_init(hwFramesRef);