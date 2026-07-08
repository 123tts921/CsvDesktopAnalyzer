$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ProjectDir

python -m PyInstaller `
  --noconfirm `
  --clean `
  --onefile `
  --windowed `
  --name "CSVFilterTool" `
  "csv_filter_app.py"

Write-Host ""
Write-Host "Build complete: $ProjectDir\dist\CSVFilterTool.exe"
