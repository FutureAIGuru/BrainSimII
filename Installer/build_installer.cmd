@ECHO OFF
ECHO **********************************************************************
ECHO * BrainSimII Build and Make Installer with or without signing        *
ECHO **********************************************************************
ECHO * Just execute to build project and make installer for local use     *
ECHO * or use a signing certificate for the creation                      *
ECHO * of a signed installer that can be put on the Web for distribution  *
ECHO **********************************************************************

REM put quote marks on current folder so paths with spaces will work
SET ORGDIR=%CD%

CD %ORGDIR%
CD ..
SET BINDIR=%CD%\BrainSimulator\bin\\release\net6.0-windows
CD %ORGDIR%

:CERTFILERETRY
SET CERTFILE=
set /p CERTFILE="Enter 'y' to digitally sign, anything else  to skip: "

CD %ORGDIR%

ECHO Current folder:  %ORGDIR%
ECHO Certificate file: %CERTFILE%
ECHO Binary folder: %BINDIR%

PAUSE
ECHO Delete old log files. . .
del "%ORGDIR%\Step*.log""

ECHO Setting up the developer path. . .
IF NOT DEFINED DevEnvDir (
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" amd64
)

REM NSIS doesn't create its own path
path|find /i "NSIS" > nul || SET PATH=%PATH%;C:\Program Files (x86)\NSIS\
REM this is some secret stuff which only adds to the path if necessary


ECHO Building the program. . .
msbuild -p:Configuration=Release -maxCpuCount:16 -t:rebuild "..\BrainSimulator.sln" >"%ORGDIR%\Step2Build.log"
PAUSE


IF "%CERTFILE%"=="y" (
	ECHO Signing the program binaries. . .Enter password [NewPassword] in popup window
	signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a "%BINDIR%\brainsimulator.exe" "%BINDIR%\brainsimulator.dll" "%BINDIR%\NeuronEngine.dll" "%BINDIR%\NeuronEngineWrapper.dll" "%BINDIR%\NeuronServer.exe" "%BINDIR%\NeuronServer.dll" "%BINDIR%\system.speech.dll" "%BINDIR%\system.IO.ports.dll" "%BINDIR%\runtimes\win\lib\net6.0\system.IO.ports.dll"  "%BINDIR%\runtimes\win\lib\net6.0\system.speech.dll" > "%ORGDIR%\Step3SignExecutables.log"
	PAUSE
) else (
ECHO Signing skipped
)

ECHO Creating the Installer. . .
makensis _BRAINSIM2.nsi >"%ORGDIR%\Step4BuildInstaller.log"
PAUSE

IF "%CERTFILE%"=="y" (
	ECHO Signing the install.exe . . .Enter password [NewPassword] in popup window
	signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a "%ORGDIR%\BrainSimII_Setup.exe" >"%ORGDIR%\Step5SignInstaller.log"
	PAUSE
) else (
ECHO Signing skipped
)



ECHO Create the version file 
..\GetVersionInfo\bin\Debug\net6.0\GetVersionInfo "%BINDIR%\brainsimulator.exe" > "%ORGDIR%\LatestBrainSimVersion.txt"

ECHO Creating the .zip file
powershell compress-archive -Update -Path %ORGDIR%\BrainSimII_Setup.exe -DestinationPath %ORGDIR%\BrainSimII_Setup.zip


ECHO Done!
PAUSE