# Scale Trainer

Windows application that helps you learn musical scales using a MIDI keyboard.

Play notes on your controller and get immediate feedback through sound, visuals, and optional speech. The interface is designed to work well with screen readers.

## Download

**[Download ScaleTrainer-Setup.exe](https://github.com/BSoft2024/ScaleTrainer2026/releases/latest)**

1. Download the installer from the latest [Release](https://github.com/BSoft2024/ScaleTrainer2026/releases)
2. Run `ScaleTrainer-Setup.exe`
3. Follow the setup wizard
4. Launch **Scale Trainer** from the Start Menu or Desktop shortcut

### Requirements

- Windows 10 or 11 (64-bit)
- MIDI keyboard recommended for practice
- **No separate .NET installation required**

## Features

- 13 scales: Major, Natural/Harmonic/Melodic Minor, Dorian, Phrygian, Lydian, Mixolydian, Locrian, Major/Minor Pentatonic, Blues, Whole Tone
- Root note selection with conventional sharp/flat spelling
- Real-time MIDI input
- Built-in soft synth plus error tone for wrong notes
- On-screen note highlighting
- Optional spoken feedback and “Speak Scale”
- Settings remembered between sessions
- Keyboard shortcuts and Help dialog
- Accessibility support (UI Automation names, live status region)

## Keyboard shortcuts

| Shortcut | Action |
|----------|--------|
| `F1` | About / Help |
| `Ctrl+S` | Speak the current scale |
| `Ctrl+R` | Refresh MIDI devices |
| `Ctrl+F` | Toggle spoken feedback |

## Build from source

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **.NET desktop development** workload
2. Clone this repository:

   ```bash
   git clone https://github.com/BSoft2024/ScaleTrainer2026.git
