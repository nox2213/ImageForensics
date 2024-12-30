using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace ImageForensics.Services
{
    public class CreateEnvironment
    {
        private const string PythonEmbedUrl = "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip";
        private const string BaseDirectory = "src";
        private const string PythonEmbedDir = "src/python-3.11.9-embed-amd64";
        private const string GetPipUrl = "https://bootstrap.pypa.io/get-pip.py";

        public static async Task<bool> SetupEnvironment(Action<string> logHandler)
        {
            try
            {
                Log.Information("Setting up Python Embedded environment...");
                logHandler("Setting up Python Embedded environment...");

                if (!Directory.Exists(PythonEmbedDir))
                {
                    Directory.CreateDirectory(BaseDirectory);
                    await DownloadFileAsync(PythonEmbedUrl, "python-embed.zip", logHandler);
                    ZipFile.ExtractToDirectory("python-embed.zip", PythonEmbedDir);
                    File.Delete("python-embed.zip");
                }

                ConfigurePython(logHandler);
                await InstallPip(logHandler);

                Log.Information("Environment setup complete.");
                logHandler("Environment setup complete.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during environment setup");
                logHandler($"Error: {ex.Message}");
                return false;
            }
        }

        private static void ConfigurePython(Action<string> logHandler)
        {
            string pthFilePath = Path.Combine(PythonEmbedDir, "python311._pth");
            if (File.Exists(pthFilePath))
            {
                Log.Information("Modifying python311._pth file...");
                logHandler("Modifying python311._pth file...");
                string[] lines = File.ReadAllLines(pthFilePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("# Uncomment to run site.main() automatically"))
                    {
                        lines[i + 1] = "import site";
                        break;
                    }
                }
                File.WriteAllLines(pthFilePath, lines);
            }
        }

        private static async Task InstallPip(Action<string> logHandler)
        {
            string pipScriptPath = Path.Combine(PythonEmbedDir, "get-pip.py");
            if (!File.Exists(pipScriptPath))
            {
                await DownloadFileAsync(GetPipUrl, pipScriptPath, logHandler);
            }

            Log.Information("Installing pip...");
            logHandler("Installing pip...");
            RunProcess(Path.Combine(PythonEmbedDir, "python.exe"), $"{pipScriptPath} --no-warn-script-location", logHandler);
        }

        private static async Task DownloadFileAsync(string url, string destination, Action<string> logHandler)
        {
            try
            {
                using HttpClient client = new HttpClient();
                Log.Information($"Downloading {url}...");
                logHandler($"Downloading {url}...");
                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(destination, FileMode.Create);
                await response.Content.CopyToAsync(fs);
                Log.Information($"Downloaded to {destination}");
                logHandler($"Downloaded to {destination}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to download {url}");
                logHandler($"Failed to download {url}: {ex.Message}");
                throw;
            }
        }

        private static void RunProcess(string fileName, string arguments, Action<string>? logHandler = null)
        {
            try
            {
                Log.Information($"Running process: {fileName} {arguments}");
                logHandler?.Invoke($"Running process: {fileName} {arguments}");

                var scriptsPath = Path.Combine(PythonEmbedDir, "Scripts");
                var originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                var newPath = $"{scriptsPath};{PythonEmbedDir};{originalPath}";
                Environment.SetEnvironmentVariable("PATH", newPath);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(startInfo);
                if (process == null)
                {
                    Log.Error("Failed to start process.");
                    logHandler?.Invoke("Failed to start process.");
                    return;
                }

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        logHandler?.Invoke(args.Data);
                        Log.Information(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        logHandler?.Invoke(args.Data);
                        Log.Error(args.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running process");
                logHandler?.Invoke($"Error running process: {ex.Message}");
                throw;
            }
        }
    }
}
