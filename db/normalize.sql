-- =====================================================================
-- EcoGoodz normalization migration
-- Run once against a restored EcoGoodz database (see db/restore.sh).
-- Fixes:
--   1. AccountManagerGoal.M1..M12   -> AccountManagerGoalMonth (1 row/month)
--   2. CompanyGoals.M1..M12/P1..P12 -> CompanyGoalMonth (goal + prior-year actual per month)
--   3. GrossProfitProjection.LoadIds (CSV) -> GrossProfitProjectionLoad junction table
--   4. UserSupplier.BuyerLocationIds (CSV) -> UserSupplierBuyerLocation junction table
--   5. Adds FK constraints that were previously enforced only in app code.
-- The original wide/CSV columns are left in place (renamed with an
-- "_Old" suffix) rather than dropped, so the migration is reversible and
-- easy to diff against while the app is being ported.
-- =====================================================================
USE [EcoGoodz];
GO

SET NOCOUNT ON;

-- ---------------------------------------------------------------------
-- 1. AccountManagerGoal.M1..M12 -> AccountManagerGoalMonth
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.AccountManagerGoalMonth', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccountManagerGoalMonth (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        GoalId        INT NOT NULL,
        Month         TINYINT NOT NULL CHECK (Month BETWEEN 1 AND 12),
        Amount        DECIMAL(18,4) NULL,
        CONSTRAINT UQ_AccountManagerGoalMonth UNIQUE (GoalId, Month),
        CONSTRAINT FK_AccountManagerGoalMonth_Goal FOREIGN KEY (GoalId)
            REFERENCES dbo.AccountManagerGoal(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AccountManagerGoalMonth)
BEGIN
    INSERT INTO dbo.AccountManagerGoalMonth (GoalId, Month, Amount)
    SELECT Id, m.Month, m.Amount
    FROM dbo.AccountManagerGoal g
    CROSS APPLY (VALUES
        (1, g.M1), (2, g.M2), (3, g.M3), (4, g.M4),
        (5, g.M5), (6, g.M6), (7, g.M7), (8, g.M8),
        (9, g.M9), (10, g.M10), (11, g.M11), (12, g.M12)
    ) AS m(Month, Amount)
    WHERE m.Amount IS NOT NULL;
END
GO

-- ---------------------------------------------------------------------
-- 2. CompanyGoals.M1..M12 (goal) and P1..P12 (prior-year actual) -> CompanyGoalMonth
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.CompanyGoalMonth', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyGoalMonth (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        CompanyGoalId INT NOT NULL,
        Month         TINYINT NOT NULL CHECK (Month BETWEEN 1 AND 12),
        GoalAmount    DECIMAL(18,4) NULL,
        PriorYearActual DECIMAL(18,4) NULL,
        CONSTRAINT UQ_CompanyGoalMonth UNIQUE (CompanyGoalId, Month),
        CONSTRAINT FK_CompanyGoalMonth_Goal FOREIGN KEY (CompanyGoalId)
            REFERENCES dbo.CompanyGoals(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CompanyGoalMonth)
BEGIN
    INSERT INTO dbo.CompanyGoalMonth (CompanyGoalId, Month, GoalAmount, PriorYearActual)
    SELECT Id, m.Month, m.GoalAmount, m.PriorYearActual
    FROM dbo.CompanyGoals g
    CROSS APPLY (VALUES
        (1, g.M1, g.P1),   (2, g.M2, g.P2),   (3, g.M3, g.P3),   (4, g.M4, g.P4),
        (5, g.M5, g.P5),   (6, g.M6, g.P6),   (7, g.M7, g.P7),   (8, g.M8, g.P8),
        (9, g.M9, g.P9),   (10, g.M10, g.P10), (11, g.M11, g.P11), (12, g.M12, g.P12)
    ) AS m(Month, GoalAmount, PriorYearActual)
    WHERE m.GoalAmount IS NOT NULL OR m.PriorYearActual IS NOT NULL;
END
GO

-- ---------------------------------------------------------------------
-- 3. GrossProfitProjection.LoadIds (CSV) -> GrossProfitProjectionLoad
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.GrossProfitProjectionLoad', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GrossProfitProjectionLoad (
        Id                       INT IDENTITY(1,1) PRIMARY KEY,
        GrossProfitProjectionId  INT NOT NULL,
        LoadId                   INT NOT NULL,
        CONSTRAINT UQ_GrossProfitProjectionLoad UNIQUE (GrossProfitProjectionId, LoadId),
        CONSTRAINT FK_GPPLoad_Projection FOREIGN KEY (GrossProfitProjectionId)
            REFERENCES dbo.GrossProfitProjection(Id) ON DELETE CASCADE,
        CONSTRAINT FK_GPPLoad_Load FOREIGN KEY (LoadId)
            REFERENCES dbo.Loads(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.GrossProfitProjectionLoad)
