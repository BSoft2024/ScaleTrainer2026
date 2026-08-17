namespace ScaleTrainer.App.Services.Audio;

/// <summary>
/// Single monophonic voice: simple oscillator + ADSR envelope.
/// Designed to be allocation-free once created.
/// </summary>
internal sealed class SynthVoice
{
    private enum Stage { Idle, Attack, Decay, Sustain, Release }

    private Stage _stage = Stage.Idle;
    private double _phase;
    private double _envelope;
    private double _frequency;
    private float _velocity;
    private int _noteNumber = -1;

    // Envelope settings (seconds)
    private const double AttackTime = 0.01;
    private const double DecayTime = 0.12;
    private const double SustainLevel = 0.65;
    private const double ReleaseTime = 0.25;

    private double _attackInc;
    private double _decayInc;
    private double _releaseInc;

    public bool IsActive => _stage != Stage.Idle;
    public int NoteNumber => _noteNumber;

    public void NoteOn(int noteNumber, float velocity, int sampleRate)
    {
        _noteNumber = noteNumber;
        _frequency = 440.0 * Math.Pow(2.0, (noteNumber - 69) / 12.0);
        _velocity = Math.Clamp(velocity, 0.01f, 1f);
        _phase = 0;
        _envelope = 0;
        _stage = Stage.Attack;

        _attackInc = 1.0 / (AttackTime * sampleRate);
        _decayInc = (1.0 - SustainLevel) / (DecayTime * sampleRate);
        _releaseInc = SustainLevel / (ReleaseTime * sampleRate);
    }

    public void NoteOff()
    {
        if (_stage != Stage.Idle && _stage != Stage.Release)
            _stage = Stage.Release;
    }

    public void ForceStop()
    {
        _stage = Stage.Idle;
        _envelope = 0;
        _noteNumber = -1;
    }

    /// <summary>
    /// Generates the next sample. Returns 0 when idle.
    /// </summary>
    public float Process(int sampleRate)
    {
        if (_stage == Stage.Idle)
            return 0f;

        // Advance envelope
        switch (_stage)
        {
            case Stage.Attack:
                _envelope += _attackInc;
                if (_envelope >= 1.0)
                {
                    _envelope = 1.0;
                    _stage = Stage.Decay;
                }
                break;

            case Stage.Decay:
                _envelope -= _decayInc;
                if (_envelope <= SustainLevel)
                {
                    _envelope = SustainLevel;
                    _stage = Stage.Sustain;
                }
                break;

            case Stage.Sustain:
                // hold
                break;

            case Stage.Release:
                _envelope -= _releaseInc;
                if (_envelope <= 0.0)
                {
                    _envelope = 0;
                    _stage = Stage.Idle;
                    _noteNumber = -1;
                    return 0f;
                }
                break;
        }

        // Simple band-limited-ish saw + square mix for a pleasant tone
        double phaseInc = _frequency / sampleRate;
        _phase += phaseInc;
        if (_phase >= 1.0) _phase -= 1.0;

        // Saw
        double saw = _phase * 2.0 - 1.0;
        // Square
        double square = _phase < 0.5 ? 1.0 : -1.0;

        float sample = (float)((0.6 * saw + 0.4 * square) * _envelope * _velocity * 0.25);
        return sample;
    }
}
