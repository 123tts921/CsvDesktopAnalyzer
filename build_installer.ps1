param(
    [switch]$ForcePublish
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".dotnet"
$env:HOME = $env:DOTNET_CLI_HOME
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$publishDir = Join-Path $projectRoot "publish\CsvDesktopAnalyzerSingleFileSelfContained"
$publishExe = Join-Path $publishDir "CsvDesktopAnalyzer.exe"
$outputDir = Join-Path $projectRoot "publish"
$outputInstaller = Join-Path $outputDir "CsvDesktopAnalyzerSetup.exe"
$installerSource = Join-Path $projectRoot "installer\InstallerProgram.cs"
$resourceArg = "/resource:`"$publishExe`",CsvDesktopAnalyzerPayload.exe"
$frameworkCsc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$frameworkRefs = @(
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll"
)

New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME | Out-Null
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

if ($ForcePublish -or -not (Test-Path $publishExe)) {
    Write-Host "Publishing app..."
    dotnet publish .\CsvDesktopAnalyzer\CsvDesktopAnalyzer.csproj `
      -c Release `
      -r win-x64 `
      --self-contained true `
      -p:PublishSingleFile=true `
      -p:PublishTrimmed=false `
      -p:EnableCompressionInSingleFile=true `
      -p:IncludeNativeLibrariesForSelfExtract=true `
      -o .\publish\CsvDesktopAnalyzerSingleFileSelfContained `
      --configfile .\NuGet.Config

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}
else {
    Write-Host "Reusing existing app: $publishExe"
}

if (-not (Test-Path $frameworkCsc)) {
    throw "C# compiler not found: $frameworkCsc"
}

if (-not (Test-Path $installerSource)) {
    throw "Installer source not found: $installerSource"
}

Write-Host "Building setup..."
& $frameworkCsc `
  /nologo `
  /target:winexe `
  /optimize+ `
  /out:$outputInstaller `
  $resourceArg `
  $frameworkRefs `
  $installerSource

if ($LASTEXITCODE -ne 0 -or -not (Test-Path $outputInstaller)) {
    throw "Installer build failed."
}

Write-Host "Setup created: $outputInstaller"
