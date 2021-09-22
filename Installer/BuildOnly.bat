REM WARNING
REM this is a first cut. It doesn't check for success, it runs in a single case, it is hard-coded to my directory structure

REM Set up the developer path
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" amd64

REM for some reason NSIS doesn't add a path to itself'
SET PATH=%PATH%;C:\Program Files (x86)\NSIS\

REM We assume you are starting the program from this folder
REM cd C:\Users\c_sim\Documents\Visual Studio 2015\Projects\BrainSimulator\Installer\

REM Build the program
msbuild -p:Configuration=Release "..\BrainSimulator.sln"


REM Run the installer
makensis _BRAINSIM2.nsi



cmd /k