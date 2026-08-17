using ScaleTrainer.Core.Scales;

Console.WriteLine("=== ScaleTrainer Core – Step 1 Demo ===\n");

var engine = new ScaleEngine();

Console.WriteLine("Available scales:");
foreach (var scale in engine.GetAvailableScales())
{
    Console.WriteLine($"  - {scale.Name} ({scale.Type})");
}

Console.WriteLine();

// Demo: C Major
const int rootC4 = 60; // Middle C
var majorNotes = engine.GetNoteNames(ScaleType.Major, rootC4);
Console.WriteLine($"C Major scale notes: {string.Join(" ", majorNotes)}");

// Membership tests
Console.WriteLine();
Console.WriteLine("Membership tests (C Major, root = 60):");
int[] testNotes = { 60, 62, 64, 65, 67, 69, 71, 61, 63 }; // C D E F G A B  + C# D#
foreach (var midi in testNotes)
{
    bool inScale = engine.IsNoteInScale(ScaleType.Major, rootC4, midi);
    string name = ScaleEngine.MidiToNoteName(midi);
    Console.WriteLine($"  {name} (MIDI {midi}): {(inScale ? "IN SCALE" : "OUTSIDE")}");
}

// Natural Minor example
Console.WriteLine();
var minorNotes = engine.GetNoteNames(ScaleType.NaturalMinor, rootC4);
Console.WriteLine($"C Natural Minor: {string.Join(" ", minorNotes)}");

// Pentatonic
var penta = engine.GetNoteNames(ScaleType.MinorPentatonic, rootC4);
Console.WriteLine($"C Minor Pentatonic: {string.Join(" ", penta)}");

Console.WriteLine("\nStep 1 completed successfully.");
