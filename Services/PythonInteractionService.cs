using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace ImageForensics.Services
{
    public class PythonInteractionService
    {
        private const string PythonExecutable = "src/python-3.11.9-embed-amd64/python.exe";

        /// <summary>
        /// Ensures the embedded Python executable is available.
        /// </summary>
        public static void EnsurePythonAvailable()
        {
            if (!File.Exists(PythonExecutable))
            {
                throw new FileNotFoundException("Embedded Python executable not found.", PythonExecutable);
            }
        }

        /// <summary>
        /// Runs a Python script with the provided arguments.
        /// </summary>
        /// <param name="scriptPath">The path to the Python script to execute.</param>
        /// <param name="arguments">Additional arguments to pass to the script.</param>
        /// <returns>The standard output from the Python process.</returns>
        public static async Task<string> RunPythonScriptAsync(string scriptPath, string arguments = "")
        {
            EnsurePythonAvailable();

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("Python script not found.", scriptPath);
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = PythonExecutable,
                Arguments = $"\"{scriptPath}\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            Log.Information("Running Python script: {ScriptPath} {Arguments}", scriptPath, arguments);

            try
            {
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Log.Error("Python script exited with code {ExitCode}. Error: {Error}", process.ExitCode, error);
                    throw new Exception($"Python script exited with code {process.ExitCode}: {error}");
                }

                Log.Information("Python script completed successfully. Output: {Output}", output);
                return output;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while running the Python script.");
                throw;
            }
        }

        /// <summary>
        /// Installs a Python package using pip.
        /// </summary>
        /// <param name="packageName">The name of the package to install.</param>
        public static async Task InstallPythonPackageAsync(string packageName)
        {
            EnsurePythonAvailable();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = PythonExecutable,
                Arguments = $"-m pip install {packageName} --no-warn-script-location",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            Log.Information("Installing Python package: {PackageName}", packageName);

            try
            {
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Log.Error("Pip install exited with code {ExitCode}. Error: {Error}", process.ExitCode, error);
                    throw new Exception($"Pip install exited with code {process.ExitCode}: {error}");
                }

                Log.Information("Package {PackageName} installed successfully. Output: {Output}", packageName, output);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while installing the Python package.");
                throw;
            }
        }
        public static async Task<string> RunSherloqGuiAsync()
        {
            string scriptPath = Path.Combine("src", "sherloq", "gui", "sherloq.py");
            return await RunPythonScriptAsync(scriptPath);
        }
    
    
    }
}
