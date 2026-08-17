using NAudio.Wave;
using ScaleTrainer.App.Services.Audio;
using System.Diagnostics;

namespace ScaleTrainer.App.Services;

/// <summary>
/// Manages WASAPI/WaveOut output and the simple synth.
/// Provides NoteOn / NoteOff / TriggerErrorTone methods that are safe to call
/// from the MIDI thread.
/// </summary>
public sealed class AudioEngine : IDisposable
{
    private readonly SimpleSynth _synth;
    private IWavePlayer? _player;
    private BufferedWaveProvider? _bufferProvider;
    private readonly WaveFormat _waveFormat;
    private bool _disposed;
    private Thread? _fillThread;
    private volatile bool _running;

    public bool IsRunning => _running;
    public int SampleRate => _synth.SampleRate;

    public AudioEngine(int sampleRate = 44100)
    {
        _synth = new SimpleSynth(sampleRate, maxVoices: 12);
        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
    }

    public void Start()
    {
        if (_running) return;

        try
        {
            // Use WaveOutEvent for maximum compatibility on Windows 10/11.
            // (WASAPI exclusive can be added later as an option.)
            _player = new WaveOutEvent
            {
                DesiredLatency = 50,          // ms – good balance of latency vs stability
                NumberOfBuffers = 2
            };

            _bufferProvider = new BufferedWaveProvider(_waveFormat)
            {
                BufferDuration = TimeSpan.FromMilliseconds(100),
                DiscardOnBufferOverflow = true
            };

            _player.Init(_bufferProvider);
            _player.Play();

            _running = true;
            _fillThread = new Thread(FillBufferLoop)
            {
                IsBackground = true,
                Name = "ScaleTrainer.AudioFill",
                Priority = ThreadPriority.AboveNormal
            };
            _fillThread.Start();

            Debug.WriteLine("AudioEngine started");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start AudioEngine: {ex.Message}");
            Stop();
            throw;
        }
    }

    public void Stop()
    {
        _running = false;

        if (_fillThread is not null)
        {
            _fillThread.Join(200);
            _fillThread = null;
        }

        _player?.Stop();
        _player?.Dispose();
        _player = null;
        _bufferProvider = null;

        _synth.AllNotesOff();
        Debug.WriteLine("AudioEngine stopped");
    }

    public void NoteOn(int noteNumber, int velocity)
    {
        float vel = velocity / 127f;
        _synth.NoteOn(noteNumber, vel);
    }

    public void NoteOff(int noteNumber)
    {
        _synth.NoteOff(noteNumber);
    }

    public void TriggerErrorTone()
    {
        _synth.TriggerErrorTone();
    }

    public void AllNotesOff()
    {
        _synth.AllNotesOff();
    }

    private void FillBufferLoop()
    {
        // Pre-allocate a reusable buffer so the loop stays allocation-free
        const int framesPerBlock = 256;
        float[] mixBuffer = new float[framesPerBlock * 2]; // stereo
        byte[] byteBuffer = new byte[framesPerBlock * 2 * 4]; // float32

        while (_running)
        {
            if (_bufferProvider is null) break;

            // Keep a modest amount of audio buffered
            int bytesNeeded = _bufferProvider.BufferLength / 4; // aim to keep buffer reasonably full
            if (_bufferProvider.BufferedBytes > bytesNeeded)
            {
                Thread.Sleep(2);
                continue;
            }

            _synth.Render(mixBuffer, 0, framesPerBlock);

            // Convert float samples to bytes
            Buffer.BlockCopy(mixBuffer, 0, byteBuffer, 0, byteBuffer.Length);

            try
            {
                _bufferProvider.AddSamples(byteBuffer, 0, byteBuffer.Length);
            }
            catch
            {
                // Buffer overflow or disposed – ignore
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }
}
