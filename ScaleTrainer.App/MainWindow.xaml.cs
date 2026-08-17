using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ScaleTrainer.App.ViewModels;

namespace ScaleTrainer.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            InitializeComponent();
            DataContext = App.Services.GetRequiredService<MainViewModel>();
            Closing += OnClosing;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Window failed to open:\n\n" + ex.ToString(),
                "Scale Trainer – Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            throw;
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.SaveOnExit();
    }
}