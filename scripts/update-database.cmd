@echo off
cd /d "%~dp0\.."
echo Migrating VisitorManagment on SQL Server Express...
dotnet tool restore
dotnet ef database update --project src\VisitorManagement.Web
if errorlevel 1 (
  echo Failed. Check that SQLEXPRESS is running.
  exit /b 1
)
echo Done. Database VisitorManagment is up to date.
