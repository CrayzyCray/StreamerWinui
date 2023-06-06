using System.Net;
using System.Runtime.InteropServices;

namespace StreamerLib;
public unsafe sealed class StreamWriter : IDisposable
{
    public const int DefaultPort = 10000;

    private StreamParameters[] _streamParameters = new StreamParameters[0];
    private List<StreamClient> _streamClientsList = new();

    /// <returns>stream index</returns>
    public int AddAvStream(nint codecParameters, nint timebase)
    {
        Array.Resize(ref _streamParameters, _streamParameters.Length + 1);
        _streamParameters[_streamParameters.Length-1] = new StreamParameters { CodecParameters = codecParameters, Timebase = timebase };

        return _streamParameters.Length - 1; //this is a StreamIndex
    }

    public bool AddClient(IPAddress ipAddress, int port = DefaultPort)
    {
        if (port is < 0 or > 65535)
            return false;

        if (_streamParameters.Length == 0)
            return false;

        string outputUrl = $"rist://{ipAddress}:{port}";

        nint formatContext;
        fixed (StreamParameters* parameters = _streamParameters)
            formatContext = LibUtil.stream_writer_add_client(outputUrl, parameters, _streamParameters.Length);
        byte[] ip = new byte[4];

        _streamClientsList.Add(new StreamClient() { FormatContext = formatContext, /*IP = ipAddress.GetAddressBytes().,*/ Port = port });

        return true;
    }

    public bool AddClientAsFile(string path)
    {
        if (_streamParameters.Length == 0)
            return false;

        nint formatContext;
        fixed (StreamParameters* parameters = _streamParameters)
            formatContext = LibUtil.stream_writer_add_client_as_file(
                path,
                parameters,
                _streamParameters.Length);

        _streamClientsList.Add(new StreamClient() { FormatContext = formatContext, IsFile = true });

        return true;
    }

    public void Clear()
    {
        DeleteAllClients();
        DeleteStreamParameters();
    }

    private void DeleteAllClients()
    {
        foreach (var fc in _streamClientsList)
            LibUtil.stream_writer_close_format_context(fc.FormatContext);

        _streamClientsList.Clear();
    }

    private void DeleteStreamParameters() =>
        _streamParameters = Array.Empty<StreamParameters>();

    public int WriteFrame(nint packet, nint packetTimebase, int streamIndex)
    {
        if (_streamClientsList.Count == 0)
            return 0;

        nint[] formatContexts = new nint[_streamClientsList.Count];
        for (int i = 0; i < _streamClientsList.Count; i++)
            formatContexts[i] = _streamClientsList[i].FormatContext;

        int ret;
        fixed (StreamClient* ptr = _streamClientsList.ToArray())
            ret = LibUtil.stream_writer_write_packet(packet, packetTimebase, ptr, formatContexts.Length);

        return ret;
    }

    public void Dispose()
    {
        DeleteAllClients();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StreamClient
    {
        public fixed byte IP[4];
        public UInt16 Port;
        public nint FormatContext;
        public bool IsFile;
    }

    public struct StreamParameters
    {
        public nint CodecParameters;
        public nint Timebase;
    }
}