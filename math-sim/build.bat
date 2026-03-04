@echo off
setlocal
for /f "usebackq tokens=*" %%i in (`"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath`) do set VSInstallDir=%%i
if "%VSInstallDir%"=="" (
    echo MSVC not found.
    exit /b 1
)
call "%VSInstallDir%\VC\Auxiliary\Build\vcvars64.bat"
cl.exe /EHsc /Fe:Simulation.exe Simulation.cpp
endlocal
