<#
.SYNOPSIS
    Converts the $vars hashtable in Set-ProdEnvVars.local.ps1 into a nested
    appsettings.Production.json file for the external-config-file approach.

.WHY
    Plesk periodically regenerates a domain's IIS config (applicationHost.config)
    from its own internal database - triggered by clicking "Deploy"/"Fetch" on
    the Git tab and other panel actions - and silently drops any <location>
    block / environmentVariables collection it doesn't recognize. That wiped
    out every custom env var Set-ProdEnvVars(.local).ps1 had set, more than
    once in production. A JSON file living OUTSIDE httpdocs (so git deploys
    never touch it) and outside applicationHost.config (so Plesk's config
    regen never touches it) survives both. See docs/deployment.md.

.USAGE
    Run this from the repo root (needs Set-ProdEnvVars.local.ps1 next to it -
    that file is never committed, see .gitignore):
        .\scripts\ConvertTo-ExternalConfig.ps1
    Produces scripts\appsettings.Production.local.json (also gitignored).
    Upload THAT file to the VPS as:
        C:\Inetpub\vhosts\app.ecogoodz.com\private\appsettings.Production.json
    Then delete it from this machine and lock down its ACL on the VPS (see
    docs/deployment.md).

    This only parses Set-ProdEnvVars.local.ps1's text as literal
    "Key" = "Value" / "Key" = 'Value' lines - it never dot-sources or executes
    that script, so it works even without IIS/WebAdministration installed.
#>

$ErrorActionPreference = "Stop"

$sourcePath = Join-Path $PSScriptRoot "Set-ProdEnvVars.local.ps1"
if (-not (Test-Path $sourcePath)) {
    throw "Set-ProdEnvVars.local.ps1 not found next to this script. Run this from a checkout that has your local secrets file."
}

$pairs = [ordered]@{}

foreach ($line in Get-Content $sourcePath) {
    if ($line -match '^\s*"([^"]+)"\s*=\s*(?:''([^'']*)''|"([^"]*)")\s*$') {
        $key = $Matches[1]
        $value = if ($Matches[2]) { $Matches[2] } else { $Matches[3] }
        $pairs[$key] = $value
    }
}

if ($pairs.Count -eq 0) {
    throw "No key/value pairs parsed - is the `$vars = [ordered]@{ ... }` block format in Set-ProdEnvVars.local.ps1 unchanged?"
}

# Build nested structure from "Section__Key" -> { "Section": { "Key": value } }
$root = [ordered]@{}
foreach ($key in $pairs.Keys) {
    $parts = $key -split '__'
    $node = $root
    for ($i = 0; $i -lt $parts.Length - 1; $i++) {
        if (-not $node.Contains($parts[$i])) {
            $node[$parts[$i]] = [ordered]@{}
        }
        $node = $node[$parts[$i]]
    }
    $node[$parts[-1]] = $pairs[$key]
}

$outputPath = Join-Path $PSScriptRoot "appsettings.Production.local.json"
$root | ConvertTo-Json -Depth 10 | Set-Content -Path $outputPath -Encoding UTF8

Write-Host "Wrote $outputPath" -ForegroundColor Green
Write-Host ""
Write-Host "Upload this file to the VPS as:" -ForegroundColor Cyan
Write-Host "  C:\Inetpub\vhosts\app.ecogoodz.com\private\appsettings.Production.json" -ForegroundColor Cyan
Write-Host ""
Write-Host "Then delete it from this machine (it contains plaintext secrets)." -ForegroundColor Yellow
