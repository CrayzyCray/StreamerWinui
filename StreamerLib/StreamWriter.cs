using System.Net;

namespace StreamerLib
{
    public unsafe class StreamWriter : IDisposable
    {
        public const int DefaultPort = 10000;
        public StreamParameters GetStreamParameters(int streamIndex) => _streamParameters[streamIndex];
        public StreamClient GetStreamClient(int id) => _streamClientsList[id];

        private StreamParameters[] _streamParameters = new StreamParameters[0];
        private List<StreamClient> _streamClientsList = new();

        /// <returns>stream index</returns>
        public int AddAvStream(nint codecParameters, nint timebase)
        {
            Array.Resize(ref _streamParameters, _streamParameters.Length + 1);
            _streamParameters[_streamParameters.Length] = new StreamParameters { CodecParameters = codecParameters, Timebase = timebase };

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
            fixed (StreamParameters* ptr = _streamParameters)
                FFmpegImport.StreamWriter_AddClient(
                    outputUrl,
                    &formatContext,
                    ptr,
                    _streamParameters.Length);

            _streamClientsList.Add(new StreamClient() { FormatContext = formatContext, IP = ipAddress, Port = port });

            return true;
        }

        public bool AddClientAsFile(string path)
        {
            if (_streamParameters.Length == 0)
                return false;

            nint formatContext;
            fixed (StreamParameters* ptr = _streamParameters)
                FFmpegImport.StreamWriter_AddClientAsFile(
                    path,
                    &formatContext,
                    ptr,
                    _streamParameters.Length);

            _streamClientsList.Add(new StreamClient() { FormatContext = formatContext, IsFile = true });

            return true;
        }

        public void Stop()
        {
            DeleteAllClients();
            DeleteStreamParameters();
        }

        private void DeleteAllClients()
        {
            foreach (var fc in _streamClientsList)
                FFmpegImport.StreamWriter_DeleteAllClients(fc.FormatContext);

            _streamClientsList.Clear();
        }

        private void DeleteStreamParameters() =>
            _streamParameters = Array.Empty<StreamParameters>();

        public int WriteFrame(nint packet, nint packetTimebase, int streamIndex)
        {
            nint[] formatContexts = new nint[_streamClientsList.Count];
            for (int i = 0; i < _streamClientsList.Count; i++)
                formatContexts[i] = _streamClientsList[i].FormatContext;

            int ret = FFmpegImport.StreamWriter_WriteFrame(
                packet,
                packetTimebase,
                _streamParameters[streamIndex].Timebase,
                formatContexts);

            return ret;
        }

        public void Dispose()
        {
            DeleteAllClients();
        }

        public class StreamClient
        {
            public IPAddress IP;
            public int Port;
            public nint FormatContext;
            public bool IsFile;
        }

        public struct StreamParameters
        {
            public nint CodecParameters;
            public nint Timebase;
        }
    }
}