
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

# Check if script is running as Administrator
if (-Not ([Security.Principal.WindowsPrincipal]([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "The script must be run as Administrator. Exiting..."
    exit 1
}
Stop-Service containerd
Write-Host "Starting containerd installation..." -ForegroundColor Green

try {
    # Define variables
    $Version = "2.0.1"
    $Arch = "amd64"
    $DownloadUrl = "https://github.com/containerd/containerd/releases/download/v$Version/containerd-$Version-windows-$Arch.tar.gz"
    $TempDir = "$Env:TEMP\containerd"
    $ExtractDir = "$TempDir\containerd-$Version"
    $TargetDir = "$Env:ProgramFiles\containerd"

    # Ensure temporary directory exists
    if (-Not (Test-Path $TempDir)) {
        New-Item -ItemType Directory -Path $TempDir | Out-Null
    }

    # Download containerd
    Write-Host "Downloading containerd from $DownloadUrl..." -ForegroundColor Cyan
    $TarballPath = "$TempDir\containerd.tar.gz"
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $TarballPath -ErrorAction Stop

    # Extract containerd
    Write-Host "Extracting containerd binaries using tar..." -ForegroundColor Cyan
    if (Test-Path $ExtractDir) {
        Remove-Item -Path $ExtractDir -Recurse -Force
    }
    $TarExe = "tar.exe" # Ensure 'tar' is available on the system
    if (-Not (Get-Command $TarExe -ErrorAction SilentlyContinue)) {
        Write-Error "'tar.exe' not found. Ensure it is available in the system PATH."
        exit 1
    }
    & $TarExe -xzf $TarballPath -C $TempDir

    # Check if extraction was successful
    if (-Not (Test-Path "$TempDir\bin")) {
        Write-Error "Extraction failed: 'bin' folder not found in $TempDir"
        exit 1
    }

    # Copy binaries to target directory
    Write-Host "Copying containerd binaries to $TargetDir..." -ForegroundColor Cyan
    if (-Not (Test-Path $TargetDir)) {
        New-Item -ItemType Directory -Path $TargetDir | Out-Null
    }
    Copy-Item -Path "$TempDir\bin\*" -Destination $TargetDir -Recurse -Force

    # Register containerd as a service
    Write-Host "Configuring and registering containerd as a Windows service..." -ForegroundColor Cyan
    & "$TargetDir\containerd.exe" config default | Out-File -Encoding ASCII "$TargetDir\config.toml"
    & "$TargetDir\containerd.exe" --register-service

    # Start containerd service
    Start-Service -Name "containerd"
    Write-Host "Containerd service started successfully." -ForegroundColor Green
} catch {
    Write-Error "Error during containerd installation: $_"
    exit 1
} finally {
    # Cleanup temporary files
    if (Test-Path $TempDir) {
        Remove-Item -Path $TempDir -Recurse -Force
    }
}
