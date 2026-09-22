# Deploying to GoDaddy (Plesk)

This is the exact configuration for Plesk's Git integration panel
(**Domains → `app.ecogoodz.com` → Git Repositories → Create repository**),
plus the one-time server-side setup it depends on.

## How it works

1. Every push to `main` on GitHub triggers `.github/workflows/deploy.yml`.
2. That workflow builds the frontend (Sass/Tabler) and runs
   `dotnet publish` in Release mode, then force-pushes **only the publish
   output** (no source, no dev tooling) to a `deploy` branch - one squashed
   commit per deploy, no history.
3. Plesk's Git integration pulls the `deploy` branch. The safest setup pulls
   into a staging folder, then runs the included staged deploy script to take
   the app offline, copy files into `\httpdocs`, and start the app pool again.

The VPS never needs the .NET SDK, Node, or npm installed - it only needs the
**ASP.NET Core Hosting Bundle** (runtime + IIS module) to run the published
app. Confirm that's installed on the server before the first deploy (Plesk's
".NET" extension / IIS Manager will show installed runtimes).

## Plesk panel configuration

| Field | Value |
|---|---|
| Repository source | **Remote repository** |
| Repository URL | `https://github.com/sethjuarez/ecogoodz.git` |
| Repository name | `app.git` (default is fine) |
| Branch | `deploy` |
| Deployment mode | **Automatic** |
| Server path | `\deploy-staging` (recommended) |
| Enable additional deployment actions | **Yes** |

> **Important:** Do **not** point Plesk Git directly at `\httpdocs` for this
> ASP.NET Core app. IIS keeps loaded `.dll` files locked, so direct Git deploys
> eventually fail with `unable to unlink old 'EcoGoodz.Web.dll'` or
> `EcoGoodz.Data.dll`. Pull to `\deploy-staging` instead, then copy into
> `\httpdocs` while the app is offline.

### Additional deployment actions script

