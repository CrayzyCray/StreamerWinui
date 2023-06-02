namespace StreamerLib;

internal sealed class Buffer<T>
{
    public T[] Array => _buffer;
    public int Buffered => _nextElementPointer;
    public int Size => _buffer.Length;
    public int SizeRemain => Size - Buffered;
    public bool IsEmpty => Buffered == 0;
    public bool IsNotEmpty => Buffered != 0;
    public bool IsFull => Buffered == Size;

    public ref T this[int index] => ref _buffer[index];
        
    private T[] _buffer;
    private int _nextElementPointer;

    public void Clear()
    {
        _nextElementPointer = 0;
    }

    public void Append(in T value)
    {
        if (_nextElementPointer >= _buffer.Length)
            throw new Exception("buffer overflowed");
        _buffer[_nextElementPointer] = value;
        _nextElementPointer++;
    }
    
    
    /// <returns>Count of buffered items</returns>
    public int FillToEnd(T[] buffer, int bufferLength)
    {
        if (bufferLength < SizeRemain)
            throw new ArgumentException("BufferLength less than need for fill");
        int ret = SizeRemain;
        System.Array.Copy(buffer, 0, _buffer, _nextElementPointer, SizeRemain);
        _nextElementPointer = _buffer.Length;
        return ret;
    }

    public void Fill(T[] buffer, int bufferLength, int startIndex, int length)
    {
        if (startIndex + length > bufferLength || length > SizeRemain)
            throw new ArgumentOutOfRangeException();
        System.Array.Copy(buffer, startIndex, _buffer, _nextElementPointer, length);
        _nextElementPointer += length;
    }

    public Buffer(int size)
    {
        _buffer = new T[size];
    }
}
