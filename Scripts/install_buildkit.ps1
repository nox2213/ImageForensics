
# Check if the directory exists; if not, create it
$directoryPath = "src\containerd+buildkit"
if (-not (Test-Path -Path $directoryPath)) {
    New-Item -ItemType Directory -Path $directoryPath | Out-Null
}

# Define the tar.gz file path and check if a new version is available
$tarFilePath = Join-Path $directoryPath "package.tar.gz"
$remoteUri = "https://example.com/path/to/latest/package.tar.gz" # Replace with actual URL

if (Test-Path -Path $tarFilePath) {
    # Compare local and remote versions
    $remoteVersion = (Invoke-WebRequest -Uri $remoteUri -Method Head).Headers["Last-Modified"]
    $localVersion = (Get-Item -Path $tarFilePath).LastWriteTime

    if ($remoteVersion -gt $localVersion) {
        # Remove old file and download the new version
        Remove-Item -Path $tarFilePath -Force
        Invoke-WebRequest -Uri $remoteUri -OutFile $tarFilePath
    }
} else {
    # Download the package if it doesn't exist
    Invoke-WebRequest -Uri $remoteUri -OutFile $tarFilePath
}

# Use the tar.gz file
Write-Host "Using package from $tarFilePath"

# Check if script is running as administrator
if (-Not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator."
    exit 1
}
Stop-Service buildkit
# Define variables
$url = "https://api.github.com/repos/moby/buildkit/releases/latest"
$version = (Invoke-RestMethod -Uri $url -UseBasicParsing).tag_name
$arch = "amd64" # Use arm64 if necessary
$TempDir = "$Env:TEMP\buildkit"
$TargetDir = "$Env:ProgramFiles\buildkit"

# Ensure temporary directory exists
if (-Not (Test-Path $TempDir)) {
    New-Item -ItemType Directory -Path $TempDir | Out-Null
}

# Download BuildKit tarball
$tarballPath = "$TempDir\buildkit-$version.windows-$arch.tar.gz"
Write-Host "Downloading BuildKit version $version for $arch..." -ForegroundColor Cyan
Invoke-WebRequest -Uri "https://github.com/moby/buildkit/releases/download/$version/buildkit-$version.windows-$arch.tar.gz" -OutFile $tarballPath

# Extract BuildKit binaries using tar.exe
Write-Host "Extracting BuildKit binaries using tar.exe..." -ForegroundColor Cyan
if (-Not (Get-Command "tar.exe" -ErrorAction SilentlyContinue)) {
    Write-Error "tar.exe is not available. Please install it and ensure it's in your PATH."
    exit 1
}

tar.exe -xvf $tarballPath -C $TempDir

# Copy binaries to target directory
Write-Host "Copying BuildKit binaries to $TargetDir..." -ForegroundColor Cyan
if (-Not (Test-Path $TargetDir)) {
    New-Item -ItemType Directory -Path $TargetDir | Out-Null
}

Copy-Item -Path "$TempDir\*" -Destination $TargetDir -Recurse -Force

# Add BuildKit to PATH
$Path = [Environment]::GetEnvironmentVariable("PATH", "Machine") + [IO.Path]::PathSeparator + $TargetDir
[Environment]::SetEnvironmentVariable("Path", $Path, "Machine")
$Env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine")

# Cleanup temporary files
Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
Remove-Item -Path $TempDir -Recurse -Force

Write-Host "BuildKit installation completed successfully." -ForegroundColor Green
exit 0
