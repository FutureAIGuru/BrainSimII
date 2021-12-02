
ECHO Setting up the developer path. . .
IF NOT DEFINED DevEnvDir (
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" amd64
    rem call vcvarsall.bat amd64 >"%ORGDIR%\Step1Setup.log"
)

REM for some reason NSIS doesn't add a path to itself'
SET PATH=%PATH%;C:\Program Files (x86)\NSIS\

REM The location of the executables 
SET EXEDIR=..\BrainSimulator\bin\Release\net6.0-windows
SET WEBDIR=C:\Users\c_sim\source\repos\FutureAIWebsite\FutureAI


REM We assume you are starting this bacth file from this folder
REM cd repos\FutureAIGuru\BrainSimII\Installer\

REM Build the program
msbuild -p:Configuration=Release "..\BrainSimulator.sln"


REM Sign the program binaries
signtool sign /f "C:\Cert\FutureAIEV.cer" /p FutureAI /t httP://timestamp.comodoca.com "%EXEDIR%\brainsimulator.exe" "%EXEDIR%\brainsimulator.dll" "%EXEDIR%\NeuronEngine.dll" "%EXEDIR%\NeuronEngineWrapper.dll" "%EXEDIR%\NeuronServer.exe" "%EXEDIR%\NeuronServer.dll"

REM Run the installer
makensis _BRAINSIM2.nsi

REM Sign the install .exe
signtool sign /f "C:\Cert\FutureAIEV.cer" /p FutureAI /t httP://timestamp.comodoca.com "Brain Simulator II Setup.exe"

ECHO Copy the exe file to the website upload folder
copy "Brain Simulator II Setup.exe" "%WEBDIR%"

Echo write the .EXE version to the website upload folder
..\GetVersionInfo\bin\Debug\net6.0\GetVersionInfo "%EXEDIR%\brainsimulator.exe" > "%WEBDIR%\LatestBrainSimVersion.txt"



cmd /k