@echo off
title Excel Importer
chcp 65001>nul
pushd "%~dp0\..\"
dotnet tool install -g dotnet-script 2>nul
dotnet-script "Assets\Scripts\Editor\ExcelImporter\ExcelImporter.cs"
popd
echo 按任意键退出
pause>nul