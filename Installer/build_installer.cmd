@ECHO OFF
ECHO **********************************************************************
ECHO * BrainSimII Build and Make Installer with or without signing        *
ECHO **********************************************************************
ECHO * Just execute to build project and make installer for local use     *
ECHO * or drop a signing certificate on the CMD file for the creation     *
ECHO * of a signed installer that can be put on the Web for distribution  *
ECHO **********************************************************************

REM put quote marks on current folder so paths with spaces will work
SET ORGDIR=%CD%

CD %ORGDIR%
CD ..
CD ..

SET BINDIR=%CD%\BrainSimulator\BrainSimulator\bin\x64\release

IF EXIST [%1] (
	SET CERTFILE=%1
) ELSE (
	SET CERTFILE="%CD%\Certificate\FutureAIEV.cer"
)
CD %ORGDIR%

SET WEBDIR=C:\Users\c_sim\source\repos\FutureAI\FutureAI
IF NOT EXIST %WEBDIR% (
	SET WEBDIR=C:\Users\c_sim\source\repos\FutureAIFuru\FutureAIWebsite
)

REM ECHO ON

IF NOT EXIST %WEBDIR% (SET WEBDIR="")

ECHO Current folder:  %ORGDIR%
ECHO Certificate file: %CERTFILE%
ECHO Binary folder: %BINDIR%
ECHO Website folder: %WEBDIR%

REM GOTO:COPYFILES


PAUSE
ECHO Delete old log files. . .
del "%ORGDIR%\Step*.log""

ECHO Setting up the developer path. . .
IF NOT DEFINED DevEnvDir (
    call vcvarsall.bat amd64 >"%ORGDIR%\Step1Setup.log"
)
REM NSIS doesn't create its own path
path|find /i "NSIS" > nul || SET PATH=%PATH%;C:\Program Files (x86)\NSIS\
REM this is some secret stuff which only adds to the path if necessary

ECHO Building the program. . .
msbuild -p:Configuration=Release "..\BrainSimulator.sln" >"%ORGDIR%\Step2Build.log"
PAUSE


IF EXIST %CERTFILE% (
ECHO Signing the program binaries. . .
signtool sign /f %CERTFILE% /p FutureAI /t http://timestamp.comodoca.com "%BINDIR%\brainsimulator.exe" "%BINDIR%\webcamlib.dll" "%BINDIR%\touchless.vision.dll" "%BINDIR%\NeuronEngine.dll" "%BINDIR%\NeuronEngineWrapper.dll" "%BINDIR%\NeuronServer.exe" >"%ORGDIR%\Step3SignExecutables.log"
)
PAUSE

ECHO Creating the Installer. . .
makensis _BRAINSIM2.nsi >"%ORGDIR%\Step4BuildInstaller.log"
PAUSE

IF EXIST %CERTFILE%y (
ECHO Signing the install.exe. . .
signtool sign /f %CERTFILE% /p FutureAI /t http://timestamp.comodoca.com "%ORGDIR%\Brain Simulator II Setup.exe" >"%ORGDIR%\Step5SignInstaller.log"
)

:COPYFILES    

ECHO Create the version file 
..\GetVersionInfo\bin\Debug\netcoreapp3.1\GetVersionInfo "%BINDIR%\brainsimulator.exe" > "%ORGDIR%\LatestBrainSimVersion.txt"

ECHO Copy files to %WEBDIR%
IF EXIST "%WEBDIR%" (
	ECHO Copy the installer exe file to the website upload folder
	copy "Brain Simulator II Setup.exe" %WEBDIR%

	ECHO Copy the version file to the website upload folder
	copy "LatestBrainSimVersion.txt" %WEBDIR%
)

ECHO Done!
REM copy "T:\REPOSITORY\BrainSimII\Installer\Brain Simulator II Setup.exe" T:\MK_INSTALLER
PAUSE