using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace ImageForensics.Services
{
    public class ContainerBuildService
    {
        private static readonly string BuildkitExecutable = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "buildkit", "bin", "buildctl.exe");

        public async Task BuildContainerAsync(string containerName, string dockerfilePath, string contextPath, string outputPath)
        {
            Log.Information("Starting container build for {ContainerName}", containerName);

            ValidateInputs(containerName, dockerfilePath, contextPath);

            if (!File.Exists(BuildkitExecutable))
                throw new FileNotFoundException("BuildKit executable not found.", BuildkitExecutable);

            try
            {
                string tarballPath = Path.Combine(Path.GetTempPath(), $"{containerName}.tar");
                CreateTarballFromContext(contextPath, tarballPath);

                string arguments = BuildCommandArguments(dockerfilePath, contextPath, outputPath, containerName);
                await ExecuteBuildKitCommandAsync(arguments);

                Log.Information("Container '{ContainerName}' built successfully. Output path: {OutputPath}", containerName, outputPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while building the container for {ContainerName}", containerName);
                throw;
            }
        }

        private static void ValidateInputs(string containerName, string dockerfilePath, string contextPath)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name must be provided.", nameof(containerName));

            if (!File.Exists(dockerfilePath))
                throw new FileNotFoundException("Dockerfile not found at the specified path.", dockerfilePath);

            if (!Directory.Exists(contextPath))
                throw new DirectoryNotFoundException("Build context directory not found.");
        }

        private static string BuildCommandArguments(string dockerfilePath, string contextPath, string outputPath, string containerName)
        {
            return $"build --frontend=dockerfile.v0 " +
                   $"--local context={contextPath} " +
                   $"--local dockerfile={dockerfilePath} " +
                   $"--output type=oci,name={containerName},dest={outputPath}";
        }

        private void CreateTarballFromContext(string contextPath, string tarballPath)
        {
            try
            {
                if (File.Exists(tarballPath))
                    File.Delete(tarballPath);

                using var tarStream = new FileStream(tarballPath, FileMode.Create, FileAccess.Write);
                using var writer = new StreamWriter(tarStream, Encoding.UTF8);

                foreach (var filePath in Directory.GetFiles(contextPath, "*", SearchOption.AllDirectories))
                {
                    string entryName = Path.GetRelativePath(contextPath, filePath)
                        .Replace(Path.DirectorySeparatorChar, '/');
                    writer.WriteLine($"{entryName}");
                }

                Log.Information("Build context tarball created at {TarballPath}", tarballPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create tarball from build context.");
                throw;
            }
        }

        private async Task ExecuteBuildKitCommandAsync(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = BuildkitExecutable,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    Log.Information(args.Data);
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    Log.Error(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"BuildKit command failed with exit code {process.ExitCode}.");
        }
    }
}
