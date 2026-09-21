-- =====================================================================
-- Resets ASP.NET Core Identity tables after importing the schema+data
-- bacpac (see docs/database-setup.md). The bacpac is exported from a
-- local dev database, which may contain local test accounts/passwords -
-- those must never end up in production.
--
-- Safe/expected to run exactly once, immediately after the bacpac import
-- and before the app's first startup against the production database.
-- The app repopulates everything here automatically on its next startup:
--   - AspNetRoles       <- IdentitySeeder.SeedRolesAsync
--   - AspNetUsers, AspNetUserRoles <- LegacyUserMigrator (from dbo.[User]),
--     gated by the Migration__SeedLegacyUsers=true environment variable.
-- =====================================================================
USE [EcoGoodz];
GO
SET NOCOUNT ON;

DELETE FROM dbo.AspNetUserTokens;
DELETE FROM dbo.AspNetUserLogins;
DELETE FROM dbo.AspNetUserClaims;
DELETE FROM dbo.AspNetUserRoles;
DELETE FROM dbo.AspNetRoleClaims;
DELETE FROM dbo.AspNetUsers;
DELETE FROM dbo.AspNetRoles;

PRINT 'Identity tables reset - the app will repopulate roles and legacy users on next startup.';
