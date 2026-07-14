@echo off
chcp 65001 >nul
title Bağlantı Portalı - WebRTC

where dotnet >nul 2>&1
if errorlevel 1 (
  echo .NET SDK bulunamadı. Önce .NET 8 SDK yükleyin.
  pause
  exit /b 1
)

echo Bağımlılıklar hazırlanıyor...
dotnet restore Web\WebRTCSignalingServer.csproj
if errorlevel 1 (
  echo Bağımlılıklar yüklenemedi.
  pause
  exit /b 1
)

echo Uygulama http://localhost:8080 adresinde başlatılıyor...
start "" http://localhost:8080
dotnet run --project Web\WebRTCSignalingServer.csproj
