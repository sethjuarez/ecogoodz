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

IF OBJECT_ID(N'dbo.Product', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Product', N'Name') IS NOT NULL
        AND COL_LENGTH(N'dbo.Product', N'NameSort') IS NULL
    BEGIN
        ALTER TABLE dbo.Product ADD NameSort AS CONVERT(nvarchar(450), [Name]) PERSISTED;
    END;

    IF COL_LENGTH(N'dbo.Product', N'NameSort') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Product')
              AND name = N'IX_Product_NameSort_Id')
    BEGIN
        CREATE INDEX IX_Product_NameSort_Id
            ON dbo.Product (NameSort, Id)
            INCLUDE (IdParent, IsActive);
    END;

    IF COL_LENGTH(N'dbo.Product', N'IdParent') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Product')
              AND name = N'IX_Product_IdParent')
    BEGIN
        CREATE INDEX IX_Product_IdParent
            ON dbo.Product (IdParent)
            INCLUDE (NameSort, IsActive);
    END;
END;

IF OBJECT_ID(N'dbo.Location', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Location', N'Location') IS NOT NULL
        AND COL_LENGTH(N'dbo.Location', N'LocationSort') IS NULL
    BEGIN
        ALTER TABLE dbo.Location ADD LocationSort AS CONVERT(nvarchar(450), [Location]) PERSISTED;
    END;

    IF COL_LENGTH(N'dbo.Location', N'City') IS NOT NULL
        AND COL_LENGTH(N'dbo.Location', N'CitySort') IS NULL
    BEGIN
        ALTER TABLE dbo.Location ADD CitySort AS CONVERT(nvarchar(450), [City]) PERSISTED;
    END;

    IF COL_LENGTH(N'dbo.Location', N'LocationSort') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Location')
              AND name = N'IX_Location_LocationSort_Id')
    BEGIN
        CREATE INDEX IX_Location_LocationSort_Id
            ON dbo.Location (LocationSort, Id)
            INCLUDE (ClientId, IsBuyer, CitySort, [State], Country, IsActive);
    END;

    IF COL_LENGTH(N'dbo.Location', N'ClientId') IS NOT NULL
        AND COL_LENGTH(N'dbo.Location', N'IsBuyer') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Location')
              AND name = N'IX_Location_Client')
    BEGIN
        CREATE INDEX IX_Location_Client
            ON dbo.Location (IsBuyer, ClientId)
            INCLUDE (LocationSort, CitySort, IsActive);
    END;
END;

IF OBJECT_ID(N'dbo.PackageType', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.PackageType', N'Type') IS NOT NULL
        AND COL_LENGTH(N'dbo.PackageType', N'TypeSort') IS NULL
    BEGIN
        ALTER TABLE dbo.PackageType ADD TypeSort AS CONVERT(nvarchar(450), [Type]) PERSISTED;
    END;

    IF COL_LENGTH(N'dbo.PackageType', N'TypeSort') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.PackageType')
              AND name = N'IX_PackageType_TypeSort_Id')
    BEGIN
        CREATE INDEX IX_PackageType_TypeSort_Id
            ON dbo.PackageType (TypeSort, Id)
            INCLUDE (IsActive);
    END;
END;

IF OBJECT_ID(N'dbo.BuyerProduct', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.BuyerProduct')
          AND name = N'IX_BuyerProduct_Buyer')
    BEGIN
        CREATE INDEX IX_BuyerProduct_Buyer
            ON dbo.BuyerProduct (Buyer, Id)
            INCLUDE (Location, Product, IsActive);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.BuyerProduct')
          AND name = N'IX_BuyerProduct_Location_Product')
    BEGIN
        CREATE INDEX IX_BuyerProduct_Location_Product
            ON dbo.BuyerProduct (Location, Product)
            INCLUDE (Buyer, IsActive);
    END;
END;

IF OBJECT_ID(N'dbo.SupplierProduct', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.SupplierProduct')
          AND name = N'IX_SupplierProduct_Supplier')
    BEGIN
        CREATE INDEX IX_SupplierProduct_Supplier
            ON dbo.SupplierProduct (Supplier, Id)
            INCLUDE (Location, Product, Packaging, IsActive);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.SupplierProduct')
          AND name = N'IX_SupplierProduct_Location_Product')
    BEGIN
        CREATE INDEX IX_SupplierProduct_Location_Product
            ON dbo.SupplierProduct (Location, Product)
            INCLUDE (Supplier, Packaging, IsActive);
    END;
END;

IF OBJECT_ID(N'dbo.BuyerSupplier', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.BuyerSupplier')
          AND name = N'IX_BuyerSupplier_Buyer')
    BEGIN
        CREATE INDEX IX_BuyerSupplier_Buyer
            ON dbo.BuyerSupplier (Buyer, Id)
            INCLUDE (Supplier, BuyerLocation, SupplierLocation, Status, IsActive);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.BuyerSupplier')
          AND name = N'IX_BuyerSupplier_Supplier')
    BEGIN
        CREATE INDEX IX_BuyerSupplier_Supplier
            ON dbo.BuyerSupplier (Supplier, Id)
            INCLUDE (Buyer, BuyerLocation, SupplierLocation, Status, IsActive);
    END;
END;
""";

        return context.Database.ExecuteSqlRawAsync(sql);
    }
}
