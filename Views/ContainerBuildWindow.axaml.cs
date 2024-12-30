using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ImageForensics.Services;
using Serilog;
using System;

namespace ImageForensics
{
    public partial class ContainerBuildWindow : Window
    {
        private readonly ContainerBuildService _buildService;
        private readonly TextBlock? _logOutput;

        public ContainerBuildWindow()
        {
            InitializeComponent();
            _buildService = new ContainerBuildService();
            _logOutput = this.FindControl<TextBlock>("LogOutput");
            if (_logOutput == null)
            {
                Log.Error("LogOutput TextBlock not found. Logging will not be displayed in the UI.");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public ContainerBuildWindow(string containerName, string dockerfilePath, string contextPath, string outputPath)
            : this()
        {
            StartBuild(containerName, dockerfilePath, contextPath, outputPath);
        }

        public async void StartBuild(string containerName, string dockerfilePath, string contextPath, string outputPath)
        {
            try
            {
                AppendLog($"Starting build process for container: {containerName}");
                await _buildService.BuildContainerAsync(containerName, dockerfilePath, contextPath, outputPath);
                AppendLog($"Container '{containerName}' built successfully.");
            }
            catch (Exception ex)
            {
                AppendLog($"Error building container: {ex.Message}");
                Log.Error(ex, "Error building container.");
            }
        }

        private void AppendLog(string message)
        {
            Log.Information(message);

            if (_logOutput == null)
            {
                Log.Warning("LogOutput is not initialized. Log message cannot be displayed in the UI.");
                return;
            }

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_logOutput.Text?.Length > 1000)
                {
                    _logOutput.Text = string.Empty;
                }
                _logOutput.Text += $"{DateTime.Now:HH:mm:ss} {message}\n";
            });
        }

        private void OnCloseButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}