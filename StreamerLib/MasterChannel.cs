using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Tracing.Parsers.IIS_Trace;
using NAudio.CoreAudioApi;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace StreamerLib;

public class MasterChannel : IDisposable
{
    public const int SampleSizeInBytes = 4;

    //public List<WasapiAudioCapturingChannel> AudioChannels => _audioChannels;
    public int FrameSizeInBytes => _audioEncoder.FrameSizeInBytes;
    public StreamWriter StreamWriter { get; }
    public Encoders Encoder { get; }
    public MasterChannelStates State { get; private set; } = MasterChannelStates.Monitoring;

    private AudioEncoder _audioEncoder;
    private List<WasapiAudioCapturingChannel> _audioChannels = new(2);
    private byte[] _masterBuffer;
    private Thread _mixerThread;
    private ManualResetEvent _manualResetEvent;

    public MasterChannel(StreamWriter streamWriter, Encoders encoder)
    {
        _manualResetEvent = new(false);
        _mixerThread = new (MixingMethod);
        _mixerThread.Start();
        StreamWriter = streamWriter;
        Encoder = encoder;
        _audioEncoder = new(streamWriter, encoder);
        _masterBuffer = new byte[_audioEncoder.FrameSizeInBytes];
    }


    public WasapiAudioCapturingChannel? AddChannel(MMDevice device)
    {
        if (State == MasterChannelStates.Streaming)
            return null;
        var channel = new WasapiAudioCapturingChannel(device, _audioEncoder.FrameSizeInBytes);

        if (channel.WaveFormat.SampleRate != _audioEncoder.SampleRate)
            throw new Exception("sample rate must be 48000");

        _audioChannels.Add(channel);
        channel.DataAvailable += ReceiveBuffer;
        return channel;
    }

    public void RemoveChannel(WasapiAudioCapturingChannel capturingChannel)
    {
        if (!_audioChannels.Contains(capturingChannel))
            throw new ArgumentException("MasterChannel not contains this WASAPIAudioCapturingChannel");

        capturingChannel.DataAvailable -= ReceiveBuffer;
        _audioChannels.Remove(capturingChannel);
    }

    internal void StartStreaming()
    {
        for (int i = _audioChannels.Count - 1; i >= 0; i--)
            if (_audioChannels[i].CaptureState == CaptureState.Stopped)
                _audioChannels[i].StartRecording();
        //foreach (var channel in _audioChannels)
        //    if (channel.CaptureState == CaptureState.Stopped)
        //        channel.StartRecording();
        State = MasterChannelStates.Streaming;
    }
    
    internal void StopStreaming()
    {
        State = MasterChannelStates.Monitoring;
    }

    private void ReceiveBuffer(object? sender, EventArgs e)
    {
        if (State != MasterChannelStates.Streaming)
            return;

        _manualResetEvent.Set();
        //while (_audioChannels[0].BufferIsAvailable)
        //    writer0.Write(_audioChannels[0].ReadNextBuffer());

        //while (_audioChannels[1].BufferIsAvailable)
        //    writer1.Write(_audioChannels[1].ReadNextBuffer());

    }

    BinaryWriter writer0 = new BinaryWriter(File.Open(@"C:\Users\Cray\Desktop\St\raw0", FileMode.Create));
    BinaryWriter writer1 = new BinaryWriter(File.Open(@"C:\Users\Cray\Desktop\St\raw1", FileMode.Create));
    //Stopwatch stopwatch = Stopwatch.StartNew();

    private unsafe void MixingMethod()
    {
        while (true)
        {
            _manualResetEvent.WaitOne();
            //Console.WriteLine($"WaitOne");

            while (AllBuffersIsAvailable())
            {
                //Test();
                Mix();
            }
            
            _manualResetEvent.Reset();
        }

        void Test()
        {
            //long time = stopwatch.ElapsedMilliseconds;
            writer0.Write(_audioChannels[0].ReadNextBuffer());
            writer1.Write(_audioChannels[1].ReadNextBuffer());
            //time = stopwatch.ElapsedMilliseconds - time;
            //Console.WriteLine(time);
        }

        void Mix()
        {
            var readedBuffer = _audioChannels[0].ReadNextBuffer();
            if (readedBuffer.Count != _masterBuffer.Length)
                throw new Exception();
            Array.Copy(readedBuffer.Array, readedBuffer.Offset, _masterBuffer, 0, _masterBuffer.Length);

            writer0.Write(readedBuffer);

            fixed (byte* ptr1 = _masterBuffer)
            {
                float* masterBufferFloat = (float*)ptr1;

                for (int i = 1; i < _audioChannels.Count; i++)
                {
                    var buffer = _audioChannels[i].ReadNextBuffer();

                    writer1.Write(buffer);

                    fixed (byte* ptr2 = &buffer.Array[buffer.Offset])
                    {
                        float* bufferFloat = (float*)ptr2;
                        AddBuffer(masterBufferFloat, bufferFloat, _masterBuffer.Length / 4);
                    }
                }

                //ClipBuffer(masterBufferFloat, _masterBuffer.Length / 4);

                _audioEncoder.EncodeAndWriteFrame(_masterBuffer);
            }

            void AddBuffer(float* destonation, float* source, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    destonation[i] += source[i];
                }
            }

            void ClipBuffer(float* buffer, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    if (buffer[i] > 1f)
                        buffer[i] = 1f;
                    else if (buffer[i] < -1f)
                        buffer[i] = -1f;
                }
            }
        }
    }

    private bool AllBuffersIsAvailable()
    {
        foreach (var channel in _audioChannels)
            if (channel.BufferIsAvailable == false) 
                return false;
        return true;
    }

    public void Dispose()
    {
        Console.WriteLine("Dispose");
        _audioChannels.ForEach(c => c.StopRecording());
    }
}

public enum MasterChannelStates
{
    Monitoring,
    Streaming
}