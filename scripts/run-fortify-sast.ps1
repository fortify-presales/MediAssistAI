[CmdletBinding()]
param(
    [string]$SolutionPath = "MediAssistAI.sln",
    [string]$OutputDirectory = "artifacts\fortify\sast",
    [string]$BuildId = "MediAssistAI-local",
    [string]$DotnetRoot,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command sourceanalyzer -ErrorAction SilentlyContinue)) {
    throw "Fortify Static Code Analyzer (sourceanalyzer) was not found on PATH."
}

$resolvedSolutionPath = Resolve-Path -Path $SolutionPath
$resolvedOutputDirectory = Join-Path (Get-Location) $OutputDirectory
$fprPath = Join-Path $resolvedOutputDirectory "MediAssistAI.fpr"

if ([string]::IsNullOrWhiteSpace($DotnetRoot)) {
    $candidateRoots = @(
        $env:DOTNET_ROOT,
        (Join-Path $env:USERPROFILE ".dotnet"),
        (Join-Path $env:USERPROFILE "dotnet9"),
        (Join-Path $env:ProgramFiles "dotnet")
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    $DotnetRoot = $candidateRoots | Where-Object {
        (Test-Path (Join-Path $_ "dotnet.exe")) -and
        (Test-Path (Join-Path $_ "sdk")) -and
        (Get-ChildItem (Join-Path $_ "sdk") -Directory -ErrorAction SilentlyContinue | Select-Object -First 1)
    } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($DotnetRoot)) {
    throw "No .NET SDK root was found. Set -DotnetRoot to the directory containing dotnet.exe and sdk."
}

$dotnetPath = Join-Path $DotnetRoot "dotnet.exe"
$env:DOTNET_ROOT = $DotnetRoot
& $dotnetPath --version
if ($LASTEXITCODE -ne 0) {
    throw "The .NET SDK at '$DotnetRoot' could not be used."
}

New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

Write-Host "Cleaning Fortify build session '$BuildId'..."
& sourceanalyzer -b $BuildId -clean

if (-not $SkipBuild) {
    Write-Host "Translating $resolvedSolutionPath..."
    & sourceanalyzer -b $BuildId $dotnetPath build $resolvedSolutionPath --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Fortify translation failed with exit code $LASTEXITCODE."
    }

    $translatedFiles = & sourceanalyzer -b $BuildId -show-files
    if ($LASTEXITCODE -ne 0 -or -not $translatedFiles) {
        throw "Fortify translation produced no input files. Check the build output above before scanning."
    }
}

Write-Host "Scanning translated sources..."
& sourceanalyzer -b $BuildId -scan -f $fprPath
if ($LASTEXITCODE -ne 0) {
    throw "Fortify scan failed with exit code $LASTEXITCODE."
}

Write-Host "Fortify SAST scan completed: $fprPath"
Write-Host "Open the FPR in Audit Workbench, or import it into FoD/SSC using your approved workflow."