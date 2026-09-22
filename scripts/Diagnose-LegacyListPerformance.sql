/*
Run this against the production EcoGoodz legacy database in SSMS/Azure Data Studio.
It is read-only. Turn on "Include Actual Execution Plan" before running the
target-query section if you can.
*/

SET NOCOUNT ON;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

PRINT '1) Table sizes for list pages';
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    SUM(p.rows) AS [Rows]
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE t.name IN (
    'Buyer',
    'Supplier',
    'BuyerSupplier',
    'BuyerProduct',
    'SupplierProduct',
    'Location',
    'Product',
    'PackageType',
    'Loads',
    'User'
)
GROUP BY s.name, t.name
ORDER BY [Rows] DESC;

PRINT '2) Existing indexes on slow-list tables';
SELECT
    OBJECT_SCHEMA_NAME(i.object_id) AS SchemaName,
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_primary_key AS IsPrimaryKey,
    i.is_unique AS IsUnique,
    STUFF((
        SELECT ', ' + c2.name
        FROM sys.index_columns AS ic2
        JOIN sys.columns AS c2 ON c2.object_id = ic2.object_id AND c2.column_id = ic2.column_id
        WHERE ic2.object_id = i.object_id
          AND ic2.index_id = i.index_id
          AND ic2.key_ordinal > 0
        ORDER BY ic2.key_ordinal
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS KeyColumns
FROM sys.indexes AS i
WHERE OBJECT_NAME(i.object_id) IN (
    'Buyer',
    'Supplier',
    'BuyerSupplier',
    'BuyerProduct',
    'SupplierProduct',
    'Location'
)
ORDER BY TableName, IndexName;

PRINT '3) Missing-index recommendations since last SQL Server restart';
SELECT TOP (50)
    DB_NAME(mid.database_id) AS DatabaseName,
    OBJECT_SCHEMA_NAME(mid.object_id, mid.database_id) AS SchemaName,
    OBJECT_NAME(mid.object_id, mid.database_id) AS TableName,
    migs.user_seeks,
    migs.user_scans,
    migs.avg_total_user_cost,
    migs.avg_user_impact,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns
FROM sys.dm_db_missing_index_group_stats AS migs
JOIN sys.dm_db_missing_index_groups AS mig ON mig.index_group_handle = migs.group_handle
JOIN sys.dm_db_missing_index_details AS mid ON mid.index_handle = mig.index_handle
WHERE mid.database_id = DB_ID()
  AND OBJECT_NAME(mid.object_id, mid.database_id) IN (
      'Buyer',
      'Supplier',
      'BuyerSupplier',
      'BuyerProduct',
      'SupplierProduct',
      'Location'
  )
ORDER BY migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans) DESC;

PRINT '4) Recent expensive cached statements touching slow-list tables';
SELECT TOP (25)
    qs.execution_count,
    qs.total_elapsed_time / 1000 AS TotalElapsedMs,
    qs.max_elapsed_time / 1000 AS MaxElapsedMs,
    qs.total_worker_time / 1000 AS TotalCpuMs,
    qs.total_logical_reads AS TotalLogicalReads,
    SUBSTRING(
        st.text,
        (qs.statement_start_offset / 2) + 1,
        ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) ELSE qs.statement_end_offset END - qs.statement_start_offset) / 2) + 1
    ) AS StatementText
FROM sys.dm_exec_query_stats AS qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS st
WHERE st.text LIKE '%[[]Buyer]%'
   OR st.text LIKE '%[[]Supplier]%'
   OR st.text LIKE '%[[]BuyerSupplier]%'
   OR st.text LIKE '%[[]BuyerProduct]%'
   OR st.text LIKE '%[[]SupplierProduct]%'
   OR st.text LIKE '%[[]Location]%'
ORDER BY qs.max_elapsed_time DESC;

PRINT '5) Target queries: run with Actual Execution Plan enabled';

PRINT '5a) Buyer default list';
SELECT COUNT(*) FROM dbo.Buyer;
SELECT TOP (20)
    b.Id,
    b.Name,
    u.FirstName,
    u.LastName,
    b.IsActive
FROM dbo.Buyer AS b
LEFT JOIN dbo.[User] AS u ON b.AccountManager = u.Id
ORDER BY b.Name;

PRINT '5b) Supplier default list';
SELECT COUNT(*) FROM dbo.Supplier;
SELECT TOP (20)
    s.Id,
    s.Name,
    u.FirstName,
    u.LastName,
    s.IsActive
FROM dbo.Supplier AS s
LEFT JOIN dbo.[User] AS u ON s.AccountManager = u.Id
ORDER BY s.Name;

PRINT '5c) Buyer/Supplier matches default list';
SELECT COUNT(*) FROM dbo.BuyerSupplier;
SELECT TOP (20)
    bs.Id,
    b.Name AS BuyerName,
    s.Name AS SupplierName,
    bl.Location AS BuyerLocation,
    sl.Location AS SupplierLocation,
    bs.IsActive
FROM dbo.BuyerSupplier AS bs
LEFT JOIN dbo.Buyer AS b ON bs.Buyer = b.Id
LEFT JOIN dbo.Supplier AS s ON bs.Supplier = s.Id
LEFT JOIN dbo.Location AS bl ON bs.BuyerLocation = bl.Id
LEFT JOIN dbo.Location AS sl ON bs.SupplierLocation = sl.Id
ORDER BY b.Name;

PRINT '5d) Buyer products default list';
SELECT COUNT(*) FROM dbo.BuyerProduct;
SELECT TOP (20)
    bp.Id,
    b.Name AS BuyerName,
    l.Location AS LocationName,
    p.Name AS ProductName,
    bp.IsActive
FROM dbo.BuyerProduct AS bp
LEFT JOIN dbo.Buyer AS b ON bp.Buyer = b.Id
LEFT JOIN dbo.Location AS l ON bp.Location = l.Id
LEFT JOIN dbo.Product AS p ON bp.Product = p.Id
ORDER BY b.Name;

PRINT '5e) Supplier products default list';
SELECT COUNT(*) FROM dbo.SupplierProduct;
SELECT TOP (20)
    sp.Id,
    s.Name AS SupplierName,
    l.Location AS LocationName,
    p.Name AS ProductName,
    pt.Type AS PackagingName,
    sp.IsActive
FROM dbo.SupplierProduct AS sp
LEFT JOIN dbo.Supplier AS s ON sp.Supplier = s.Id
LEFT JOIN dbo.Location AS l ON sp.Location = l.Id
LEFT JOIN dbo.Product AS p ON sp.Product = p.Id
LEFT JOIN dbo.PackageType AS pt ON sp.Packaging = pt.Id
ORDER BY s.Name;

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
