namespace ScaleTrainer.App.Services.Audio;

/// <summary>
/// Very lightweight polyphonic synth with a fixed voice pool.
/// All processing is allocation-free after construction.
/// </summary>
internal sealed class SimpleSynth
{
    private readonly SynthVoice[] _voices;
    private readonly ErrorToneGenerator _errorTone = new();
    private readonly object _lock = new();   // only used for NoteOn/NoteOff from MIDI thread

    public int SampleRate { get; }
    public int MaxVoices => _voices.Length;

    public SimpleSynth(int sampleRate = 44100, int maxVoices = 12)
    {
        SampleRate = sampleRate;
        _voices = new SynthVoice[maxVoices];
        for (int i = 0; i < maxVoices; i++)
            _voices[i] = new SynthVoice();
    }

    public void NoteOn(int noteNumber, float velocity)
    {
        lock (_lock)
        {
            // Try to find a free voice or the quietest one
            SynthVoice? free = null;
            SynthVoice? lowest = null;

            foreach (var v in _voices)
            {
                if (!v.IsActive)
                {
                    free = v;
                    break;
                }
                // Prefer stealing the same note if it is still sounding
                if (v.NoteNumber == noteNumber)
                {
                    free = v;
                    break;
                }
            }

            var voice = free ?? _voices[0]; // simple steal of first if all busy
            voice.NoteOn(noteNumber, velocity, SampleRate);
        }
    }

    public void NoteOff(int noteNumber)
    {
        lock (_lock)
        {
            foreach (var v in _voices)
            {
                if (v.IsActive && v.NoteNumber == noteNumber)
                    v.NoteOff();
            }
        }
    }

    public void TriggerErrorTone()
    {
        _errorTone.Trigger(SampleRate);
    }

    public void AllNotesOff()
    {
        lock (_lock)
        {
            foreach (var v in _voices)
                v.ForceStop();
        }
    }

    /// <summary>
    /// Mixes all active voices + error tone into the provided stereo buffer.
    /// Called from the audio callback – must stay allocation-free.
    /// </summary>
    public void Render(float[] buffer, int offset, int samples)
    {
        // Clear the region we will write
        for (int i = 0; i < samples * 2; i++)
            buffer[offset + i] = 0f;

        // Render voices (no lock needed if we accept rare race; for safety we could lock,
        // but for lowest latency we keep it unlocked and rely on atomic stage changes)
        foreach (var voice in _voices)
        {
            if (!voice.IsActive) continue;

            for (int i = 0; i < samples; i++)
            {
                float s = voice.Process(SampleRate);
                int idx = offset + i * 2;
                buffer[idx] += s;       // left
                buffer[idx + 1] += s;   // right
            }
        }

        // Error tone
        if (_errorTone.IsActive)
        {
            for (int i = 0; i < samples; i++)
            {
                float s = _errorTone.Process(SampleRate);
                int idx = offset + i * 2;
                buffer[idx] += s;
                buffer[idx + 1] += s;
            }
        }
    }
}
