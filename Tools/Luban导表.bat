@echo off
title Excel Importer
pushd "%~dp0\..\"
dotnet tool install -g dotnet-script 2>nul
dotnet-script "Assets\Scripts\Editor\ExcelImporter\ExcelImporter.cs"
popd
pause