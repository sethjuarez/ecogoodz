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
IIS site/app pool (Plesk exposes these under the domain's hosting settings,
often labeled "Environment variables" for .NET Core sites, or settable via
IIS Manager's `<aspNetCore>` config if not). ASP.NET Core's configuration
system reads double-underscore-delimited env vars as nested config keys, so
set:

| Environment variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__EcoGoodz` | Real production SQL Server connection string |
| `Smtp__Host`, `Smtp__Port`, `Smtp__UserName`, `Smtp__Password`, `Smtp__FromAddress` | Real SMTP credentials (see `SmtpOptions.cs`) |

**This step still needs to happen once, directly in Plesk**, before the app
will start successfully in production - it will fail to connect to a
database otherwise. This is the next concrete action once you're back in the
Plesk UI.

## TLS

The GoDaddy VPS TLS certificate needs to match `app.ecogoodz.com` specifically
(a subdomain) - see the open item in `docs/security-review.md`. Plesk's
Let's Encrypt integration (SSL/TLS Certificates under the domain) is the
easiest path if it isn't already covering that subdomain.

## First deploy checklist

1. [ ] Confirm Hosting Bundle (not just runtime) is unnecessary, but ASP.NET
       Core runtime + IIS module *is* installed on the VPS.
2. [ ] Set the environment variables above in Plesk for `app.ecogoodz.com`.
3. [ ] Create the Git repository in Plesk with the settings above (this
       triggers the first pull once `deploy` branch exists).
4. [ ] Confirm/replace the app pool name in the deployment-actions script.
5. [ ] Push to `main` (or re-run the `Publish to deploy branch` workflow
       manually) to create the initial `deploy` branch.
6. [ ] Verify the site loads at `https://app.ecogoodz.com` and can reach the
       database.
7. [ ] Confirm TLS certificate covers `app.ecogoodz.com`.
