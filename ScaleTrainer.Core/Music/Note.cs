namespace ScaleTrainer.Core.Music;

/// <summary>
/// Represents a musical note using MIDI note number (0-127).
/// </summary>
public readonly record struct Note
{
    public const int MinMidi = 0;
    public const int MaxMidi = 127;

    public int MidiNumber { get; }

    public Note(int midiNumber)
    {
        if (midiNumber < MinMidi || midiNumber > MaxMidi)
            throw new ArgumentOutOfRangeException(nameof(midiNumber), "MIDI note must be between 0 and 127.");
        MidiNumber = midiNumber;
    }

    public int Octave => (MidiNumber / 12) - 1;

    public int PitchClass => MidiNumber % 12;

    public static Note FromMidi(int midiNumber) => new(midiNumber);

    public override string ToString() => $"MIDI {MidiNumber}";
}

