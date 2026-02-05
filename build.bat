@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ========================================
echo  RDP Manager Build Script
echo ========================================
echo.

echo [1/4] Building...
dotnet msbuild "RDPManager.csproj" /p:Configuration=Release /p:Platform=x86 /t:Rebuild /v:minimal
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)
echo Build OK!
echo.

echo [2/4] Creating publish folder...
if not exist "publish" mkdir "publish"

echo [3/4] Merging DLLs...
if not exist "packages\ILMerge.3.0.41\tools\net452\ILMerge.exe" (
    echo ILMerge not found, downloading...
    if not exist "nuget.exe" (
        powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile 'nuget.exe'"
    )
    "nuget.exe" install ILMerge -Version 3.0.41 -OutputDirectory packages
)
"packages\ILMerge.3.0.41\tools\net452\ILMerge.exe" /target:winexe "/out:publish\RDPManager.exe" "bin\x86\Release\RDPManager.exe" "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll" /targetplatform:v4 /ndebug
if errorlevel 1 (
    echo Merge failed!
    pause
    exit /b 1
)

echo [4/4] Copying files...
copy /Y "MSTSCLib.dll" "publish\" >nul
copy /Y "AxMSTSCLib.dll" "publish\" >nul
copy /Y "app.ico" "publish\" >nul

echo.
echo ========================================
echo  Build Complete!
echo ========================================
echo.
echo Output:
dir /b "publish"
echo.
pause
