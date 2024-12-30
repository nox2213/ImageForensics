using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImageForensics.Services;
using Serilog;
using System;

namespace ImageForensics
{
    public partial class App : Application
    {
        private ContainerdSubprocessWindow? subprocessWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            InitializeLogging();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += OnExit;

                // Create and show the subprocess logging window
                subprocessWindow = new ContainerdSubprocessWindow();
                subprocessWindow.Show();

                // Start containerd and pass logs to the subprocess window
                _ = ContainerdManager.ExecuteEnvironmentAsync(message => subprocessWindow?.AppendLog(message));

                // Create and set the main application window
                var mainWindow = new StartSetupWindow(); // Replace MainWindow with your actual main window class
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            Log.Information("Application is exiting. Cleaning up subprocesses...");
            subprocessWindow?.Close();
            await ContainerdManager.StopContainerdAndBuildKit(message => subprocessWindow?.AppendLog(message));
            Log.CloseAndFlush();
        }

            private void InitializeLogging()
            {
                string logFilePath = $"Logs/log-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Infinite)
                    .CreateLogger();

                Log.Information("Logging initialized.");
            }

    }
}
