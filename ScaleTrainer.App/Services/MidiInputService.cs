using NAudio.Midi;
using System.Diagnostics;

namespace ScaleTrainer.App.Services;

/// <summary>
/// Lightweight MIDI input service using NAudio.
/// Raises NoteOn / NoteOff events on a background thread.
/// Designed so the hot path stays allocation-light.
/// </summary>
public sealed class MidiInputService : IDisposable
{
    private MidiIn? _midiIn;
    private bool _disposed;

    public event EventHandler<MidiNoteEventArgs>? NoteOn;
    public event EventHandler<MidiNoteEventArgs>? NoteOff;

    public bool IsOpen => _midiIn is not null;

    public string? CurrentDeviceName { get; private set; }

    /// <summary>
    /// Returns a list of available MIDI input devices.
    /// </summary>
    public static IReadOnlyList<MidiDeviceInfo> GetDevices()
    {
        var list = new List<MidiDeviceInfo>();
        for (int i = 0; i < MidiIn.NumberOfDevices; i++)
        {
            var info = MidiIn.DeviceInfo(i);
            list.Add(new MidiDeviceInfo(i, info.ProductName));
        }
        return list;
    }

    /// <summary>
    /// Opens the MIDI input device at the given index.
    /// Closes any previously opened device.
    /// </summary>
    public void Open(int deviceIndex)
    {
        Close();

        if (deviceIndex < 0 || deviceIndex >= MidiIn.NumberOfDevices)
            throw new ArgumentOutOfRangeException(nameof(deviceIndex));

        _midiIn = new MidiIn(deviceIndex);
        _midiIn.MessageReceived += OnMessageReceived;
        _midiIn.ErrorReceived += OnErrorReceived;
        _midiIn.Start();

        CurrentDeviceName = MidiIn.DeviceInfo(deviceIndex).ProductName;
        Debug.WriteLine($"MIDI input opened: {CurrentDeviceName}");
    }

    /// <summary>
    /// Tries to open the first available device. Returns true if successful.
    /// </summary>
    public bool TryOpenFirstDevice()
    {
        if (MidiIn.NumberOfDevices == 0)
            return false;

        try
        {
            Open(0);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open first MIDI device: {ex.Message}");
            return false;
        }
    }

    public void Close()
    {
        if (_midiIn is null) return;

        try
        {
            _midiIn.Stop();
            _midiIn.MessageReceived -= OnMessageReceived;
            _midiIn.ErrorReceived -= OnErrorReceived;
            _midiIn.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error closing MIDI device: {ex.Message}");
        }
        finally
        {
            _midiIn = null;
            CurrentDeviceName = null;
        }
    }

    private void OnMessageReceived(object? sender, MidiInMessageEventArgs e)
    {
        // Fast path – only care about Note On / Note Off
        if (e.MidiEvent is NoteOnEvent noteOn)
        {
            if (noteOn.Velocity > 0)
            {
                NoteOn?.Invoke(this, new MidiNoteEventArgs(noteOn.NoteNumber, noteOn.Velocity));
            }
            else
            {
                // Note On with velocity 0 is treated as Note Off by many devices
                NoteOff?.Invoke(this, new MidiNoteEventArgs(noteOn.NoteNumber, 0));
            }
        }
        else if (e.MidiEvent is NoteEvent noteOff && noteOff.CommandCode == MidiCommandCode.NoteOff)
        {
            NoteOff?.Invoke(this, new MidiNoteEventArgs(noteOff.NoteNumber, 0));
        }
    }

    private void OnErrorReceived(object? sender, MidiInMessageEventArgs e)
    {
        Debug.WriteLine($"MIDI error: {e.MidiEvent}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        Close();
        _disposed = true;
    }
}

public sealed class MidiDeviceInfo
{
    public int Index { get; }
    public string Name { get; }

    public MidiDeviceInfo(int index, string name)
    {
        Index = index;
        Name = name;
    }

    public override string ToString() => Name;
}

public sealed class MidiNoteEventArgs : EventArgs
{
    public int NoteNumber { get; }
    public int Velocity { get; }

    public MidiNoteEventArgs(int noteNumber, int velocity)
    {
        NoteNumber = noteNumber;
        Velocity = velocity;
    }
}
