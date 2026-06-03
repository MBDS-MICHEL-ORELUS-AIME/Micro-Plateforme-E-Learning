@echo off
echo Lancement de la plateforme E-Learning...
cd /d "%~dp0E-learningProject.Web"
dotnet run --launch-profile http
pause
