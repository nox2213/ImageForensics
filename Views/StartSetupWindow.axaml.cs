using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Serilog;
using System.Threading.Tasks;
using System;
using Avalonia.Controls.Notifications;

using ImageForensics.Services;

namespace ImageForensics
{
    public partial class StartSetupWindow : Window
    {
        private TextBlock? statusText;
        private ProgressBar? progressBar;
        private ScrollViewer? logScrollViewer;
        private TextBlock? logText;
        private Image? loadingImage;
        private Grid? loadingCanvas; // Updated to match the new XAML
        private INotificationManager? notificationManager;

        public StartSetupWindow()
        {
            InitializeComponent();
            InitializeControls();
            _ = RunSetupAsync();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            statusText = this.FindControl<TextBlock>("StatusText");
            progressBar = this.FindControl<ProgressBar>("ProgressBar");
            logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            logText = this.FindControl<TextBlock>("LogText");
            loadingImage = this.FindControl<Image>("LoadingImage");
            loadingCanvas = this.FindControl<Grid>("LoadingCanvas"); // Updated to Grid

            notificationManager = new WindowNotificationManager(this)
            {
                Position = NotificationPosition.TopRight
            };

            if (statusText == null || progressBar == null || logScrollViewer == null ||
                logText == null || loadingImage == null || loadingCanvas == null || 
                notificationManager == null)
            {
                throw new NullReferenceException("Failed to initialize controls.");
            }
        }

        private async Task RunSetupAsync()
        {
            if (statusText == null || progressBar == null || logScrollViewer == null || 
                logText == null || notificationManager == null || loadingCanvas == null || 
                loadingImage == null)
            {
                return;
            }

            void UpdateLog(string message)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    logText.Text += $"{message}\n";
                    logScrollViewer.ScrollToEnd();
                });
            }

            try
            {
                statusText.Text = "Starting setup...\n";
                progressBar.IsIndeterminate = true;

                // Step 1: Run CreateEnvironment
                bool environmentSetup = await Task.Run(() => CreateEnvironment.SetupEnvironment(UpdateLog));
                if (!environmentSetup)
                {
                    statusText.Text += "Environment setup failed. Please check the logs.\n";
                    logScrollViewer.ScrollToEnd();
                    progressBar.IsIndeterminate = false;
                    return;
                }

                // Show the loading overlay
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    loadingCanvas.IsVisible = true;
                    loadingImage.IsVisible = true;
                });

                // Wait for environment setup to complete
                await WaitForEnvironmentSetupAsync();

                // Simulate additional setup steps (if needed)
                await Task.Delay(1000);

                statusText.Text += "Environment setup completed successfully.\n";

                // Hide the loading overlay
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    loadingCanvas.IsVisible = false;
                });

                // Launch the main application window
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during setup.");
                notificationManager?.Show(new Notification(
                    "Critical Error",
                    "An unexpected error occurred during setup. Please restart the application.",
                    NotificationType.Error
                ));
                statusText.Text += "Critical error. Setup aborted.\n";
                progressBar.IsIndeterminate = false;
            }
        }

        private async Task WaitForEnvironmentSetupAsync()
        {
            while (!ContainerdManager.EnvironmentSetupComplete)
            {
                await Task.Delay(500); // Polling interval
            }
        }
    }
}
