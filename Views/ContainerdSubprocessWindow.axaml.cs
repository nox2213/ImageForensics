using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using Serilog;

namespace ImageForensics
{
    public partial class ContainerdSubprocessWindow : Window
    {
        private readonly TextBlock? _logOutput;

        public ContainerdSubprocessWindow()
        {
            InitializeComponent();
            _logOutput = this.FindControl<TextBlock>("LogOutput");
            if (_logOutput == null)
            {
                Log.Error("LogOutput TextBlock not found.");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void AppendLog(string message)
        {
            if (_logOutput == null)
            {
                Log.Error("LogOutput is not initialized. Cannot append log message.");
                return; // Exit early if _logOutput is null
            }

            // Ensure thread-safe UI updates
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (_logOutput.Text?.Length > 1000)
                    {
                        _logOutput.Text = string.Empty; // Clear logs when exceeding 1000 characters
                    }

                    _logOutput.Text += $"{DateTime.Now:HH:mm:ss} {message}\n";
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to append log message to LogOutput.");
                }
            });
        }
    }
}
