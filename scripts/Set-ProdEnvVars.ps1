<#
.SYNOPSIS
    Sets ASP.NET Core environment variables for the EcoGoodz site at the
    ApplicationHost.config (server) level, so they survive every deploy
    (deploy pushes only overwrite web.config, never applicationHost.config).

.USAGE
    Run this INSIDE an elevated PowerShell session on the production VPS
    (via RDP as Administrator). Do not run this from your laptop.

    Right-click PowerShell -> "Run as Administrator", then:
        cd C:\path\you\uploaded\this\to
        .\Set-ProdEnvVars.ps1

    You will be prompted for each value. Passwords are entered as secure
    strings (masked) and never written to disk by this script.

.NOTES
    - Site name below must match the IIS site name exactly as shown in
      IIS Manager's left-hand tree.
    - After running, recycle the app pool (Plesk "Recycle" button, or
      Restart-WebAppPool below) for the new variables to take effect.
#>

Import-Module WebAdministration

# ---- CONFIGURE THIS ----
$siteName = "app.ecogoodz.com"
# -------------------------

$pspath = "MACHINE/WEBROOT/APPHOST/$siteName"
$section = "system.webServer/aspNetCore/environmentVariables"

function Read-PlainOrSecure([string]$prompt, [bool]$secure) {
    if ($secure) {
        $sec = Read-Host -Prompt $prompt -AsSecureString
        $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
        try {
            return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
        } finally {
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    } else {
        return Read-Host -Prompt $prompt
    }
}

# name => whether to mask input
$vars = [ordered]@{
    "ConnectionStrings__EcoGoodz"      = $true
    "Smtp__Host"                       = $false
    "Smtp__Port"                       = $false
    "Smtp__UserName"                   = $true
    "Smtp__Password"                   = $true
    "Smtp__FromAddress"                = $false
    "Smtp__TenantName"                 = $false
    "Migration__SeedLegacyUsers"       = $false
}

Write-Host "Setting environment variables for site '$siteName' at ApplicationHost.config level..." -ForegroundColor Cyan

foreach ($name in $vars.Keys) {
    $value = Read-PlainOrSecure -prompt "Enter value for $name" -secure $vars[$name]

    if ([string]::IsNullOrWhiteSpace($value)) {
        Write-Host "Skipping $name (blank input)" -ForegroundColor Yellow
        continue
    }

    # Remove any existing entry with this name first (avoids duplicates on re-run)
    $existing = Get-WebConfigurationProperty -pspath $pspath -filter $section -name "." |
        Select-Object -ExpandProperty Collection |
        Where-Object { $_.name -eq $name }

    if ($existing) {
        Clear-WebConfiguration -pspath $pspath -filter "$section/add[@name='$name']"
    }

    Add-WebConfigurationProperty -pspath $pspath -filter $section -name "." -value @{ name = $name; value = $value }
    Write-Host "Set $name" -ForegroundColor Green
}

Write-Host ""
Write-Host "Done. Recycling app pool for site '$siteName'..." -ForegroundColor Cyan

# Plesk names the app pool after the site name in most cases; adjust if different.
$poolName = $siteName
try {
    Restart-WebAppPool -Name $poolName -ErrorAction Stop
    Write-Host "App pool '$poolName' recycled." -ForegroundColor Green
} catch {
    Write-Host "Could not recycle app pool '$poolName' automatically. Recycle it manually from Plesk (Websites & Domains -> app.ecogoodz.com -> Recycle)." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Verify with:  Get-WebConfigurationProperty -pspath '$pspath' -filter '$section' -name '.'" -ForegroundColor Cyan
