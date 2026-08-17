namespace ScaleTrainer.App.Services.Audio;

/// <summary>
/// Generates a short, distinctive error tone (descending pitch).
/// Completely self-contained and allocation-free after construction.
/// </summary>
internal sealed class ErrorToneGenerator
{
    private bool _active;
    private double _phase;
    private double _frequency;
    private double _envelope;
    private int _samplesRemaining;
    private int _totalSamples;

    private const double StartFreq = 880.0;   // A5
    private const double EndFreq = 220.0;     // A3
    private const double DurationSeconds = 0.18;
    private const double Volume = 0.35;

    public bool IsActive => _active;

    public void Trigger(int sampleRate)
    {
        _active = true;
        _phase = 0;
        _frequency = StartFreq;
        _envelope = 1.0;
        _totalSamples = (int)(DurationSeconds * sampleRate);
        _samplesRemaining = _totalSamples;
    }

    public float Process(int sampleRate)
    {
        if (!_active)
            return 0f;

        // Linear frequency sweep downward
        double t = 1.0 - ((double)_samplesRemaining / _totalSamples);
        _frequency = StartFreq + (EndFreq - StartFreq) * t;

        // Simple exponential-ish decay
        _envelope = 1.0 - t;
        _envelope *= _envelope; // faster decay near the end

        double phaseInc = _frequency / sampleRate;
        _phase += phaseInc;
        if (_phase >= 1.0) _phase -= 1.0;

        float sample = (float)(Math.Sin(_phase * Math.PI * 2.0) * _envelope * Volume);

        _samplesRemaining--;
        if (_samplesRemaining <= 0)
            _active = false;

        return sample;
    }
}