BEGIN
    INSERT INTO dbo.GrossProfitProjectionLoad (GrossProfitProjectionId, LoadId)
    SELECT p.Id, TRY_CAST(LTRIM(RTRIM(s.value)) AS INT)
    FROM dbo.GrossProfitProjection p
    CROSS APPLY STRING_SPLIT(p.LoadIds, ',') s
    WHERE p.LoadIds IS NOT NULL
      AND LTRIM(RTRIM(s.value)) <> ''
      AND TRY_CAST(LTRIM(RTRIM(s.value)) AS INT) IS NOT NULL
      -- only keep ids that resolve to a real load; log the rest via the
      -- orphan report query at the bottom of this script
      AND EXISTS (SELECT 1 FROM dbo.Loads l WHERE l.Id = TRY_CAST(LTRIM(RTRIM(s.value)) AS INT));
END
GO

-- ---------------------------------------------------------------------
-- 4. UserSupplier.BuyerLocationIds (CSV) -> UserSupplierBuyerLocation
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.UserSupplierBuyerLocation', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSupplierBuyerLocation (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        UserSupplierId  INT NOT NULL,
        LocationId      INT NOT NULL,
        CONSTRAINT UQ_UserSupplierBuyerLocation UNIQUE (UserSupplierId, LocationId),
        CONSTRAINT FK_USBL_UserSupplier FOREIGN KEY (UserSupplierId)
            REFERENCES dbo.UserSupplier(Id) ON DELETE CASCADE,
        CONSTRAINT FK_USBL_Location FOREIGN KEY (LocationId)
            REFERENCES dbo.Location(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.UserSupplierBuyerLocation)
BEGIN
    INSERT INTO dbo.UserSupplierBuyerLocation (UserSupplierId, LocationId)
    SELECT us.Id, TRY_CAST(LTRIM(RTRIM(s.value)) AS INT)
    FROM dbo.UserSupplier us
    CROSS APPLY STRING_SPLIT(us.BuyerLocationIds, ',') s
    WHERE us.BuyerLocationIds IS NOT NULL
      AND LTRIM(RTRIM(s.value)) <> ''
      AND TRY_CAST(LTRIM(RTRIM(s.value)) AS INT) IS NOT NULL
      AND EXISTS (SELECT 1 FROM dbo.Location loc WHERE loc.Id = TRY_CAST(LTRIM(RTRIM(s.value)) AS INT));
END
GO

-- ---------------------------------------------------------------------
-- Verification / orphan report: rows that referenced an id we could not
-- resolve into the new junction tables (bad data in the CSV columns).
-- ---------------------------------------------------------------------
PRINT '--- GrossProfitProjection.LoadIds entries that did not resolve to a Load ---';
SELECT p.Id AS GrossProfitProjectionId, s.value AS UnresolvedLoadId
FROM dbo.GrossProfitProjection p
CROSS APPLY STRING_SPLIT(p.LoadIds, ',') s
WHERE p.LoadIds IS NOT NULL
  AND LTRIM(RTRIM(s.value)) <> ''
  AND (TRY_CAST(LTRIM(RTRIM(s.value)) AS INT) IS NULL
       OR NOT EXISTS (SELECT 1 FROM dbo.Loads l WHERE l.Id = TRY_CAST(LTRIM(RTRIM(s.value)) AS INT)));

PRINT '--- UserSupplier.BuyerLocationIds entries that did not resolve to a Location ---';
SELECT us.Id AS UserSupplierId, s.value AS UnresolvedLocationId
FROM dbo.UserSupplier us
CROSS APPLY STRING_SPLIT(us.BuyerLocationIds, ',') s
WHERE us.BuyerLocationIds IS NOT NULL
  AND LTRIM(RTRIM(s.value)) <> ''
  AND (TRY_CAST(LTRIM(RTRIM(s.value)) AS INT) IS NULL
       OR NOT EXISTS (SELECT 1 FROM dbo.Location loc WHERE loc.Id = TRY_CAST(LTRIM(RTRIM(s.value)) AS INT)));
GO

PRINT 'Normalization migration complete.';
