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
3. Plesk's Git integration pulls the `deploy` branch into `\httpdocs`
   whenever it changes, then runs a small recycle script.

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
| Server path | `\httpdocs` |
| Enable additional deployment actions | **Yes** |

### Additional deployment actions script

Paste this in the deployment-actions box (adjust the app pool name to match
whatever Plesk named it for `app.ecogoodz.com` - check IIS Manager or Plesk's
"IIS Application Pool" section under the domain's hosting settings):

```powershell
Import-Module WebAdministration
Restart-WebAppPool -Name "<app-pool-name-for-app.ecogoodz.com>"
```

This forces the ASP.NET Core Module to pick up the newly deployed
`EcoGoodz.Web.dll` and reload configuration. Without it, IIS may keep serving
the previous build until the app pool recycles on its own schedule.

## Secrets and environment-specific settings

**Nothing production-sensitive is ever committed** - not to `main`, not to
the `deploy` branch. `appsettings.Development.json` (local Docker connection
string, dev-only flags) is explicitly excluded from publish output; see the
`CopyToPublishDirectory="Never"` item in `EcoGoodz.Web.csproj`. There is no
`appsettings.Production.json` in the repo at all.

Production configuration must be supplied as **environment variables** on the
IIS site/app pool. ASP.NET Core's configuration system reads
double-underscore-delimited env vars as nested config keys, so set:

| Environment variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__EcoGoodz` | Real production SQL Server connection string (see `docs/database-setup.md`) |
| `Migration__SeedLegacyUsers` | `true` (one-time bootstrap, safe to leave set - see `docs/database-setup.md`) |
| `Smtp__Host`, `Smtp__Port`, `Smtp__UserName`, `Smtp__Password`, `Smtp__FromAddress` | Amazon SES SMTP credentials (see below and `SmtpOptions.cs`) |
| `Smtp__TenantName` | SES tenant name (`ecogoodz`) - isolates this app's sending reputation from other projects in the same personal AWS account (see below) |
| `DataProtection__KeysDirectory` | A folder **outside** `httpdocs` (e.g. `C:\Inetpub\vhosts\app.ecogoodz.com\private\dp-keys`) to persist the DataProtection key ring - see "DataProtection keys" below. Without this, antiforgery tokens/reset links/auth cookies break on every app pool recycle. |

### Where these actually get set (not Plesk's UI)

This Plesk installation has no built-in "Environment variables" panel for
.NET Core sites, and the two things that look like they might work don't:

- The classic **"ASP.NET Configuration for Website"** panel (Framework
  4.8.0, VB.NET, Windows auth, "Connection string manager") is entirely for
  classic ASP.NET (`System.Web`). ASP.NET Core doesn't read any of it -
  leave this panel untouched.
- Plesk's separately-installable **".NET Toolkit" extension** does add an
  environment-variables UI, but it writes them into `web.config` - which
  our `deploy` branch **force-pushes a freshly regenerated `web.config` on
  every deploy**, silently wiping anything set there (a known Plesk issue,
  EXTPLESK-5018). Not usable with our CI/CD model.

The deploy-safe location is **`applicationHost.config`** (server/site level)
- it's never touched by `dotnet publish` or a `deploy` branch pull, and it's
never served over HTTP. Setting it requires real Windows admin access to the
VPS (RDP), not just Plesk's webspace/FTP system user:

1. RDP to the VPS as `Administrator` (GoDaddy: **My Products → Servers →
   Manage → Settings → Access → Login credentials** to set/reset that
   password; if RDP itself is unreachable, use **Server Actions → Recovery
   Console** as a no-password fallback to log into Windows directly).
2. Either use IIS Manager's **Configuration Editor** (section
   `system.webServer/aspNetCore`, **From:** dropdown set to
   `ApplicationHost.config <location path='...'>` - not `Web.config` -
   then edit the `environmentVariables` collection), or run
   `scripts/Set-ProdEnvVars.ps1` from an elevated PowerShell prompt, which
   does the same thing via `WebAdministration` cmdlets and prompts for each
   secret interactively (masked for passwords, nothing written to disk).
3. Recycle the app pool afterward (Plesk's "Recycle" button, or the script
   does it automatically).

**This step still needs to happen once**, directly on the VPS via RDP,
before the app will start successfully in production - it will fail to
connect to a database otherwise.

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
2. [ ] Set the environment variables above via RDP + `applicationHost.config`
       (see "Where these actually get set" above) - not Plesk's UI.
3. [ ] Create the Git repository in Plesk with the settings above (this
       triggers the first pull once `deploy` branch exists).
4. [ ] Confirm/replace the app pool name in the deployment-actions script.
5. [ ] Push to `main` (or re-run the `Publish to deploy branch` workflow
       manually) to create the initial `deploy` branch.
6. [ ] Request SES production access (or verify each real staff email
       individually) before relying on password-reset emails for anyone
       outside the sandbox-verified test list.
7. [ ] Verify the site loads at `https://app.ecogoodz.com` and can reach the
       database.
8. [ ] Confirm TLS certificate covers `app.ecogoodz.com`.
