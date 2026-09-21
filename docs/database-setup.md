# Production database setup

This documents how the production database gets stood up on the GoDaddy
**hosted SQL Server** (a separate managed database, not SQL Server running on
the VPS itself). It's a managed/hosted instance behind Plesk, and in practice
**direct external SQL connectivity from a dev machine doesn't work** - Plesk's
own connection host string (`.\MSSQLSERVER2022:0`) is a local-machine-only
reference, named-instance resolution (UDP 1434) isn't reachable externally,
and a direct port-1433 attempt times out (firewalled). So instead of
`sqlpackage` over a live connection, this uses **Plesk's built-in "Import
dump" feature** (Databases > EcoGoodz > Import dump), which uploads a native
SQL Server `.bak` file over HTTPS through the browser - no external
connectivity to the database required at all.

## Step 1 - Produce a clean, production-ready `.bak`

The local Docker database (`db/restore.sh` + `db/normalize.sql` +
`db/add_foreign_keys.sql`) is the source of truth for schema and legacy
business data. Its `AspNetUsers`/`AspNetRoles`/etc. Identity tables, however,
hold local dev test accounts that must never reach production. Rather than
running a cleanup script against the hosted DB after import (not possible -
Plesk has no MS SQL query console), clean a **temporary duplicate** of the
local DB before ever exporting it, so the `.bak` handed to Plesk is already
production-safe:

```powershell
# 1. Back up the local (already normalized) dev DB
docker exec ecogoodz-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C -Q "BACKUP DATABASE [EcoGoodz] TO DISK = N'/var/opt/mssql/data/EcoGoodz_export.bak' WITH FORMAT, INIT;"

# 2. Restore it into a throwaway duplicate DB
docker exec ecogoodz-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C -Q "RESTORE DATABASE [EcoGoodzExport] FROM DISK = N'/var/opt/mssql/data/EcoGoodz_export.bak' WITH MOVE N'Ecogoodz' TO N'/var/opt/mssql/data/EcoGoodzExport.mdf', MOVE N'Ecogoodz_log' TO N'/var/opt/mssql/data/EcoGoodzExport_log.ldf', REPLACE;"

# 3. Clear Identity tables in the duplicate only (db/reset-identity-tables.sql logic)
docker exec ecogoodz-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C -d EcoGoodzExport -i db/reset-identity-tables.sql

# 4. Back up the cleaned duplicate - THIS is the file to import into production
docker exec ecogoodz-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C -Q "BACKUP DATABASE [EcoGoodzExport] TO DISK = N'/var/opt/mssql/data/EcoGoodz_prod_ready.bak' WITH FORMAT, INIT;"
docker cp ecogoodz-sqlserver:/var/opt/mssql/data/EcoGoodz_prod_ready.bak .\backup\EcoGoodz_prod_ready.bak

# 5. Clean up the duplicate DB and scratch files in the container
docker exec ecogoodz-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C -Q "ALTER DATABASE [EcoGoodzExport] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [EcoGoodzExport];"
docker exec ecogoodz-sqlserver rm -f /var/opt/mssql/data/EcoGoodz_export.bak /var/opt/mssql/data/EcoGoodz_prod_ready.bak /var/opt/mssql/data/EcoGoodzExport.mdf /var/opt/mssql/data/EcoGoodzExport_log.ldf
```

The resulting `.bak` is never committed - it contains real legacy business
data and is trivially regenerated on demand.

## Step 2 - Import via Plesk's UI

In Plesk: **Databases > EcoGoodz > Import dump**, select the `.bak` produced
above, upload it. This restores the full schema (business tables + empty
Identity tables) and all current legacy data directly on the server - no
external SQL connectivity needed.

> If the upload is rejected for being too large, check Plesk's PHP upload
> size limit (Tools & Settings > PHP Settings) and raise it, or use the
> Plesk File Manager to upload the `.bak` into a temp folder first and point
> Import dump at that server-side path instead.

## Step 3 - (Skipped)

Not needed - the `.bak` from Step 1 already has empty Identity tables.

## Step 4 - Configure the app and let it finish the job

Set these on the production site in Plesk (see `docs/deployment.md` for
where):

| Environment variable | Value |
|---|---|
| `ConnectionStrings__EcoGoodz` | The hosted SQL Server connection string |
| `Migration__SeedLegacyUsers` | `true` |

On its **first startup**, the app will automatically:
1. Apply any pending Identity schema migrations (`Database.MigrateAsync()` in
   `Program.cs`) - covers this and any future Identity migration without ever
   needing `dotnet ef`/the SDK on the server.
2. Re-seed `AspNetRoles` (`IdentitySeeder`).
3. Re-migrate every legacy `User` row into a fresh `AspNetUsers` row
   (`LegacyUserMigrator`) - each with no password set, `MustChangePassword`
   forced, so real staff set their own password via "Forgot your password?"
   on first login.

`Migration__SeedLegacyUsers=true` is safe to leave set permanently - both
steps are idempotent (skip anything already migrated), so subsequent
restarts are no-ops.

## Keeping production data current

If the legacy backup is re-normalized later (e.g. a new tier from
`docs/legacy-controller-inventory.md` needs a schema change captured in
`db/normalize.sql`), repeat Step 1 and re-upload via Plesk's Import dump.
Note this **overwrites the whole database**, so once production has real
data of its own (new orders, new users placed after go-live), a full
re-import would destroy it - at that point, schema changes need to be applied
as targeted `ALTER` scripts against production instead (via a one-off page in
the app, or by getting direct SQL access resolved with GoDaddy support).

## If direct SQL connectivity ever gets resolved

If GoDaddy/Plesk support opens external access to the hosted SQL Server
(firewall allow-list, or a fixed non-default port), `sqlpackage` becomes
useful for incremental schema-only syncs (`/Action:Publish`) without
re-uploading a full `.bak` each time:

```powershell
dotnet tool install -g microsoft.sqlpackage
sqlpackage /Action:Publish `
  /SourceFile:"db/EcoGoodz.bacpac" `
  /TargetConnectionString:"<connection string from .env>"
```
