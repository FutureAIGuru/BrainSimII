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

REM Sign the program binaries
signtool sign /f "C:\Users\c_sim\Documents\Visual Studio 2015\Projects\Certificate\FutureAIEV.cer" /p FutureAI /t httP://timestamp.comodoca.com "C:\Users\c_sim\Documents\Visual Studio 2015\Projects\BrainSimulator\BrainSimulator\bin\x64\Release\brainsimulator.exe" "C:\Users\c_sim\Documents\Visual Studio 2015\Projects\BrainSimulator\BrainSimulator\bin\x64\Release\webcamlib.dll" "C:\Users\c_sim\Documents\Visual Studio 2015\Projects\BrainSimulator\BrainSimulator\bin\x64\Release\touchless.vision.dll" "C:\Users\c_sim\Documents\Visual Studio 2015\Projects\BrainSimulator\BrainSimulator\bin\x64\Release\NeuronEngine.dll" "C:\Users\c_sim\Documents\Visual Studio 2015\Projects\BrainSimulator\BrainSimulator\bin\x64\Release\NeuronEngineWrapper.dll" "C:\Users\c_sim\Documents\Visual Studio 2015\Projects\BrainSimulator\BrainSimulator\bin\x64\Release\NeuronServer.exe"

REM Run the installer
makensis _BRAINSIM2.nsi

REM Sign the install .exe
signtool sign /f "C:\Users\c_sim\Documents\Visual Studio 2015\Projects\Certificate\FutureAIEV.cer" /p FutureAI /t httP://timestamp.comodoca.com "C:\Users\c_sim\Documents\Visual Studio 2015\Projects\BrainSimulator\Installer\Brain Simulator II Setup.exe"

ECHO Copy the exe file to the website upload folder
copy "Brain Simulator II Setup.exe" "C:\Users\c_sim\source\repos\FutureAI\FutureAI"

Echo write the file version to the website upload folder
PowerShell.exe (get-command 'C:\Users\c_sim\Documents\Visual Studio 2015\Projects\BrainSimulator\BrainSimulator\bin\x64\Release\brainsimulator.exe').fileversioninfo.ProductVersion > "C:\Users\c_sim\source\repos\FutureAI\FutureAI\LatestBrainSimVersion.txt"


cmd /k