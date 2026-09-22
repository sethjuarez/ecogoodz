/*
Run this manually against the EcoGoodz SQL Server database before deploying the
app to a restored legacy database. The app does not execute this script at
startup; it is safe/idempotent DBA maintenance for the normalized legacy schema.

The computed columns intentionally convert legacy text values to nvarchar(450):
that makes them indexable on SQL Server and preserves the existing database
collation, but values longer than 450 characters sort by that prefix.
*/

SET NOCOUNT ON;

IF COL_LENGTH('dbo.Buyer', 'NameSort') IS NULL
BEGIN
    ALTER TABLE dbo.Buyer ADD NameSort AS CONVERT(nvarchar(450), [Name]) PERSISTED;
END;

IF COL_LENGTH('dbo.Supplier', 'NameSort') IS NULL
BEGIN
    ALTER TABLE dbo.Supplier ADD NameSort AS CONVERT(nvarchar(450), [Name]) PERSISTED;
END;

IF COL_LENGTH('dbo.Product', 'NameSort') IS NULL
BEGIN
    ALTER TABLE dbo.Product ADD NameSort AS CONVERT(nvarchar(450), [Name]) PERSISTED;
END;

IF COL_LENGTH('dbo.PackageType', 'TypeSort') IS NULL
BEGIN
    ALTER TABLE dbo.PackageType ADD TypeSort AS CONVERT(nvarchar(450), [Type]) PERSISTED;
END;

IF COL_LENGTH('dbo.Location', 'LocationSort') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD LocationSort AS CONVERT(nvarchar(450), [Location]) PERSISTED;
END;

IF COL_LENGTH('dbo.Location', 'CitySort') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD CitySort AS CONVERT(nvarchar(450), [City]) PERSISTED;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Buyer') AND name = 'IX_Buyer_NameSort_Id')
BEGIN
    CREATE INDEX IX_Buyer_NameSort_Id ON dbo.Buyer (NameSort, Id) INCLUDE (AccountManager, IsActive);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Supplier') AND name = 'IX_Supplier_NameSort_Id')
BEGIN
    CREATE INDEX IX_Supplier_NameSort_Id ON dbo.Supplier (NameSort, Id) INCLUDE (AccountManager, IsActive);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'IX_Product_NameSort_Id')
BEGIN
    CREATE INDEX IX_Product_NameSort_Id ON dbo.Product (NameSort, Id) INCLUDE (IdParent, IsActive);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PackageType') AND name = 'IX_PackageType_TypeSort_Id')
BEGIN
    CREATE INDEX IX_PackageType_TypeSort_Id ON dbo.PackageType (TypeSort, Id) INCLUDE (IsActive);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Location') AND name = 'IX_Location_LocationSort_Id')
BEGIN
    CREATE INDEX IX_Location_LocationSort_Id ON dbo.Location (LocationSort, CitySort, Id)
        INCLUDE (ClientId, IsBuyer, State, Country, IsActive);
END;
