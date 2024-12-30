# uninstall_containerd_and_buildkit.ps1
Write-Host "Starting containerd and buildkit uninstallation..." -ForegroundColor Green

try {
    # Stop and remove containerd service
    Write-Host "Stopping and removing containerd service..." -ForegroundColor Cyan
    if (Get-Service -Name "containerd" -ErrorAction SilentlyContinue) {
        Stop-Service -Name "containerd" -Force
        & "$Env:ProgramFiles\containerd\containerd.exe" --unregister-service
    }

    # Remove containerd files
    Write-Host "Removing containerd files..." -ForegroundColor Cyan
    if (Test-Path "$Env:ProgramFiles\containerd") {
        Remove-Item -Path "$Env:ProgramFiles\containerd" -Recurse -Force
    }

    # Remove buildkit files
    Write-Host "Removing buildkit files..." -ForegroundColor Cyan
    if (Test-Path "$Env:ProgramFiles\buildkit") {
        Remove-Item -Path "$Env:ProgramFiles\buildkit" -Recurse -Force
    }

    # Remove buildkit from PATH
    Write-Host "Removing buildkit from PATH..." -ForegroundColor Cyan
    $Path = [Environment]::GetEnvironmentVariable("PATH", "Machine") -replace [Regex]::Escape(";$Env:ProgramFiles\buildkit"), ""
    [Environment]::SetEnvironmentVariable("Path", $Path, "Machine")

    Write-Host "containerd and buildkit uninstalled successfully." -ForegroundColor Green
} catch {
    Write-Error "Error during uninstallation: $_"
    exit 1
}

