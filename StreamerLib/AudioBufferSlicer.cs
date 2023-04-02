namespace StreamerLib;

public class AudioBufferSlicer
{
    public int SliceSizeInBytes { get; }

    private Buffer<byte> _buffer;

    public AudioBufferSlicer(int sizeInBytes)
    {
        SliceSizeInBytes = sizeInBytes;
        _buffer = new(SliceSizeInBytes);
    }

    public List<byte[]> SliceBufferToArraySegments(byte[] buffer, int bufferLength)
    {
        int bytesWrittenInBufferMode = _buffer.IsNotEmpty ? _buffer.SizeRemain : 0;
        int retSize = (bufferLength - bytesWrittenInBufferMode) / SliceSizeInBytes + 1;
        List<byte[]> segmentsList = new(retSize);

        if (bytesWrittenInBufferMode != 0)
        {
            _buffer.FillToEnd(buffer, bufferLength);
            segmentsList.Add(_buffer.Array);
        }

        for (int i = bytesWrittenInBufferMode; i + SliceSizeInBytes <= bufferLength; i += SliceSizeInBytes)
        {
            byte[] buf = new byte[SliceSizeInBytes];
            Array.Copy(buffer, i, buf, 0, buf.Length);
            segmentsList.Add(buf);
        }


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