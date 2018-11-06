@echo off
setlocal enableextensions enabledelayedexpansion 

set ServerRoot=%USERPROFILE%\Downloads\httpd-2.4.37-win64-VC15\Apache24
set ModulesDir=%ServerRoot%\modules
set InstanceRoot=%~dp0.
set ErrorLog=%InstanceRoot%\logs\error.log

set ModAuthOpenIDC=%USERPROFILE%\Downloads\mod_auth_openidc-2.3.8-apache-2.4.x-win64\Apache24\modules
set PATH=%ModAuthOpenIDC%\..\bin;%PATH%

if not exist "%InstanceRoot%\logs" (
    mkdir "%InstanceRoot%\logs"
)
if exist "%ErrorLog%" (
    del /f/q "%ErrorLog%"
)

"%ServerRoot%\bin\httpd.exe" -w -X -d "%ServerRoot%" -f "%InstanceRoot%\conf\httpd.conf" -E "%ErrorLog%" -DWindows %*
