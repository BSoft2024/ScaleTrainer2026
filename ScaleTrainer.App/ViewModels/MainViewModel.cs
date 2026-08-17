using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScaleTrainer.App.Services;
using ScaleTrainer.App.Views;
using ScaleTrainer.Core.Scales;

namespace ScaleTrainer.App.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ScaleEngine _scaleEngine;
    private readonly MidiInputService _midiService;
    private readonly AudioEngine _audioEngine;
    private readonly SpeechService _speechService;
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private bool _disposed;
    private bool _loadingSettings;
    private bool _currentScaleUsesSharps = true;

    public MainViewModel(
        ScaleEngine scaleEngine,
        MidiInputService midiService,
        AudioEngine audioEngine,
        SpeechService speechService,
        SettingsService settingsService)
    {
        _scaleEngine = scaleEngine;
        _midiService = midiService;
        _audioEngine = audioEngine;
        _speechService = speechService;
        _settingsService = settingsService;

        _settings = _settingsService.Load();
        _loadingSettings = true;

        AvailableScales = new ObservableCollection<ScaleDefinition>(
            _scaleEngine.GetAvailableScales());

        SelectedScale = AvailableScales.FirstOrDefault(s => s.Type == _settings.SelectedScale)
                        ?? AvailableScales.FirstOrDefault(s => s.Type == ScaleType.Major)
                        ?? AvailableScales.First();

        AvailableRoots = new ObservableCollection<RootItem>();
        for (int midi = 48; midi <= 72; midi++)
        {
            AvailableRoots.Add(new RootItem(midi, ScaleEngine.MidiToNoteName(midi)));
        }

        SelectedRoot = AvailableRoots.FirstOrDefault(r => r.MidiNumber == _settings.SelectedRootMidi)
                       ?? AvailableRoots.FirstOrDefault(r => r.MidiNumber == 60)
                       ?? AvailableRoots.First();

        AvailableMidiDevices = new ObservableCollection<MidiDeviceInfo>(MidiInputService.GetDevices());

        if (!string.IsNullOrEmpty(_settings.MidiDeviceName))
        {
            SelectedMidiDevice = AvailableMidiDevices.FirstOrDefault(d => d.Name == _settings.MidiDeviceName)
                                 ?? AvailableMidiDevices.FirstOrDefault();
        }
        else
        {
            SelectedMidiDevice = AvailableMidiDevices.FirstOrDefault();
        }

        SpeakFeedbackEnabled = _settings.SpeakFeedbackEnabled;

        UpdateScaleNotes();

        _midiService.NoteOn += OnMidiNoteOn;
        _midiService.NoteOff += OnMidiNoteOff;

        try { _audioEngine.Start(); }
        catch (Exception ex) { StatusMessage = $"Audio engine failed to start: {ex.Message}"; }

        if (SelectedMidiDevice is not null)
        {
            try
            {
                _midiService.Open(SelectedMidiDevice.Index);
                IsMidiConnected = true;
                StatusMessage = $"MIDI connected: {SelectedMidiDevice.Name}. Play notes to practice.";
            }
            catch { TryStartMidi(); }
        }
        else
        {
            TryStartMidi();
        }

        _loadingSettings = false;
    }

    public ObservableCollection<ScaleDefinition> AvailableScales { get; }
    public ObservableCollection<RootItem> AvailableRoots { get; }
    public ObservableCollection<MidiDeviceInfo> AvailableMidiDevices { get; }

    [ObservableProperty] private ScaleDefinition? _selectedScale;
    [ObservableProperty] private RootItem? _selectedRoot;
    [ObservableProperty] private MidiDeviceInfo? _selectedMidiDevice;
    [ObservableProperty] private ObservableCollection<ScaleNoteItem> _currentScaleNotes = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _lastPlayedNote = string.Empty;
    [ObservableProperty] private bool _isMidiConnected;
    [ObservableProperty] private bool _speakFeedbackEnabled;

    partial void OnSelectedScaleChanged(ScaleDefinition? value)
    {
        UpdateScaleNotes();
        if (!_loadingSettings && value is not null)
        {
            _settings.SelectedScale = value.Type;
            SaveSettings();
        }
    }

    partial void OnSelectedRootChanged(RootItem? value)
    {
        UpdateScaleNotes();
        if (!_loadingSettings && value is not null)
        {
            _settings.SelectedRootMidi = value.MidiNumber;
            SaveSettings();
        }
    }

    partial void OnSelectedMidiDeviceChanged(MidiDeviceInfo? value)
    {
        if (value is null || _loadingSettings) return;
        try
        {
            _midiService.Open(value.Index);
            IsMidiConnected = true;
            StatusMessage = $"MIDI connected: {value.Name}";
            _settings.MidiDeviceName = value.Name;
            SaveSettings();
        }
        catch (Exception ex)
        {
            IsMidiConnected = false;
            StatusMessage = $"Failed to open MIDI device: {ex.Message}";
        }
    }

    partial void OnSpeakFeedbackEnabledChanged(bool value)
    {
        if (_loadingSettings) return;
        _settings.SpeakFeedbackEnabled = value;
        SaveSettings();
    }

    private void SaveSettings() => _settingsService.Save(_settings);

    private void UpdateScaleNotes()
    {
        CurrentScaleNotes.Clear();
        if (SelectedScale is null || SelectedRoot is null) return;

        _currentScaleUsesSharps = ScaleEngine.PreferSharpsForRoot(SelectedRoot.MidiNumber);

        var names = _scaleEngine.GetNoteNames(
            SelectedScale.Type,
            SelectedRoot.MidiNumber,
            _currentScaleUsesSharps);

        // Also store the pitch class (0-11) so we can match any octave later
        var definition = _scaleEngine.GetScale(SelectedScale.Type);
        for (int i = 0; i < names.Count; i++)
        {
            int midi = SelectedRoot.MidiNumber + definition.Intervals[i];
            int pitchClass = ((midi % 12) + 12) % 12;
            CurrentScaleNotes.Add(new ScaleNoteItem(names[i], pitchClass));
        }

        StatusMessage = $"{SelectedScale.Name} on {SelectedRoot.DisplayName}. " +
                        $"Notes: {string.Join(" ", names)}";
    }

    private void OnMidiNoteOn(object? sender, MidiNoteEventArgs e)
    {
        _audioEngine.NoteOn(e.NoteNumber, e.Velocity);

        if (SelectedScale is null || SelectedRoot is null) return;

        bool inScale = _scaleEngine.IsNoteInScale(
            SelectedScale.Type,
            SelectedRoot.MidiNumber,
            e.NoteNumber);

        // Name with the same accidental system as the displayed scale
        string noteName = ScaleEngine.MidiToNoteName(e.NoteNumber, _currentScaleUsesSharps);
        int playedPitchClass = ((e.NoteNumber % 12) + 12) % 12;

        if (!inScale)
            _audioEngine.TriggerErrorTone();

        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            LastPlayedNote = noteName;
            StatusMessage = inScale ? $"Correct: {noteName}" : $"Wrong note: {noteName}";

            // Clear previous highlights
            foreach (var item in CurrentScaleNotes)
            {
                item.IsHighlighted = false;
                item.WasCorrect = null;
            }

            // Match by PITCH CLASS (ignores octave) – this is the key fix
            var match = CurrentScaleNotes.FirstOrDefault(n => n.PitchClass == playedPitchClass);
            if (match is not null)
            {
                match.IsHighlighted = true;
                match.WasCorrect = inScale;
            }

            if (SpeakFeedbackEnabled)
            {
                if (inScale)
                    _speechService.SpeakCorrect(noteName);
                else
                    _speechService.SpeakWrong(noteName);
            }
        });
    }

    private void OnMidiNoteOff(object? sender, MidiNoteEventArgs e)
    {
        _audioEngine.NoteOff(e.NoteNumber);
    }

    private void TryStartMidi()
    {
        if (AvailableMidiDevices.Count == 0)
        {
            StatusMessage = "No MIDI input devices found. Connect a keyboard and use Refresh.";
            IsMidiConnected = false;
            return;
        }

        if (_midiService.TryOpenFirstDevice())
        {
            IsMidiConnected = true;
            SelectedMidiDevice = AvailableMidiDevices.FirstOrDefault(d => d.Name == _midiService.CurrentDeviceName)
                                 ?? AvailableMidiDevices.First();
            StatusMessage = $"MIDI connected: {_midiService.CurrentDeviceName}. Play notes to practice.";
            _settings.MidiDeviceName = _midiService.CurrentDeviceName;
            SaveSettings();
        }
        else
        {
            IsMidiConnected = false;
            StatusMessage = "Could not open MIDI device.";
        }
    }

    [RelayCommand]
    private void SpeakScale()
    {
        if (SelectedScale is null || SelectedRoot is null) return;

        _speechService.SpeakScale(
            SelectedScale.Name,
            SelectedRoot.DisplayName,
            CurrentScaleNotes.Select(n => n.Name));

        StatusMessage = $"Speaking {SelectedScale.Name} on {SelectedRoot.DisplayName}…";
    }

    [RelayCommand]
    private void RefreshMidiDevices()
    {
        AvailableMidiDevices.Clear();
        foreach (var d in MidiInputService.GetDevices())
            AvailableMidiDevices.Add(d);
        StatusMessage = $"Found {AvailableMidiDevices.Count} MIDI device(s).";
    }

    [RelayCommand]
    private void ToggleSpeakFeedback()
    {
        SpeakFeedbackEnabled = !SpeakFeedbackEnabled;
        StatusMessage = SpeakFeedbackEnabled ? "Spoken feedback enabled." : "Spoken feedback disabled.";
    }

    [RelayCommand]
    private void ShowAbout()
    {
        var about = new AboutWindow { Owner = Application.Current.MainWindow };
        about.ShowDialog();
    }

    public void SaveOnExit() => SaveSettings();

    public void Dispose()
    {
        if (_disposed) return;
        _midiService.NoteOn -= OnMidiNoteOn;
        _midiService.NoteOff -= OnMidiNoteOff;
        SaveSettings();
        _disposed = true;
    }
}

public sealed class RootItem
{
    public int MidiNumber { get; }
    public string DisplayName { get; }
    public RootItem(int midiNumber, string displayName)
    {
        MidiNumber = midiNumber;
        DisplayName = displayName;
    }
    public override string ToString() => DisplayName;
}

public partial class ScaleNoteItem : ObservableObject
{
    public string Name { get; }
    public int PitchClass { get; }   // 0–11, used for octave-independent matching

    [ObservableProperty] private bool _isHighlighted;
    [ObservableProperty] private bool? _wasCorrect;

    public ScaleNoteItem(string name, int pitchClass)
    {
        Name = name;
        PitchClass = pitchClass;
    }

    public override string ToString() => Name;
}
