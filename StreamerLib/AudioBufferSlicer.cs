namespace StreamerLib;

public class AudioBufferSlicer
{
    public byte[] Buffer => _buffer.Array;
    public byte[] OriginalArray => _originalArray;
    public bool BufferIsFull => _buffer.IsFull;
    public int BufferedCount => _bufferSecond.Buffered;
    public int SliceSizeInBytes => _sliceSizeInBytes;
    
    private Buffer<byte> _buffer;
    private Buffer<byte> _bufferSecond;
    private int _sliceSizeInBytes;
    private byte[] _originalArray = Array.Empty<byte>();
    
    public AudioBufferSlicer(int SliceSizeInSamples, int SampleSizeInBytes, int Channels)
    {
        int sizeInBytes = SliceSizeInSamples * SampleSizeInBytes * Channels;
        _buffer = new(sizeInBytes);
        _bufferSecond = new(sizeInBytes);
        _sliceSizeInBytes = sizeInBytes;
    }
    
    public AudioBufferSlicer(int sizeInBytes)
    {
        _buffer = new(sizeInBytes);
        _bufferSecond = new(sizeInBytes);
        _sliceSizeInBytes = sizeInBytes;
    }
    
    public List<ArraySegment<byte>> SliceBufferToArraySegments(byte[] buffer, int bufferLength)
    {
        _originalArray = buffer;
        int bytesWritedInBufferMode = 0;
        
        if (_buffer.IsFull)
            _buffer.Clear();

        int retSize = (bufferLength - bytesWritedInBufferMode) / _sliceSizeInBytes +
                      Convert.ToInt32(_buffer.IsFull);
        List<ArraySegment<byte>> buffersList = new List<ArraySegment<byte>>(retSize);
        
        //swap buffers
        (_buffer, _bufferSecond) = (_bufferSecond, _buffer);
        
        if (_buffer.NotEmpty)
        {
            bytesWritedInBufferMode = _buffer.SizeRemain;
            _buffer.FillToEnd(buffer, bufferLength);
            buffersList.Add(new ArraySegment<byte>(_buffer.Array));
        }

        for (int i = bytesWritedInBufferMode; i + _sliceSizeInBytes <= bufferLength; i += _sliceSizeInBytes)
            buffersList.Add(new ArraySegment<byte>(buffer, i, _sliceSizeInBytes));
        
        int index = bufferLength - (bufferLength - bytesWritedInBufferMode) % _sliceSizeInBytes;
        _bufferSecond.Fill(buffer, bufferLength, index, bufferLength - index);
        
        return buffersList;
    }

    public void Clear()
    {
        _buffer.Clear();
        _bufferSecond.Clear();
        _originalArray = Array.Empty<byte>();
    }
}

public class AudioBufferSlicer2
{
    public int SliceSizeInBytes { get; }

    private Buffer<byte> _buffer;
    private Buffer<byte> _bufferSecond;

    public AudioBufferSlicer2(int sizeInBytes)
    {
        SliceSizeInBytes = sizeInBytes;
        _buffer = new(SliceSizeInBytes);
        _bufferSecond = new(SliceSizeInBytes);
    }

    public List<ArraySegment<byte>> SliceBufferToArraySegments(byte[] buffer, int bufferLength)
    {
        (_buffer, _bufferSecond) = (_bufferSecond, _buffer);

        if (_bufferSecond.IsFull)
            _bufferSecond = new(SliceSizeInBytes);

        int bytesWrittenInBufferMode = (_buffer.NotEmpty) ? _buffer.SizeRemain : 0;
        int retSize = (bufferLength - bytesWrittenInBufferMode) / SliceSizeInBytes + 1;
        List<ArraySegment<byte>> segmentsList = new List<ArraySegment<byte>>(retSize);

        if (_buffer.NotEmpty)
        {
            _buffer.FillToEnd(buffer, bufferLength);
            segmentsList.Add(new ArraySegment<byte>(_buffer.Array));
        }

        for (int i = bytesWrittenInBufferMode; i + SliceSizeInBytes <= bufferLength; i += SliceSizeInBytes)
            segmentsList.Add(new ArraySegment<byte>(buffer, i, SliceSizeInBytes));

        int indexOfLastBufferedSample = bufferLength - (bufferLength - bytesWrittenInBufferMode) % SliceSizeInBytes;

        if (indexOfLastBufferedSample < bufferLength)
            _bufferSecond.Fill(buffer, bufferLength, indexOfLastBufferedSample, bufferLength - indexOfLastBufferedSample);

        return segmentsList;
    }

    public void Clear()
    {
        _buffer = new(SliceSizeInBytes);
        _bufferSecond = new(SliceSizeInBytes);
    }
}