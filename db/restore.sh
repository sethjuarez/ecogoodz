#!/bin/bash
# Restores the EcoGoodz database into the sqlserver container from the .bak
# file mounted at /var/opt/mssql/backup/EcoGoodz.bak.
set -euo pipefail

SQLCMD="/opt/mssql-tools18/bin/sqlcmd -S ecogoodz-sqlserver -U sa -P ${MSSQL_SA_PASSWORD} -C"
BACKUP_PATH="/var/opt/mssql/backup/EcoGoodz.bak"
DATA_DIR="/var/opt/mssql/data"

echo "Waiting for SQL Server to accept connections..."
until $SQLCMD -Q "SELECT 1" >/dev/null 2>&1; do
  sleep 2
done

echo "Checking whether EcoGoodz database already exists..."
EXISTS=$($SQLCMD -h -1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'EcoGoodz'" | tr -d '[:space:]')
if [ "$EXISTS" = "1" ]; then
  echo "EcoGoodz database already exists, skipping restore."
  exit 0
fi

echo "Inspecting backup file list..."
$SQLCMD -Q "RESTORE FILELISTONLY FROM DISK = N'${BACKUP_PATH}'" -s"," -W > /tmp/filelist.txt
cat /tmp/filelist.txt

# Extract logical names (first column) for the data and log files.
LOGICAL_DATA=$(awk -F',' 'NR>2 && $2 !~ /_log/ && $2 !~ /Log/ {print $1; exit}' /tmp/filelist.txt | xargs)
LOGICAL_LOG=$(awk -F',' 'NR>2 && ($2 ~ /_log/ || $2 ~ /Log/) {print $1; exit}' /tmp/filelist.txt | xargs)

if [ -z "$LOGICAL_DATA" ] || [ -z "$LOGICAL_LOG" ]; then
  echo "Could not auto-detect logical file names; falling back to defaults."
  LOGICAL_DATA=${LOGICAL_DATA:-EcoGoodz}
  LOGICAL_LOG=${LOGICAL_LOG:-EcoGoodz_log}
fi

echo "Logical data file: $LOGICAL_DATA"
echo "Logical log file:  $LOGICAL_LOG"

echo "Restoring EcoGoodz database..."
$SQLCMD -Q "
RESTORE DATABASE [EcoGoodz]
FROM DISK = N'${BACKUP_PATH}'
WITH MOVE N'${LOGICAL_DATA}' TO N'${DATA_DIR}/EcoGoodz.mdf',
     MOVE N'${LOGICAL_LOG}' TO N'${DATA_DIR}/EcoGoodz_log.ldf',
     REPLACE, STATS = 10;
"

echo "Creating application login/user (ecogoodz2024) if missing..."
$SQLCMD -Q "
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'ecogoodz2024')
BEGIN
    CREATE LOGIN [ecogoodz2024] WITH PASSWORD = N'EcoGoodz!App2026', CHECK_POLICY = OFF;
END
"
$SQLCMD -Q "
USE [EcoGoodz];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'ecogoodz2024')
BEGIN
    CREATE USER [ecogoodz2024] FOR LOGIN [ecogoodz2024];
    ALTER ROLE db_owner ADD MEMBER [ecogoodz2024];
END
"

echo "Restore complete."
