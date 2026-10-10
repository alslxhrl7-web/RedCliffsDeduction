@echo off
rem Copies the Claude Code slash commands and settings for Red Cliffs Deduction into .claude (run once from the project folder).
if not exist "%~dp0..\..\.claude\commands" mkdir "%~dp0..\..\.claude\commands"
copy /Y "%~dp0commands\*.md" "%~dp0..\..\.claude\commands\" >nul
if not exist "%~dp0..\..\.claude\settings.json" copy /Y "%~dp0settings.json" "%~dp0..\..\.claude\settings.json" >nul
echo CLAUDE_SETUP_DONE: /check, /optimize
