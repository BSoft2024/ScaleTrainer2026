namespace ScaleTrainer.Core.Scales;

/// <summary>
/// Immutable definition of a scale: name + interval pattern from the root (in semitones).
/// </summary>
public sealed class ScaleDefinition
{
    public ScaleType Type { get; }
    public string Name { get; }
    public IReadOnlyList<int> Intervals { get; }

    public ScaleDefinition(ScaleType type, string name, int[] intervals)
    {
        Type = type;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Intervals = intervals ?? throw new ArgumentNullException(nameof(intervals));

        if (intervals.Length == 0)
            throw new ArgumentException("Scale must have at least one interval.", nameof(intervals));
    }

    public override string ToString() => Name;
}
