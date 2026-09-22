using EcoGoodz.Data;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Infrastructure;

public static class LegacyDatabaseIndexes
{
    public static Task EnsureAsync(EcoGoodzDbContext context)
    {
        const string sql = """
IF OBJECT_ID(N'dbo.Buyer', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Buyer', N'Name') IS NOT NULL
        AND COL_LENGTH(N'dbo.Buyer', N'NameSort') IS NULL
    BEGIN
        ALTER TABLE dbo.Buyer ADD NameSort AS CONVERT(nvarchar(450), [Name]) PERSISTED;
    END;

    IF COL_LENGTH(N'dbo.Buyer', N'NameSort') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Buyer')
              AND name = N'IX_Buyer_NameSort_Id')
    BEGIN
        CREATE INDEX IX_Buyer_NameSort_Id
            ON dbo.Buyer (NameSort, Id)
            INCLUDE (AccountManager, IsActive);
    END;

    IF COL_LENGTH(N'dbo.Buyer', N'AccountManager') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Buyer')
              AND name = N'IX_Buyer_AccountManager')
    BEGIN
        CREATE INDEX IX_Buyer_AccountManager
            ON dbo.Buyer (AccountManager)
            INCLUDE (NameSort, IsActive);
    END;
END;

IF OBJECT_ID(N'dbo.Supplier', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Supplier', N'Name') IS NOT NULL
        AND COL_LENGTH(N'dbo.Supplier', N'NameSort') IS NULL
    BEGIN
        ALTER TABLE dbo.Supplier ADD NameSort AS CONVERT(nvarchar(450), [Name]) PERSISTED;
    END;

    IF COL_LENGTH(N'dbo.Supplier', N'NameSort') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Supplier')
              AND name = N'IX_Supplier_NameSort_Id')
    BEGIN
        CREATE INDEX IX_Supplier_NameSort_Id
            ON dbo.Supplier (NameSort, Id)
            INCLUDE (AccountManager, IsActive);
    END;

    IF COL_LENGTH(N'dbo.Supplier', N'AccountManager') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Supplier')
              AND name = N'IX_Supplier_AccountManager')
    BEGIN
        CREATE INDEX IX_Supplier_AccountManager
            ON dbo.Supplier (AccountManager)
            INCLUDE (NameSort, IsActive);
    END;
END;
""";

        return context.Database.ExecuteSqlRawAsync(sql);
    }
}
