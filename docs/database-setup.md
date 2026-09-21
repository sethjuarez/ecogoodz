# Production database setup

This documents how the production database gets stood up on the GoDaddy
**hosted SQL Server** (a separate managed database, not SQL Server running on
the VPS itself). Because it's a managed/hosted instance, there is no
filesystem access to it - `RESTORE DATABASE ... FROM DISK` (what
`db/restore.sh` does locally against Docker) is not possible. Instead this
uses `sqlpackage`, which moves schema+data over a normal SQL connection.

## One-time setup on your machine

```powershell
dotnet tool install -g microsoft.sqlpackage
```

## Step 1 - Prepare a clean local snapshot

The local Docker database (`db/restore.sh` + `db/normalize.sql` +
`db/add_foreign_keys.sql`) is the source of truth for schema and legacy
business data. Make sure it's up to date, then export it to a `.bacpac`
(schema + data, portable, no filesystem access needed to consume it):

```powershell
sqlpackage /Action:Export `
  /SourceServerName:"localhost,14330" /SourceDatabaseName:"EcoGoodz" `
  /SourceUser:"sa" /SourcePassword:"<local sa password, see docker-compose.yml>" `
  /SourceTrustServerCertificate:True `
  /TargetFile:"db/EcoGoodz.bacpac"
```

This file is **never committed** (`db/*.bacpac` is gitignored) - it contains
real legacy business data and is trivially regenerated on demand.

> Note: if you ever see `SQL71564: ... has been orphaned from its login`,
> some leftover legacy SQL Server users/schemas need to be dropped first (this
> happened once with `ecogoodzuser`/`gdtechtest` - empty schemas left over
> from the old GoDaddy shared-hosting setup). Confirm the schema has no
> tables, then `DROP SCHEMA` / `DROP USER` for that principal.

## Step 2 - Import into the hosted SQL Server

Using the connection string GoDaddy provides for the hosted database (keep it
in your local `.env`, never commit it):

```powershell
sqlpackage /Action:Import `
  /TargetConnectionString:"<connection string from .env>" `
  /SourceFile:"db/EcoGoodz.bacpac"
```

This creates the database on the hosted server with the full schema
(business tables + the ASP.NET Identity tables) and all current legacy data.

## Step 3 - Reset Identity tables

The bacpac's `AspNetUsers`/`AspNetRoles`/etc. data came from your local dev
database (test accounts, dev passwords) - that must never reach production.
Run `db/reset-identity-tables.sql` against the hosted database once, right
after the import:

```powershell
sqlcmd -S <hosted server> -d EcoGoodz -U <admin login> -P <admin password> -C -i db/reset-identity-tables.sql
```

(Or run it via SSMS/Azure Data Studio connected to the hosted instance -
whichever is easiest.)

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

## Why not just restore the `.bak` directly?

`RESTORE DATABASE ... FROM DISK` requires the backup file to be visible on
the SQL Server's own filesystem, which a hosted/managed database product
doesn't expose. `sqlpackage` only needs a normal SQL connection (over TDS,
same as the app itself uses), so it works regardless of hosting model - and
it's the same mechanism used whether the database ends up hosted by GoDaddy,
Azure SQL, or anywhere else in the future.

## Keeping production data current

If the legacy backup is re-normalized later (e.g. a new tier from
`docs/legacy-controller-inventory.md` needs a schema change captured in
`db/normalize.sql`), repeat Steps 1-2 with `/Action:Publish` instead of
`/Action:Import` (schema-only sync, preserves existing data) once production
has real data of its own - re-importing the full bacpac at that point would
overwrite live data.
