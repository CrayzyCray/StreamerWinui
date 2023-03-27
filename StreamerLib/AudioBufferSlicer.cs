namespace StreamerLib;

internal class AudioBufferSlicer
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

internal class AudioBufferSlicer2
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

    public AudioBufferSlicer2(int SliceSizeInSamples, int SampleSizeInBytes, int Channels)
    {
        _sliceSizeInBytes = SliceSizeInSamples * SampleSizeInBytes * Channels;
        AllocateBuffers(_sliceSizeInBytes);
    }

    public AudioBufferSlicer2(int sizeInBytes)
    {
        _sliceSizeInBytes = sizeInBytes;
        AllocateBuffers(_sliceSizeInBytes);
    }

    private void AllocateBuffers(int size)
    {
        _buffer = new(size);
        _bufferSecond = new(size);
    }

    public List<ArraySegment<byte>> SliceBufferToArraySegments(byte[] buffer, int bufferLength)
    {
        _originalArray = buffer;
        int bytesWritedInBufferMode = 0;

        //swap buffers
        (_buffer, _bufferSecond) = (_bufferSecond, _buffer);

        if (_bufferSecond.IsFull)
            _bufferSecond = new(_sliceSizeInBytes);


        int retSize = (bufferLength - bytesWritedInBufferMode) / _sliceSizeInBytes + 1;//+1 for buffer
        List<ArraySegment<byte>> segmentsList = new List<ArraySegment<byte>>(retSize);


        if (_buffer.NotEmpty)
        {
            bytesWritedInBufferMode = _buffer.FillToEnd(buffer, bufferLength);
            segmentsList.Add(new ArraySegment<byte>(_buffer.Array));
        }

        for (int i = bytesWritedInBufferMode; i + _sliceSizeInBytes <= bufferLength; i += _sliceSizeInBytes)
            segmentsList.Add(new ArraySegment<byte>(buffer, i, _sliceSizeInBytes));

        int indexOfLastBufferedSample = bufferLength - (bufferLength - bytesWritedInBufferMode) % _sliceSizeInBytes;

        if (indexOfLastBufferedSample < bufferLength)
            _bufferSecond.Fill(buffer, bufferLength, indexOfLastBufferedSample, bufferLength - indexOfLastBufferedSample);

        return segmentsList;
    }

    public void Clear()
    {
        _buffer.Clear();
        _bufferSecond.Clear();
        _originalArray = Array.Empty<byte>();
    }
}