@echo off
rem Compile check outside Unity. Usage: Tools\CompileCheck\compile-check.cmd
rem If Unity is installed elsewhere: set UNITY_MANAGED=...\Editor\Data\Managed
setlocal
set EXTRA=
if not "%UNITY_MANAGED%"=="" set EXTRA=-p:UnityManaged="%UNITY_MANAGED%"
set FAIL=0
for %%P in (RuntimePlayer RuntimeEditor EditorScripts) do (
  echo === %%P
  dotnet build "%~dp0%%P.csproj" -nologo -v q %EXTRA% || set FAIL=1
)
if "%FAIL%"=="1" (echo COMPILE_CHECK_FAIL & exit /b 1)
echo COMPILE_CHECK_PASS