Paste this in the deployment-actions box (adjust the app pool name to match
whatever Plesk named it for `app.ecogoodz.com` - check IIS Manager or Plesk's
"IIS Application Pool" section under the domain's hosting settings):

```powershell
powershell.exe -ExecutionPolicy Bypass -File ".\deployment\Plesk-StagedDeploy.ps1" -AppPoolName "<app-pool-name-for-app.ecogoodz.com>"
```

This script runs from the staged checkout. It writes `app_offline.htm`, stops
the app pool, mirrors the staged publish output into sibling `\httpdocs` with
`robocopy`, removes `app_offline.htm`, then starts the app pool. Because Git
pulls into staging first, it never has to overwrite DLLs currently loaded by
IIS.

### If Plesk cannot replace locked DLLs

Immediate recovery:

1. In Plesk or IIS Manager, stop the app pool for `app.ecogoodz.com`.
2. In Plesk's Git page, run **Pull Updates / Deploy** again.
3. Start the app pool again.

Durable setup options:

- Switch Plesk's Git **Server path** from `\httpdocs` to `\deploy-staging`.
- Set the additional deployment action to run
  `.\deployment\Plesk-StagedDeploy.ps1` as shown above.
- After that, future Plesk pulls should not require RDP stop/deploy/start.

## Production latency / cold starts

If authenticated pages take 15-20 seconds after the site has been idle but are
fast on immediate refresh, the bottleneck is almost certainly IIS/Plesk cold
start: the app pool has stopped, then the first staff request has to start
Kestrel, load the app, apply Identity migrations, seed roles, and open the first
SQL connection before rendering the page.

Recommended IIS settings for the `app.ecogoodz.com` app pool:

| Setting | Value |
|---|---|
| Start Mode | `AlwaysRunning` |
| Idle Time-out (minutes) | `0` |
| Regular Time Interval (minutes) | `0` or an off-hours recycle schedule |

Also enable preload on the site/application if the Application Initialization
module is available:

```powershell
Import-Module WebAdministration
Set-ItemProperty 'IIS:\AppPools\<app-pool-name-for-app.ecogoodz.com>' -Name startMode -Value AlwaysRunning
Set-ItemProperty 'IIS:\AppPools\<app-pool-name-for-app.ecogoodz.com>' -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
Set-ItemProperty 'IIS:\Sites\app.ecogoodz.com' -Name applicationDefaults.preloadEnabled -Value True
```

If pages are still slow when refreshed immediately, check the ASP.NET Core logs
for `Slow request ...` warnings. The app logs any request slower than
`Diagnostics:SlowRequestThresholdMs` (default `2000`) so production can tell
whether the delay is a specific route/query or an app-pool wakeup.

The Buyer and Supplier index pages are especially sensitive to the legacy
database shape because legacy `Name` columns are unbounded strings. The app
creates guarded computed `NameSort` columns and supporting indexes at startup
for those two tables, then sorts those pages by the indexed computed columns.
If production stays slow after a deploy, verify the app pool identity can alter
the legacy schema and that `IX_Buyer_NameSort_Id` /
`IX_Supplier_NameSort_Id` exist in SQL Server.

## Secrets and environment-specific settings

**Nothing production-sensitive is ever committed** - not to `main`, not to
the `deploy` branch. `appsettings.Development.json` (local Docker connection
string, dev-only flags) is explicitly excluded from publish output; see the
`CopyToPublishDirectory="Never"` item in `EcoGoodz.Web.csproj`. There is no
`appsettings.Production.json` in the repo at all.

Production configuration is supplied by an **external JSON file** loaded by
`Program.cs`, not environment variables. The config keys are the same
`Section:Key` shape ASP.NET Core always uses (shown below with the
double-underscore env-var spelling for reference, in case you're translating
from an older setup):

| Config key | Env var equivalent | Purpose |
|---|---|---|
| n/a | `ASPNETCORE_ENVIRONMENT` | `Production` - still set via `web.config` (baked in by `<EnvironmentName>` in `EcoGoodz.Web.csproj`, regenerated every deploy, safe) |
| `ConnectionStrings:EcoGoodz` | `ConnectionStrings__EcoGoodz` | Real production SQL Server connection string (see `docs/database-setup.md`) |
| `Migration:SeedLegacyUsers` | `Migration__SeedLegacyUsers` | `true` (one-time bootstrap, safe to leave set - see `docs/database-setup.md`) |
| `Smtp:Host`, `Smtp:Port`, `Smtp:UserName`, `Smtp:Password`, `Smtp:FromAddress` | `Smtp__*` | Amazon SES SMTP credentials (see below and `SmtpOptions.cs`) |
| `Smtp:TenantName` | `Smtp__TenantName` | SES tenant name (`ecogoodz`) - isolates this app's sending reputation from other projects in the same personal AWS account (see below) |
| `DataProtection:KeysDirectory` | `DataProtection__KeysDirectory` | A folder **outside** `httpdocs` (e.g. `C:\Inetpub\vhosts\app.ecogoodz.com\private\dp-keys`) to persist the DataProtection key ring - see "DataProtection keys" below. Without this, antiforgery tokens/reset links/auth cookies break on every app pool recycle. |

### Where these actually get set (not Plesk's UI, not `applicationHost.config`)

This Plesk installation has no built-in "Environment variables" panel for
.NET Core sites, and none of the things that look like they might work do:

- The classic **"ASP.NET Configuration for Website"** panel (Framework
  4.8.0, VB.NET, Windows auth, "Connection string manager") is entirely for
  classic ASP.NET (`System.Web`). ASP.NET Core doesn't read any of it -
  leave this panel untouched.
- Plesk's separately-installable **".NET Toolkit" extension** does add an
  environment-variables UI, but it writes them into `web.config` - which
  our `deploy` branch **force-pushes a freshly regenerated `web.config` on
  every deploy**, silently wiping anything set there (a known Plesk issue,
  EXTPLESK-5018). Not usable with our CI/CD model.
- **`applicationHost.config` (server/site level) was tried and does NOT
  work reliably on this VPS either**, despite being the textbook-correct
  IIS answer. In production, the custom `environmentVariables` collection
  we added to the site's `<location>` block was silently wiped out more
  than once - confirmed to happen when clicking **Deploy/Fetch on Plesk's
  Git tab**, and suspected to also happen on other panel actions. Plesk
  periodically regenerates a domain's entire IIS config from its own
  internal database, and since it has no knowledge of our manually-added
  block, it drops it on regeneration. **Do not rely on
  `Set-ProdEnvVars.ps1`/`applicationHost.config` for anything you need to
  survive future deploys** - it's kept in the repo only as a fallback/
  emergency tool, not the primary mechanism.

The actual deploy-safe location is a **JSON file outside `httpdocs`**
(`Program.cs` loads it via `AddJsonFile(..., optional: true)` from
`C:\Inetpub\vhosts\app.ecogoodz.com\private\appsettings.Production.json` by
default, overridable with an `ExternalConfigPath` setting). Being outside
`httpdocs` means the `deploy` branch pull never touches it; being outside
`applicationHost.config` means Plesk's own config regeneration never touches
it either. It only needs to be created **once**:

1. Locally, fill in `scripts/Set-ProdEnvVars.local.ps1` (gitignored) with
   real secrets, same as before - it's now only used as the input to the
   next step, not run directly against IIS.
2. Run `scripts/ConvertTo-ExternalConfig.ps1` locally. It parses that file
   as plain text (never executes it, so no IIS tools required) and writes
   `scripts/appsettings.Production.local.json` (also gitignored).
3. RDP to the VPS as `Administrator` (GoDaddy: **My Products → Servers →
   Manage → Settings → Access → Login credentials** to set/reset that
   password; if RDP itself is unreachable, use **Server Actions → Recovery
   Console** as a no-password fallback to log into Windows directly).
   Upload that JSON file to
   `C:\Inetpub\vhosts\app.ecogoodz.com\private\appsettings.Production.json`
   (create the `private` folder if it doesn't already exist - it's also
   used for the DataProtection keys directory below).
4. Grant the app pool identity (check IIS Manager > Application Pools for
   the exact identity name, e.g. `IWPD_1(app.ecog_94i)`) read access to that
   file/folder, and Modify access specifically on the `dp-keys`
   subdirectory (DataProtection needs to write key files there):
   ```powershell
   $acl = Get-Acl 'C:\Inetpub\vhosts\app.ecogoodz.com\private'
   $rule = New-Object System.Security.AccessControl.FileSystemAccessRule('<identity>','ReadAndExecute','ContainerInherit,ObjectInherit','None','Allow')
   $acl.AddAccessRule($rule)
   Set-Acl 'C:\Inetpub\vhosts\app.ecogoodz.com\private' $acl
   ```
5. Recycle the app pool. Delete the local `.json`/`.local.ps1` copies once
   confirmed working (they contain plaintext secrets).

**This step still needs to happen once**, directly on the VPS via RDP,
before the app will start successfully in production - it will fail to
connect to a database otherwise. Unlike the `applicationHost.config`
approach, it should never need to be repeated after future deploys.

The database itself (schema + legacy data) needs to be imported into the
hosted SQL Server *before* the app's first production start - see
`docs/database-setup.md` for the exact steps (GoDaddy's hosted SQL Server
product doesn't allow a direct `.bak` file restore over the network, but
Plesk's own "Import dump" UI does).

### Why Amazon SES instead of the legacy GoDaddy mailbox

The legacy app relayed password-reset/notification email through
`Reports@ecogoodz.com` via `smtpout.secureserver.net` (see the old
`Web.config`). That mailbox turned out to actually be hosted on Microsoft 365
(GoDaddy just resells/fronts it - confirmed via the domain's MX/SPF records
pointing at `*.protection.outlook.com`), and it isn't a licensed user visible
in GoDaddy's simplified Email & Office admin panel - getting its credentials
would have meant using a real staff member's Microsoft 365 login, which we
didn't want to do.

Amazon SES gives the app its own dedicated sender identity instead:

1. In the AWS SES console (region **us-east-1**), verify the `ecogoodz.com`
   domain (**Verified identities > Create identity > Domain**) - this adds a
   few DNS records (DKIM CNAMEs, etc.) wherever `ecogoodz.com`'s DNS is
   managed.
2. Create SMTP credentials (**SMTP settings > Create SMTP credentials**) -
   this is a dedicated IAM-backed username/password pair, unrelated to any
   staff member's login.
3. Note the SMTP endpoint SES gives you (e.g.
   `email-smtp.us-east-1.amazonaws.com`, or a Mail Manager endpoint like
   `<id>.mail-manager-smtp.amazonaws.com`), port `587`, STARTTLS.
4. **New SES accounts start in "sandbox" mode** - only pre-verified
   recipient addresses can receive mail. Either verify each recipient
   individually (**Verified identities > Create identity > Email address**,
   fine for early testing) or request production access
   (**Account dashboard > Request production access**) before go-live, since
   real staff resetting their password can't all be pre-verified individually.
5. Set `Smtp__FromAddress` to any address at the verified domain (e.g.
   `no-reply@ecogoodz.com` - it doesn't need to be a real mailbox, unlike the
   legacy setup).

### SES tenant isolation

Since this SES account is a personal AWS account (may host other unrelated
projects over time), EcoGoodz sends through a dedicated **SES tenant** named
`ecogoodz`. This isolates its sending reputation, suppression list, and
enforcement policy from anything else sharing the account - a reputation
issue in one project can't pause sending for the other.

1. In the SES console, go to **Tenants > Create tenant**, name it `ecogoodz`.
2. Associate the `ecogoodz.com` verified domain identity with the tenant.
3. Create (or associate) a configuration set with the tenant - required
   before it can send.
4. Set `Smtp__TenantName=ecogoodz`. The app adds this as an `X-SES-TENANT`
   header on every outbound message (`SmtpEmailSender.cs`); if unset, mail
   sends at the account level with no tenant isolation.

### DataProtection keys

ASP.NET Core's DataProtection system encrypts antiforgery tokens, the auth
cookie, and password-reset tokens. By default it tries to persist its key
ring to the app pool identity's user profile or the registry - if IIS can't
give it either (a common default on Plesk-managed app pools unless
**Load User Profile** is explicitly enabled), it silently falls back to an
**in-memory key ring that's regenerated every process restart**. Every app
pool recycle then invalidates every outstanding antiforgery token, session
cookie, and password-reset link, breaking things like the ForgotPassword
form with a confusing `AntiforgeryValidationException` deep in the logs
(only visible with `stdoutLogEnabled="true"` in `web.config`, off by
default).

Fix (do both - either alone is enough, but both is belt-and-suspenders):

1. In IIS Manager, **Application Pools > (pool for app.ecogoodz.com) >
   Advanced Settings > Load User Profile = True**.
2. Set `DataProtection__KeysDirectory` to a folder **outside** `httpdocs`
   (survives every deploy, e.g.
   `C:\Inetpub\vhosts\app.ecogoodz.com\private\dp-keys`) - `Program.cs`
   calls `PersistKeysToFileSystem` against this path when set, so keys
   persist across recycles/restarts regardless of the app pool setting
   above.

## TLS

The GoDaddy VPS TLS certificate needs to match `app.ecogoodz.com` specifically
(a subdomain) - see the open item in `docs/security-review.md`. Plesk's
Let's Encrypt integration (SSL/TLS Certificates under the domain) is the
easiest path if it isn't already covering that subdomain.

## First deploy checklist

1. [ ] Confirm Hosting Bundle (not just runtime) is unnecessary, but ASP.NET
       Core runtime + IIS module *is* installed on the VPS.
2. [x] Create `appsettings.Production.json` outside `httpdocs` via RDP (see
       "Where these actually get set" above) - not Plesk's UI, not
       `applicationHost.config`.
3. [ ] Create the Git repository in Plesk with the settings above (this
       triggers the first pull once `deploy` branch exists).
4. [ ] Confirm/replace the app pool name in the deployment-actions script.
5. [ ] Push to `main` (or re-run the `Publish to deploy branch` workflow
       manually) to create the initial `deploy` branch.
6. [x] Request SES production access - **done**, account is out of sandbox
       mode.
7. [ ] Verify the site loads at `https://app.ecogoodz.com` and can reach the
       database.
8. [ ] Confirm TLS certificate covers `app.ecogoodz.com`.
