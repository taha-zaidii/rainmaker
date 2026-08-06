#!/usr/bin/env zsh

echo "🧹 Cleaning up suspended processes & freeing ports (4200, 5019)..."

# 1. Kill any process on port 4200 (Angular) and 5019 (.NET)
lsof -ti:4200 | xargs kill -9 2>/dev/null || true
lsof -ti:5019 | xargs kill -9 2>/dev/null || true

# 2. Terminate background dotnet / ng serve instances
pkill -9 -f "dotnet run" 2>/dev/null || true
pkill -9 -f "dotnet watch" 2>/dev/null || true
pkill -9 -f "ng serve" 2>/dev/null || true

# 3. Ensure Docker DB is running and updated
docker start rainmaker-mssql 2>/dev/null || true
docker exec -i rainmaker-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Multinet@123!" -C -d InternDB -i /dev/stdin < db/seed/005_demo_ruc_fixes.sql 2>/dev/null || true

echo "✅ Ports freed & SQL Server running with latest stored procedures!"

