$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ProjectDir

python -m PyInstaller `
  --noconfirm `
  --clean `
  --onefile `
  --windowed `
  --name "CSVChartViewer" `
  "csv_chart_viewer.py"

Write-Host ""
Write-Host "Build complete: $ProjectDir\dist\CSVChartViewer.exe"
