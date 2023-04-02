namespace StreamerLib;

internal class Buffer<T>
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
    private int _nextElementPointer = 0;

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

/*
public static class FFmpegHelper
{
    public static unsafe void ErrStrPrint(int errNum)
    {
        int maxErrorStringSize = 64;
        byte[] str1 = new byte[maxErrorStringSize];
        fixed (byte* str2 = str1)
        {
            ffmpeg.av_make_error_string(str2, 64, errNum);
            DebugLogUnmanagedPtr(str2);
        }
    }
    
    public static void InicializeFFmpeg()
    {
        SetFfmpegBinaresPath(@"C:\Users\Cray\Desktop\Programs\ffmpeg");
        DynamicallyLoadedBindings.Initialize();
    }

    public static unsafe void DebugLogUnmanagedPtr(byte* ptr) =>
        Debug.WriteLine(Marshal.PtrToStringAnsi((IntPtr)ptr));

    public static unsafe string UnmanagedPtrToString(byte* ptr) =>
        Marshal.PtrToStringAnsi((IntPtr)ptr) ?? String.Empty;

    static void SetFfmpegBinaresPath()=>
        DynamicallyLoadedBindings.LibrariesPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin");

    static void SetFfmpegBinaresPath(string path) =>
        DynamicallyLoadedBindings.LibrariesPath = path; 
}
*/


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