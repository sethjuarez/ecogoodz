param(
    [Parameter(Mandatory = $true)]
    [string] $AppPoolName,

    [string] $LivePath = "",

    [int] $ShutdownDelaySeconds = 5
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$stagingRoot = Split-Path -Parent $scriptRoot

if ([string]::IsNullOrWhiteSpace($LivePath)) {
    $domainRoot = Split-Path -Parent $stagingRoot
    $LivePath = Join-Path $domainRoot "httpdocs"
}

$stagingFullPath = [System.IO.Path]::GetFullPath($stagingRoot)
$liveFullPath = [System.IO.Path]::GetFullPath($LivePath)

if ($stagingFullPath.TrimEnd("\") -ieq $liveFullPath.TrimEnd("\")) {
    throw "Staging path and live path must be different. Configure Plesk Git Server path to a staging folder, not httpdocs."
}

$offlinePath = Join-Path $liveFullPath "app_offline.htm"
$offlineHtml = @"
<!doctype html>
<html>
<head><meta charset="utf-8"><title>EcoGoodz updating</title></head>
<body style="font-family: system-ui, sans-serif; margin: 3rem;">
  <h1>EcoGoodz is updating</h1>
  <p>The app is deploying a new version. Please refresh in a minute.</p>
</body>
</html>
"@

Import-Module WebAdministration

New-Item -ItemType Directory -Path $liveFullPath -Force | Out-Null
Set-Content -Path $offlinePath -Value $offlineHtml -Encoding UTF8

if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Stop-WebAppPool -Name $AppPoolName
}
else {
    throw "App pool '$AppPoolName' was not found."
}

Start-Sleep -Seconds $ShutdownDelaySeconds

$robocopyArgs = @(
    $stagingFullPath,
    $liveFullPath,
    "/MIR",
    "/XD", ".git", "deployment",
    "/XF", "app_offline.htm",
    "/R:3",
    "/W:2",
    "/NFL",
    "/NDL",
    "/NP"
)

& robocopy @robocopyArgs
$robocopyExitCode = $LASTEXITCODE
if ($robocopyExitCode -gt 7) {
    throw "Robocopy failed with exit code $robocopyExitCode."
}

Remove-Item -Path $offlinePath -Force -ErrorAction SilentlyContinue
Start-WebAppPool -Name $AppPoolName
