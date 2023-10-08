@echo off

@REM SET GAME_PATH="e:\Games\steamapps\common\Oxenfree II Lost Signals\Oxenfree2_Data\StreamingAssets\aa\StandaloneWindows64"
SET SEVENZIP_PATH="c:\Program Files\7-Zip\7z.exe"
SET PATCHED_RA_PATH="resources.assets"

@REM if not exist %GAME_PATH% (
@REM   echo game folder NOT FOUND
@REM   exit /B 1
@REM )
if not exist %PATCHED_RA_PATH% (
  echo file resources.assets NOT FOUND
  exit /B 1
)
if not exist %SEVENZIP_PATH% (
  echo 7z.exe NOT FOUND
  exit /B 1
)

set tag=
set prod=
set /p "tag=Version tag: "
set /p "prod=For production type "prod", for release candidate type "rc": "

if not defined tag (
  @REM nightly version tag
  set tag=nightly-%DATE:~0,2%%DATE:~3,2%%DATE:~8,2%
)

echo[
echo Version tag: %tag%
if "%prod%"=="prod" (echo Production build: yes) else (echo Production build: no)

@REM exit /B 0

@echo off

@REM fetching sheets
curl -L "https://docs.google.com/spreadsheets/d/e/2PACX-1vT5oJvYqc0o3aLCVK9D0fYCFqHfhjfatt2G8BbHpdQ7vybLJ2hUUuG4A9ayCrsg9X8bQ4lqXtmPNLHo/pub?gid=961188793&single=true&output=tsv" > loc.tsv
curl -L "https://docs.google.com/spreadsheets/d/e/2PACX-1vT5oJvYqc0o3aLCVK9D0fYCFqHfhjfatt2G8BbHpdQ7vybLJ2hUUuG4A9ayCrsg9X8bQ4lqXtmPNLHo/pub?gid=959054798&single=true&output=tsv" > dialogue.tsv


@REM making a patch
if "%prod%"=="prod" (
  bin\Debug\net6.0\oxen32pack.exe -l es-419 --prod loc_packages_assets_.bundle "loc.tsv"
  if %ERRORLEVEL% neq 0 ( echo Program exited with error && timeout /t 10 && exit /B 1)
  bin\Debug\net6.0\oxen32pack.exe -l es-419 --prod dialogue_packages_assets_all.bundle dialogue.tsv
  if %ERRORLEVEL% neq 0 ( echo Program exited with error && timeout /t 10 && exit /B 1)
)
if "%prod%"=="rc" (
  bin\Debug\net6.0\oxen32pack.exe -l es-419 -t %tag% --prod loc_packages_assets_.bundle "loc.tsv"
  if %ERRORLEVEL% neq 0 ( echo Program exited with error && timeout /t 10 && exit /B 1)
  bin\Debug\net6.0\oxen32pack.exe -l es-419 -t %tag% --prod dialogue_packages_assets_all.bundle dialogue.tsv
  if %ERRORLEVEL% neq 0 ( echo Program exited with error && timeout /t 10 && exit /B 1)
)
if NOT "%prod%"=="prod" if NOT "%prod%"=="rc" (
  bin\Debug\net6.0\oxen32pack.exe -l es-419 -t %tag% loc_packages_assets_.bundle "loc.tsv"
  if %ERRORLEVEL% neq 0 ( echo Program exited with error && timeout /t 10 && exit /B 1)
  bin\Debug\net6.0\oxen32pack.exe -l es-419 -t %tag% dialogue_packages_assets_all.bundle dialogue.tsv
  if %ERRORLEVEL% neq 0 ( echo Program exited with error && timeout /t 10 && exit /B 1)
)

@echo off
@REM folders structure
rd /s /q "Oxenfree2_Data" 2> nul
md "Oxenfree2_Data/StreamingAssets/aa/StandaloneWindows64"
copy /Y %PATCHED_RA_PATH% Oxenfree2_Data
copy /Y loc_packages_assets_.bundle.mod "Oxenfree2_Data/StreamingAssets/aa/StandaloneWindows64/loc_packages_assets_.bundle"
copy /Y dialogue_packages_assets_all.bundle.mod "Oxenfree2_Data/StreamingAssets/aa/StandaloneWindows64/dialogue_packages_assets_all.bundle"

@REM archive
%SEVENZIP_PATH% a "Oxenfree II Lost Signals_ru-"%tag%.7z Oxenfree2_Data/*

@REM removing files
rd /s /q "Oxenfree2_Data" 2> nul
if exist loc_packages_assets_.bundle.mod del /F loc_packages_assets_.bundle.mod 
if exist dialogue_packages_assets_all.bundle.mod del /F dialogue_packages_assets_all.bundle.mod
@echo on