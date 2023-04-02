using System.Collections.Generic;

namespace StreamerLib;

public class AudioBufferSlicer
{
    public int SliceSizeInBytes { get; }
    public bool BufferIsFull => _buffer.IsFull;

    private Buffer<byte> _buffer;
    private Buffer<byte> _bufferSecond;

    public AudioBufferSlicer(int sizeInBytes)
    {
        SliceSizeInBytes = sizeInBytes;
        _buffer = new(SliceSizeInBytes);
        _bufferSecond = new(SliceSizeInBytes);
    }

    public List<ArraySegment<byte>> SliceBufferToArraySegments(byte[] buffer, int bufferLength)
    {
        (_buffer, _bufferSecond) = (_bufferSecond, _buffer);

        int bytesWrittenInBufferMode = _buffer.IsNotEmpty ? _buffer.SizeRemain : 0;
        int retSize = (bufferLength - bytesWrittenInBufferMode) / SliceSizeInBytes + 1-1;
        List<ArraySegment<byte>> segmentsList = new List<ArraySegment<byte>>(retSize);

        if (_buffer.IsNotEmpty)
        {
            _buffer.FillToEnd(buffer, bufferLength);
            segmentsList.Add(new ArraySegment<byte>(_buffer.Array));
        }

        for (int i = bytesWrittenInBufferMode; i + SliceSizeInBytes <= bufferLength; i += SliceSizeInBytes)
            segmentsList.Add(new ArraySegment<byte>(buffer, i, SliceSizeInBytes));

        int indexOfLastBufferedSample = bufferLength - (bufferLength - bytesWrittenInBufferMode) % SliceSizeInBytes;

        if (_bufferSecond.IsNotEmpty)
            _bufferSecond = new(SliceSizeInBytes);

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

public class AudioBufferSlicer2
{
    public int SliceSizeInBytes { get; }
    public bool LastHasUsingBuffer { get; private set; }

    private Buffer<byte> _buffer;

    public AudioBufferSlicer2(int sizeInBytes)
    {
        SliceSizeInBytes = sizeInBytes;
        _buffer = new(SliceSizeInBytes);
    }

    public List<ArraySegment<byte>> SliceBufferToArraySegments(byte[] bufferr, int bufferLength)
    {
        byte[] buffer = new byte[bufferLength];
        Array.Copy(bufferr, buffer, bufferLength);

        int bytesWrittenInBufferMode = _buffer.IsNotEmpty ? _buffer.SizeRemain : 0;
        int retSize = (bufferLength - bytesWrittenInBufferMode) / SliceSizeInBytes + 1;
        List<ArraySegment<byte>> segmentsList = new List<ArraySegment<byte>>(retSize);

        if (bytesWrittenInBufferMode != 0)
        {
            _buffer.FillToEnd(buffer, bufferLength);
            segmentsList.Add(new ArraySegment<byte>(_buffer.Array));
            LastHasUsingBuffer = true;
        }
        else
        {
            LastHasUsingBuffer = false;
        }

        for (int i = bytesWrittenInBufferMode; i + SliceSizeInBytes <= bufferLength; i += SliceSizeInBytes)
            segmentsList.Add(new ArraySegment<byte>(buffer, i, SliceSizeInBytes));


        int indexOfLastBufferedSample = bufferLength - (bufferLength - bytesWrittenInBufferMode) % SliceSizeInBytes;

        if (indexOfLastBufferedSample < bufferLength)
        {
            _buffer = new(SliceSizeInBytes);
            _buffer.Fill(buffer, bufferLength, indexOfLastBufferedSample, bufferLength - indexOfLastBufferedSample);
        }

        return segmentsList;
    }

    public void Clear()
    {
        _buffer = new(SliceSizeInBytes);
    }
}