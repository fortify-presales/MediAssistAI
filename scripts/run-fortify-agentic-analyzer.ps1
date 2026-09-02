[CmdletBinding()]
param(
    [string]$ProjectDirectory = ".",
    [string]$OutputPath = "artifacts\fortify\faa\MediAssistAI.faa.sarif",
    [string]$Scope = "src/Agents,src/Api",
    [Parameter(Mandatory)]
    [string]$FortifyFprPath
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command fortifyaa -ErrorAction SilentlyContinue)) {
    throw "Fortify Agentic Analyzer (fortifyaa) was not found on PATH."
}

$resolvedProjectDirectory = Resolve-Path -Path $ProjectDirectory
$resolvedOutputPath = Join-Path (Get-Location) $OutputPath
$resolvedFortifyFprPath = Resolve-Path -Path $FortifyFprPath
New-Item -ItemType Directory -Path (Split-Path -Parent $resolvedOutputPath) -Force | Out-Null

# FAA cannot select a platform when both FoD and SSC credentials are inherited.
Get-ChildItem Env: | Where-Object Name -like "FCLI_DEFAULT_SSC_*" | ForEach-Object {
    Remove-Item "Env:$($_.Name)"
}

& fortifyaa -scan $resolvedProjectDirectory --scope $Scope --fpr $resolvedFortifyFprPath --output $resolvedOutputPath --message-format plain
if ($LASTEXITCODE -ne 0) {
    throw "Fortify Agentic Analyzer failed with exit code $LASTEXITCODE."
}

Write-Host "Fortify Agentic Analyzer scan completed: $resolvedOutputPath"