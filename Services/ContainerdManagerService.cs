using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;
using Serilog;

namespace ImageForensics.Services
{
    public static class ContainerdManager
    {
        private static readonly string InstallScriptPath = Path.Combine("Scripts", "install_containerd.ps1");
        private static readonly string InstallBuildkitPath = Path.Combine("Scripts", "install_buildkit.ps1");
        private static readonly string UninstallScriptPath = Path.Combine("Scripts", "uninstall_buildkit+containerd.ps1");
        public static bool EnvironmentSetupComplete { get; private set; } = false;
        public static async Task ExecuteEnvironmentAsync(Action<string>? logCallback = null)
        {
            try
            {
                if (!IsRunningAsAdministrator())
                {
                    RestartAsAdministrator();
                    return;
                }

                logCallback?.Invoke("Starting containerd setup...");
                await StartContainerd(logCallback);

                logCallback?.Invoke("Starting BuildKit setup...");
                await StartBuildkit(logCallback);

                EnvironmentSetupComplete = true;

                logCallback?.Invoke("Environment setup completed successfully.");
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"Error setting up environment: {ex.Message}");
                Log.Error(ex, "Error setting up environment.");
                throw;
            }
        }

        private static async Task StartContainerd(Action<string>? logCallback = null)
        {
            if (!File.Exists(InstallScriptPath))
            {
                throw new FileNotFoundException("Install script not found.", InstallScriptPath);
            }

            logCallback?.Invoke("Starting containerd...");
            Log.Information("Starting containerd...");
            await ExecuteScriptAsync(InstallScriptPath, logCallback);
            logCallback?.Invoke("Containerd started successfully.");
        }

        private static async Task StartBuildkit(Action<string>? logCallback = null)
        {
            if (!File.Exists(InstallBuildkitPath))
            {
                throw new FileNotFoundException("Install script not found.", InstallBuildkitPath);
            }

            logCallback?.Invoke("Starting BuildKit...");
            Log.Information("Starting BuildKit...");
            await ExecuteScriptAsync(InstallBuildkitPath, logCallback);
            logCallback?.Invoke("BuildKit started successfully.");
        }

        public static async Task StopContainerdAndBuildKit(Action<string>? logCallback = null)
        {
            if (!File.Exists(UninstallScriptPath))
            {
                throw new FileNotFoundException("Uninstall script not found.", UninstallScriptPath);
            }

            logCallback?.Invoke("Stopping containerd and BuildKit...");
            Log.Information("Stopping containerd and BuildKit...");
            await ExecuteScriptAsync(UninstallScriptPath, logCallback);
            logCallback?.Invoke("Containerd and BuildKit stopped successfully.");
        }

        private static async Task ExecuteScriptAsync(string scriptPath, Action<string>? logCallback)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    logCallback?.Invoke(args.Data);
                    Log.Information(args.Data);
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    logCallback?.Invoke($"Error: {args.Data}");
                    Log.Error(args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var exited = await Task.Run(() => process.WaitForExit(60000));
            if (!exited)
            {
                process.Kill();
                throw new TimeoutException("Script execution timed out.");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Script execution failed with exit code {process.ExitCode}.");
            }
        }

        private static bool IsRunningAsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RestartAsAdministrator()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true
            };

            try
            {
                Process.Start(startInfo);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to restart application with elevated privileges.");
                throw new InvalidOperationException("Application requires Administrator privileges to run.", ex);
            }
        }
    }
}
