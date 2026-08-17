using ScaleTrainer.Core.Music;

namespace ScaleTrainer.Core.Scales;

/// <summary>
/// Pure logic engine for scale definitions, note generation, and membership testing.
/// Allocation-light and suitable for real-time use once a scale is selected.
/// </summary>
public sealed class ScaleEngine
{
    private readonly Dictionary<ScaleType, ScaleDefinition> _scales;
    private readonly Dictionary<ScaleType, HashSet<int>> _pitchClassSets;

    public ScaleEngine()
    {
        _scales = CreateBuiltInScales();
        _pitchClassSets = new Dictionary<ScaleType, HashSet<int>>();

        foreach (var kvp in _scales)
        {
            var set = new HashSet<int>();
            foreach (var interval in kvp.Value.Intervals)
                set.Add(interval % 12);
            _pitchClassSets[kvp.Key] = set;
        }
    }

    public IReadOnlyCollection<ScaleDefinition> GetAvailableScales() => _scales.Values;

    public ScaleDefinition GetScale(ScaleType type)
    {
        if (!_scales.TryGetValue(type, out var def))
            throw new ArgumentException($"Scale type {type} is not defined.", nameof(type));
        return def;
    }

    public IReadOnlyList<int> GetNotesInScale(ScaleType scaleType, int rootMidi, int minMidi = 0, int maxMidi = 127)
    {
        if (rootMidi < 0 || rootMidi > 127)
            throw new ArgumentOutOfRangeException(nameof(rootMidi));

        var definition = GetScale(scaleType);
        var result = new List<int>();

        for (int midi = minMidi; midi <= maxMidi; midi++)
        {
            int relative = (midi - rootMidi) % 12;
            if (relative < 0) relative += 12;

            if (definition.Intervals.Contains(relative))
                result.Add(midi);
        }

        return result;
    }

    public bool IsNoteInScale(ScaleType scaleType, int rootMidi, int playedMidi)
    {
        if (!_pitchClassSets.TryGetValue(scaleType, out var set))
            return false;

        int relative = (playedMidi - rootMidi) % 12;
        if (relative < 0) relative += 12;

        return set.Contains(relative);
    }

    /// <summary>
    /// Returns note names using conventional enharmonic spelling for the given root.
    /// </summary>
    public IReadOnlyList<string> GetNoteNames(ScaleType scaleType, int rootMidi, bool? forceSharps = null)
    {
        var definition = GetScale(scaleType);
        bool useSharps = forceSharps ?? PreferSharpsForRoot(rootMidi);

        var names = new List<string>(definition.Intervals.Count);
        foreach (var interval in definition.Intervals)
        {
            int midi = rootMidi + interval;
            names.Add(MidiToNoteName(midi, useSharps));
        }

        return names;
    }

    /// <summary>
    /// Returns a conventional name for a single MIDI note, using preferred spelling
    /// for the pitch class when no force is given.
    /// </summary>
    public static string MidiToNoteName(int midiNumber, bool? useSharps = null)
    {
        if (midiNumber < 0 || midiNumber > 127)
            return "?";

        int pitchClass = midiNumber % 12;
        int octave = (midiNumber / 12) - 1;

        bool sharps = useSharps ?? PreferSharpsForPitchClass(pitchClass);

        string[] sharpNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        string[] flatNames  = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

        string name = sharps ? sharpNames[pitchClass] : flatNames[pitchClass];
        return $"{name}{octave}";
    }

    /// <summary>
    /// Decides whether a scale built on this root should prefer sharps or flats
    /// for conventional spelling.
    /// </summary>
    public static bool PreferSharpsForRoot(int rootMidi)
    {
        return PreferSharpsForPitchClass(rootMidi % 12);
    }

    /// <summary>
    /// Conventional preference for each pitch class (circle-of-fifths informed).
    /// Pitch classes that are commonly notated with flats prefer flats.
    /// </summary>
    private static bool PreferSharpsForPitchClass(int pitchClass)
    {
        // Prefer flats for: Db(1), Eb(3), Gb(6), Ab(8), Bb(10)
        // Prefer sharps for: C#(1 is borderline but we treat as flat-preferring for major keys),
        //                    D#(3), F#(6), G#(8), A#(10) → these are the flat equivalents above.
        // Simple rule used by many teaching apps:
        return pitchClass switch
        {
            1  => false, // Db preferred over C#
            3  => false, // Eb preferred over D#
            6  => false, // Gb preferred over F# in many flat-key contexts (we still allow F# when forced)
            8  => false, // Ab preferred over G#
            10 => false, // Bb strongly preferred over A#
            _  => true   // C, D, E, F, G, A, B and the sharp versions of the others
        };
    }

    private static Dictionary<ScaleType, ScaleDefinition> CreateBuiltInScales()
    {
        return new Dictionary<ScaleType, ScaleDefinition>
        {
            [ScaleType.Major] = new ScaleDefinition(
                ScaleType.Major, "Major",
                new[] { 0, 2, 4, 5, 7, 9, 11 }),

            [ScaleType.NaturalMinor] = new ScaleDefinition(
                ScaleType.NaturalMinor, "Natural Minor",
                new[] { 0, 2, 3, 5, 7, 8, 10 }),

            [ScaleType.HarmonicMinor] = new ScaleDefinition(
                ScaleType.HarmonicMinor, "Harmonic Minor",
                new[] { 0, 2, 3, 5, 7, 8, 11 }),

            [ScaleType.MelodicMinor] = new ScaleDefinition(
                ScaleType.MelodicMinor, "Melodic Minor",
                new[] { 0, 2, 3, 5, 7, 9, 11 }),

            [ScaleType.Dorian] = new ScaleDefinition(
                ScaleType.Dorian, "Dorian",
                new[] { 0, 2, 3, 5, 7, 9, 10 }),

            [ScaleType.Phrygian] = new ScaleDefinition(
                ScaleType.Phrygian, "Phrygian",
                new[] { 0, 1, 3, 5, 7, 8, 10 }),

            [ScaleType.Lydian] = new ScaleDefinition(
                ScaleType.Lydian, "Lydian",
                new[] { 0, 2, 4, 6, 7, 9, 11 }),

            [ScaleType.Mixolydian] = new ScaleDefinition(
                ScaleType.Mixolydian, "Mixolydian",
                new[] { 0, 2, 4, 5, 7, 9, 10 }),

            [ScaleType.Locrian] = new ScaleDefinition(
                ScaleType.Locrian, "Locrian",
                new[] { 0, 1, 3, 5, 6, 8, 10 }),

            [ScaleType.MajorPentatonic] = new ScaleDefinition(
                ScaleType.MajorPentatonic, "Major Pentatonic",
                new[] { 0, 2, 4, 7, 9 }),

            [ScaleType.MinorPentatonic] = new ScaleDefinition(
                ScaleType.MinorPentatonic, "Minor Pentatonic",
                new[] { 0, 3, 5, 7, 10 }),

            [ScaleType.Blues] = new ScaleDefinition(
                ScaleType.Blues, "Blues",
                new[] { 0, 3, 5, 6, 7, 10 }),

            [ScaleType.WholeTone] = new ScaleDefinition(
                ScaleType.WholeTone, "Whole Tone",
                new[] { 0, 2, 4, 6, 8, 10 })
        };
    }
}
