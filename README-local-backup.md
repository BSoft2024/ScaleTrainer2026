# Scale Trainer

Windows application that helps you learn musical scales using a MIDI keyboard.

Play notes on your controller and get immediate feedback:
- Soft synth playback
- Error tone for wrong notes
- On-screen highlight
- Optional spoken feedback
- Screen-reader friendly interface

## Requirements

- Windows 10 or 11 (64-bit)
- A MIDI keyboard (optional for UI exploration, required for practice)
- **No separate .NET install required** when using the official installer

## Download / Install

1. Open the [Releases](https://github.com/BSoft2024/ScaleTrainer2026/releases) page
2. Download `ScaleTrainer-Setup.exe`
3. Run the installer
4. Launch **Scale Trainer** from the Start Menu or Desktop shortcut

## Features

- 13 scales (Major, minors, modes, pentatonics, Blues, Whole Tone)
- Root note selection with conventional enharmonic spelling
- Real-time MIDI input
- Built-in soft synth + error tone
- Visual note highlighting
- Speak Scale and optional spoken correct/wrong feedback
- Settings remembered between sessions
- Keyboard shortcuts and Help dialog
- Accessibility support (UI Automation, live region)

## Keyboard shortcuts

| Shortcut | Action |
|----------|--------|
| F1 | About / Help |
| Ctrl+S | Speak current scale |
| Ctrl+R | Refresh MIDI devices |
| Ctrl+F | Toggle spoken feedback |

## Build from source

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **.NET desktop development** workload
2. Clone this repository
3. Open `ScaleTrainer.sln`
4. Restore NuGet packages
5. Set **ScaleTrainer.App** as the startup project
6. Build and run (F5)

### Publish a self-contained build

In Visual Studio:

- Deployment mode: **Self-contained**
- Target runtime: **win-x64**
- Configuration: **Release**

Or from a developer command prompt:

```bash
dotnet publish ScaleTrainer.App -c Release -r win-x64 --self-contained true
