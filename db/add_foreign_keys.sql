-- =====================================================================
-- EcoGoodz FK-constraint backfill
-- The legacy app enforced referential integrity entirely in code; the
-- database itself has zero FOREIGN KEY constraints. This script adds
-- them back so the database enforces its own integrity going forward.
--
-- Strategy: a column is treated as a FK candidate when its name exactly
-- matches another table's name (the app's naming convention, e.g.
-- Loads.Buyer -> Buyer.Id) or matches "<Table>Id" (e.g. LocationId ->
-- Location.Id). For each candidate we verify there are zero orphaned
-- values before adding the constraint, and skip (with a NOTICE) any
-- candidate that still has orphans instead of failing the whole script.
-- =====================================================================
USE [EcoGoodz];
GO
SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#Candidates') IS NOT NULL DROP TABLE #Candidates;

SELECT
    c.TABLE_NAME  AS FromTable,
    c.COLUMN_NAME AS FromColumn,
    t.TABLE_NAME  AS ToTable
INTO #Candidates
FROM INFORMATION_SCHEMA.COLUMNS c
JOIN INFORMATION_SCHEMA.TABLES t
    ON t.TABLE_TYPE = 'BASE TABLE'
   AND (
        t.TABLE_NAME = c.COLUMN_NAME                                   -- e.g. Loads.Buyer -> Buyer
        OR t.TABLE_NAME = LEFT(c.COLUMN_NAME, LEN(c.COLUMN_NAME) - 2)  -- e.g. LocationId -> Location
       )
WHERE c.TABLE_NAME <> t.TABLE_NAME
  AND c.DATA_TYPE IN ('int','bigint','smallint')
  AND c.TABLE_NAME NOT LIKE '__MigrationHistory'
  -- skip the junction tables we just created; their FKs already exist
  AND c.TABLE_NAME NOT IN ('AccountManagerGoalMonth','CompanyGoalMonth','GrossProfitProjectionLoad','UserSupplierBuyerLocation')
  AND EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS pk
        WHERE pk.TABLE_NAME = t.TABLE_NAME AND pk.COLUMN_NAME = 'Id'
      )
  AND NOT EXISTS (
        -- skip if a same-named FK already exists
        SELECT 1 FROM sys.foreign_keys fk
        WHERE fk.name = 'FK_' + c.TABLE_NAME + '_' + c.COLUMN_NAME + '_' + t.TABLE_NAME
      );

DECLARE @FromTable NVARCHAR(128), @FromColumn NVARCHAR(128), @ToTable NVARCHAR(128);
DECLARE @sql NVARCHAR(MAX), @orphanCount INT, @fkName NVARCHAR(300);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT FromTable, FromColumn, ToTable FROM #Candidates ORDER BY FromTable, FromColumn;

OPEN cur;
FETCH NEXT FROM cur INTO @FromTable, @FromColumn, @ToTable;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @fkName = 'FK_' + @FromTable + '_' + @FromColumn + '_' + @ToTable;

    -- Count orphans (non-null FK values with no matching parent row)
    SET @sql = N'SELECT @cnt = COUNT(*) FROM ' + QUOTENAME(@FromTable) + ' f
                 WHERE f.' + QUOTENAME(@FromColumn) + ' IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM ' + QUOTENAME(@ToTable) + ' p WHERE p.Id = f.' + QUOTENAME(@FromColumn) + ')';
    EXEC sp_executesql @sql, N'@cnt INT OUTPUT', @cnt = @orphanCount OUTPUT;

    IF @orphanCount = 0
    BEGIN
        BEGIN TRY
            SET @sql = N'ALTER TABLE ' + QUOTENAME(@FromTable) +
                       N' ADD CONSTRAINT ' + QUOTENAME(@fkName) +
                       N' FOREIGN KEY (' + QUOTENAME(@FromColumn) + N') REFERENCES ' + QUOTENAME(@ToTable) + N'(Id)';
            EXEC sp_executesql @sql;
            PRINT 'Added FK: ' + @fkName;
        END TRY
        BEGIN CATCH
            PRINT 'SKIPPED (error): ' + @fkName + ' - ' + ERROR_MESSAGE();
        END CATCH
    END
    ELSE
    BEGIN
        PRINT 'SKIPPED (orphans=' + CAST(@orphanCount AS NVARCHAR(20)) + '): ' + @fkName;
    END

    FETCH NEXT FROM cur INTO @FromTable, @FromColumn, @ToTable;
END

CLOSE cur;
DEALLOCATE cur;
GO

PRINT 'FK backfill complete.';
SELECT COUNT(*) AS TotalForeignKeys FROM sys.foreign_keys;
GO
