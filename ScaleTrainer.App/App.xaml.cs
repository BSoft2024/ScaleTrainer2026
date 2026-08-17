using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ScaleTrainer.App.Services;
using ScaleTrainer.App.ViewModels;
using ScaleTrainer.Core.Scales;

namespace ScaleTrainer.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();

            services.AddSingleton<ScaleEngine>();
            services.AddSingleton<MidiInputService>();
            services.AddSingleton<AudioEngine>();
            services.AddSingleton<SpeechService>();
            services.AddSingleton<SettingsService>();
            services.AddTransient<MainViewModel>();

            Services = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Startup failed:\n\n" + ex.ToString(),
                "Scale Trainer – Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (Services?.GetService<MidiInputService>() is { } midi)
                midi.Dispose();
            if (Services?.GetService<AudioEngine>() is { } audio)
                audio.Dispose();
            if (Services?.GetService<SpeechService>() is { } speech)
                speech.Dispose();
        }
        catch { /* ignore */ }

        base.OnExit(e);
    }
}