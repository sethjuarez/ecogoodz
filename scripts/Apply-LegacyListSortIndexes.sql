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

IF EXISTS (
    SELECT 1
    FROM sys.indexes AS i
    JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID('dbo.Location')
      AND i.name = 'IX_Location_LocationSort_Id'
      AND c.name = 'CitySort'
      AND ic.key_ordinal > 0
)
BEGIN
    DROP INDEX IX_Location_LocationSort_Id ON dbo.Location;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Location') AND name = 'IX_Location_LocationSort_Id')
BEGIN
    CREATE INDEX IX_Location_LocationSort_Id ON dbo.Location (LocationSort, Id)
        INCLUDE (CitySort, ClientId, IsBuyer, State, Country, IsActive);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Location') AND name = 'IX_Location_CitySort_Id')
BEGIN
    CREATE INDEX IX_Location_CitySort_Id ON dbo.Location (CitySort, Id)
        INCLUDE (LocationSort, ClientId, IsBuyer, State, Country, IsActive);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Loads') AND name = 'IX_Loads_Supplier_Active_ShipmentDate_Id')
BEGIN
    CREATE INDEX IX_Loads_Supplier_Active_ShipmentDate_Id ON dbo.Loads (Supplier, IsActive, ShipmentDate DESC, Id DESC)
        INCLUDE (Buyer, BuyerLocation, SupplierLocation, LoadStatus);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Loads') AND name = 'IX_Loads_Supplier_ShipmentDate_Id')
BEGIN
    CREATE INDEX IX_Loads_Supplier_ShipmentDate_Id ON dbo.Loads (Supplier, ShipmentDate DESC, Id DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Loads') AND name = 'IX_Loads_Buyer_ShipmentDate_Id')
BEGIN
    CREATE INDEX IX_Loads_Buyer_ShipmentDate_Id ON dbo.Loads (Buyer, ShipmentDate DESC, Id DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Loads') AND name = 'IX_Loads_BuyerLocation_ShipmentDate_Id')
BEGIN
    CREATE INDEX IX_Loads_BuyerLocation_ShipmentDate_Id ON dbo.Loads (BuyerLocation, ShipmentDate DESC, Id DESC)
        INCLUDE (Buyer, Supplier, SupplierLocation, LoadStatus, IsActive);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Loads') AND name = 'IX_Loads_SupplierLocation_ShipmentDate_Id')
BEGIN
    CREATE INDEX IX_Loads_SupplierLocation_ShipmentDate_Id ON dbo.Loads (SupplierLocation, ShipmentDate DESC, Id DESC)
        INCLUDE (Buyer, Supplier, BuyerLocation, LoadStatus, IsActive);
END;
