using ScaleTrainer.Core.Scales;

namespace ScaleTrainer.App.Services;

/// <summary>
/// User preferences that are persisted between sessions.
/// </summary>
public sealed class AppSettings
{
    public string? MidiDeviceName { get; set; }
    public bool SpeakFeedbackEnabled { get; set; } = true;
    public ScaleType SelectedScale { get; set; } = ScaleType.Major;
    public int SelectedRootMidi { get; set; } = 60; // Middle C
}
