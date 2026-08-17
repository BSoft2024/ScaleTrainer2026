using System.Speech.Synthesis;
using System.Diagnostics;

namespace ScaleTrainer.App.Services;

/// <summary>
/// Thin wrapper around System.Speech.Synthesis.
/// All speech is performed asynchronously so it never blocks the MIDI or audio threads.
/// </summary>
public sealed class SpeechService : IDisposable
{
    private readonly SpeechSynthesizer _synth;
    private readonly object _lock = new();
    private bool _disposed;

    public SpeechService()
    {
        _synth = new SpeechSynthesizer();
        _synth.SetOutputToDefaultAudioDevice();

        // Sensible defaults for a teaching app
        _synth.Rate = 0;          // -10 .. +10
        _synth.Volume = 90;       // 0 .. 100

        try
        {
            // Prefer a clear English voice if available
            var preferred = _synth.GetInstalledVoices()
                .Select(v => v.VoiceInfo)
                .FirstOrDefault(v => v.Culture.TwoLetterISOLanguageName == "en");

            if (preferred is not null)
                _synth.SelectVoice(preferred.Name);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Speech voice selection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Speaks the given text asynchronously (fire-and-forget).
    /// Safe to call from any thread.
    /// </summary>
    public void SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _disposed)
            return;

        // SpeakAsync cancels any previous utterance by default when we call it again.
        // We run it under a lock only to protect the synthesizer instance.
        lock (_lock)
        {
            try
            {
                _synth.SpeakAsyncCancelAll();
                _synth.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Speech failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Speaks the notes of a scale in a natural way.
    /// Example: "Major scale starting on C4. Notes: C4, D4, E4, F4, G4, A4, B4."
    /// </summary>
    public void SpeakScale(string scaleName, string rootName, IEnumerable<string> noteNames)
    {
        var notes = string.Join(", ", noteNames);
        var text = $"{scaleName} scale starting on {rootName}. Notes: {notes}.";
        SpeakAsync(text);
    }

    public void SpeakCorrect(string noteName)
    {
        SpeakAsync($"Correct. {noteName}");
    }

    public void SpeakWrong(string noteName)
    {
        SpeakAsync($"Wrong note. {noteName}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            try
            {
                _synth.SpeakAsyncCancelAll();
                _synth.Dispose();
            }
            catch { /* ignore */ }
        }
    }
}
