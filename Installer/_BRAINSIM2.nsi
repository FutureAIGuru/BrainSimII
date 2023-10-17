#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

!define LIC_NAME "licdata.txt"
!define COMPANY_NAME  "FutureAI"
!define APP_NAME "BrainSimII"

!define INSTALLSIZE  10000
!define VERSIONMAJOR 1
!define VERSIONMINOR 0
!define VERSIONBUILD 20210416

!define HELPURL "https://futureai.guru/TheBrainSimBook.aspx"
!define UPDATEURL "https://futureai.guru/BrainSimDownload.aspx"
!define ABOUTURL "https://futureai.guru/BooksByCharlesSimon.aspx"

!define DESCRIPTION "The experiment kit for AGI"

!define BUILD_TYPE Release

Name "${APP_NAME}"
OutFile "${APP_NAME}_Setup.exe"

!include "MUI.nsh"
!define MUI_ICON "bsicon.ico"
!define MUI_UNICON "bsicon.ico"

ShowInstDetails show
XPStyle on

!include "LogicLib.nsh"
!include "FileAssociation.nsh"

!undef LOGICLIB_VERBOSITY
!define LOGICLIB_VERBOSITY 4   ; For debugging - logiclib does with your code!

InstallDir "$PROGRAMFILES64\${COMPANY_NAME}\${APP_NAME}"

Function addLicense
    ClearErrors
    FileOpen $0 $EXEDIR\${LIC_NAME} r
    IfErrors exit
    System::Call 'kernel32::GetFileSize(i r0, i 0) i .r1'
    IntOp $1 $1 + 1 ; for terminating zero
    System::Alloc $1
    Pop $2
    System::Call 'kernel32::ReadFile(i r0, i r2, i r1, *i .r3, i 0)'
    FileClose $0
    FindWindow $0 "#32770" "" $HWNDPARENT
    GetDlgItem $0 $0 1000
    System::Free $2
exit:
FunctionEnd

Function LaunchLink
  ExecShell "" "$INSTDIR\BrainSimulator.exe"
FunctionEnd

!include "MUI2.nsh"
!insertmacro MUI_PAGE_WELCOME
!define MUI_PAGE_CUSTOMFUNCTION_SHOW addLicense
!insertmacro MUI_PAGE_LICENSE licdata.txt
!insertmacro MUI_LANGUAGE "English"
  
Function .onInit
	# the plugins dir is automatically deleted when the installer exits
	InitPluginsDir
	File /oname=$PLUGINSDIR\splash.bmp "SPLASH.bmp"
	File /oname=$PLUGINSDIR\splash.wav "SPLASH.wav"
	advsplash::show 7000 600 400 -1 $PLUGINSDIR\splash
	Pop $0
	Delete $PLUGINSDIR\splash.bmp
	Delete $PLUGINSDIR\splash.wav
FunctionEnd

Page directory /ENABLECANCEL
Page components "" "" ComponentsLeave /ENABLECANCEL
Page instfiles /ENABLECANCEL
    # These indented statements modify settings for MUI_PAGE_FINISH
    !define MUI_FINISHPAGE_NOAUTOCLOSE
    !define MUI_FINISHPAGE_RUN
    !define MUI_FINISHPAGE_RUN_CHECKED
    !define MUI_FINISHPAGE_RUN_TEXT "Start Brain Simulator II"
    !define MUI_FINISHPAGE_RUN_FUNCTION "LaunchLink"
!insertmacro MUI_PAGE_FINISH

Section
	SetShellVarContext current
	SetOutPath $INSTDIR
	createDirectory "$SMPROGRAMS\${COMPANY_NAME}"
	# Uninstaller - See function un.onInit and section "uninstall" for configuration
	writeUninstaller "$INSTDIR\uninstall.exe"
    File /oname=$INSTDIR\unicon.ico "unicon.ico"
	  
	# Registry information for add/remove programs
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "DisplayName" "${APP_NAME}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "QuietUninstallString" "$\"$INSTDIR\uninstall.exe$\" /S"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "InstallLocation" "$\"$INSTDIR$\""
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "DisplayIcon" "$\"$INSTDIR\bsicon.ico$\""
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "Publisher" "${COMPANY_NAME}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "HelpLink" "$\"${HELPURL}$\""
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "URLUpdateInfo" "$\"${UPDATEURL}$\""
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "URLInfoAbout" "$\"${ABOUTURL}$\""
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "DisplayVersion" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "VersionMajor" ${VERSIONMAJOR}
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "VersionMinor" ${VERSIONMINOR}
	# There is no option for modifying or repairing the install
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "NoModify" 1
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "NoRepair" 1
	# Set the INSTALLSIZE constant (!defined at the top of this script) so Add/Remove Programs can accurately report the size
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}" "EstimatedSize" ${INSTALLSIZE}  
    createShortCut "$SMPROGRAMS\${COMPANY_NAME}\Uninstall Brain Simulator II.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\unicon.ico"
    File /oname=$INSTDIR\SetupFirewall.exe "..\BrainSimulator\bin\release\net6.0-windows\SetupFirewall.exe"
    File /oname=$INSTDIR\SetupFirewall.dll "..\BrainSimulator\bin\release\net6.0-windows\SetupFirewall.dll"
    File /oname=$INSTDIR\SetupFirewall.runtimeconfig.json "..\BrainSimulator\bin\release\net6.0-windows\SetupFirewall.runtimeconfig.json"
    createShortCut "$SMPROGRAMS\${COMPANY_NAME}\Setup Firewall.lnk" "$INSTDIR\SetupFirewall.exe" "" "$INSTDIR\bsicon.ico"
SectionEnd

Section    "Brain Simulator II" BRAINSIM2
	SetShellVarContext current

    File /oname=$INSTDIR\bsicon.ico "bsicon.ico"
    File /oname=$INSTDIR\BrainSimulator.exe "..\BrainSimulator\bin\release\net6.0-windows\BrainSimulator.exe"
    File /oname=$INSTDIR\BrainSimulator.dll "..\BrainSimulator\bin\release\net6.0-windows\BrainSimulator.dll"
    File /oname=$INSTDIR\BrainSimulator.runtimeconfig.json "..\BrainSimulator\bin\release\net6.0-windows\BrainSimulator.runtimeconfig.json"
    File /oname=$INSTDIR\NeuronEngine.dll "..\BrainSimulator\bin\release\net6.0-windows\NeuronEngine.dll"


  File  ..\BrainSimulator\bin\release\net6.0-windows\system.speech.dll
  File  ..\BrainSimulator\bin\release\net6.0-windows\system.IO.Ports.dll
    File  ..\BrainSimulator\bin\release\net6.0-windows\BrainSimulator.deps.json

   File /r /x /oname=$INSTDIR\runtimes ..\BrainSimulator\bin\release\net6.0-windows\runtimes
    
    File /oname=$INSTDIR\NeuronEngineWrapper.dll "..\BrainSimulator\bin\release\net6.0-windows\NeuronEngineWrapper.dll"
#    File /oname=$INSTDIR\NeuronEngineWrapper.pdb "..\BrainSimulator\bin\release\net6.0-windows\NeuronEngineWrapper.pdb"
#    File /oname=$INSTDIR\WebCamLib.dll "..\BrainSimulator\bin\release\net6.0-windows\WebCamLib.dll"
#    File /oname=$INSTDIR\WebCamLib.pdb "..\BrainSimulator\bin\release\net6.0-windows\WebCamLib.pdb"
#    File /oname=$INSTDIR\Touchless.Vision.dll "..\BrainSimulator\bin\release\net6.0-windows\Touchless.Vision.dll"
#    File /oname=$INSTDIR\Touchless.Vision.pdb "..\BrainSimulator\bin\release\net6.0-windows\Touchless.Vision.pdb"
    createShortCut "$DESKTOP\Brain Simulator.lnk" "$INSTDIR\BrainSimulator.exe" "" "$INSTDIR\bsicon.ico"
    createShortCut "$SMPROGRAMS\${COMPANY_NAME}\Brain Simulator.lnk" "$INSTDIR\BrainSimulator.exe" "" "$INSTDIR\bsicon.ico"
	${registerExtension} "$INSTDIR\BrainSimulator.exe" ".xml" "XML File"
SectionEnd

Section "NeuronServer" NEURONSERVER
	SetShellVarContext current
    File /oname=$INSTDIR\nsicon.ico "nsicon.ico"
    File /oname=$INSTDIR\NeuronServer.exe "..\BrainSimulator\bin\release\net6.0-windows\NeuronServer.exe"
    File /oname=$INSTDIR\NeuronServer.dll "..\BrainSimulator\bin\release\net6.0-windows\NeuronServer.dll"
    File /oname=$INSTDIR\NeuronServer.runtimeconfig.json "..\BrainSimulator\bin\release\net6.0-windows\NeuronServer.runtimeconfig.json"
    File /oname=$INSTDIR\NeuronEngine.dll "..\BrainSimulator\bin\release\net6.0-windows\NeuronEngine.dll"
    File /oname=$INSTDIR\SetupFirewall.exe "..\BrainSimulator\bin\release\net6.0-windows\SetupFirewall.exe"
    File /oname=$INSTDIR\SetupFirewall.dll "..\BrainSimulator\bin\release\net6.0-windows\SetupFirewall.dll"
    File /oname=$INSTDIR\SetupFirewall.runtimeconfig.json "..\BrainSimulator\bin\release\net6.0-windows\SetupFirewall.runtimeconfig.json"
#    File /oname=$INSTDIR\NeuronEngine.pdb "..\BrainSimulator\bin\release\net6.0-windows\NeuronEngine.pdb"
    File /oname=$INSTDIR\NeuronEngineWrapper.dll "..\BrainSimulator\bin\release\net6.0-windows\NeuronEngineWrapper.dll"
#    File /oname=$INSTDIR\NeuronEngineWrapper.pdb "..\BrainSimulator\bin\release\net6.0-windows\NeuronEngineWrapper.pdb"
    createShortCut "$DESKTOP\Neuron Server.lnk" "$INSTDIR\NeuronServer.exe" "" "$INSTDIR\nsicon.ico"
    createShortCut "$SMPROGRAMS\${COMPANY_NAME}\Neuron Server.lnk" "$INSTDIR\NeuronServer.exe" "" "$INSTDIR\nsicon.ico"
SectionEnd


Section "Network Examples" NETWORKEXAMPLES
	SetShellVarContext current
    File /r /x /oname=$INSTDIR\Networks ..\BrainSimulator\bin\release\net6.0-windows\Networks
SectionEnd

!macro VerifyUserIsAdmin
UserInfo::GetAccountType
pop $0
${If} $0 != "admin" ;Require admin rights on NT4+
    messageBox mb_iconstop "Administrator rights required!"
    setErrorLevel 740 ;ERROR_ELEVATION_REQUIRED
    quit
${EndIf}
!macroend
 
Function ComponentsLeave
    StrCpy $R1 0
    ${If} ${SectionIsSelected} ${BRAINSIM2}
        StrCpy $R1 1
    ${EndIf}
    ${If} ${SectionIsSelected} ${NEURONSERVER}
        StrCpy $R1 1
    ${EndIf}
    ${Unless} $R1 == 1	
        MessageBox MB_OK "Please select at least one component"
        Abort
    ${EndIf}
FunctionEnd

# Uninstaller
 
function un.onInit
	SetShellVarContext current
	!insertmacro VerifyUserIsAdmin
functionEnd
 
section "uninstall"
	# Remove files
    delete $INSTDIR\bsicon.ico
    delete $INSTDIR\SetupFirewall.exe
    delete $INSTDIR\SetupFirewall.dll
    delete $INSTDIR\SetupFirewall.runtimeconfig.json
    delete $INSTDIR\BrainSimulator.exe
    delete $INSTDIR\BrainSimulator.dll
    delete $INSTDIR\BrainSimulator.runtimeconfig.json
    delete $INSTDIR\NeuronEngine.dll
#    delete $INSTDIR\NeuronEngine.pdb
    delete $INSTDIR\NeuronEngineWrapper.dll
#    delete $INSTDIR\NeuronEngineWrapper.pdb
    delete $INSTDIR\nsicon.ico
    delete $INSTDIR\NeuronServer.exe
    delete $INSTDIR\NeuronServer.dll
    delete $INSTDIR\NeuronServer.runtimeconfig.json
    delete $INSTDIR\NeuronEngine.dll
#    delete $INSTDIR\NeuronEngine.pdb
    delete $INSTDIR\NeuronEngineWrapper.dll
#    delete $INSTDIR\NeuronEngineWrapper.pdb
#    delete $INSTDIR\WebCamLib.dll
#    delete $INSTDIR\WebCamLib.pdb
#    delete $INSTDIR\Touchless.Vision.dll
#    delete $INSTDIR\Touchless.Vision.pdb
    delete $INSTDIR\unicon.ico
    delete $INSTDIR\Networks\*.*
    RMDIR /r $INSTDIR\Networks

	# Remove Start Menu launchers
	delete "$DESKTOP\Brain Simulator.lnk"
    delete "$DESKTOP\Neuron Server.lnk"
	delete "$SMPROGRAMS\${COMPANY_NAME}\Setup Firewall.lnk"
	delete "$SMPROGRAMS\${COMPANY_NAME}\Brain Simulator.lnk"
    delete "$SMPROGRAMS\${COMPANY_NAME}\Neuron Server.lnk"
    delete "$SMPROGRAMS\${COMPANY_NAME}\Uninstall Brain Simulator II.lnk"
	RMDIR  "$SMPROGRAMS\${COMPANY_NAME}"

	# Always delete uninstaller as the last action
	delete $INSTDIR\uninstall.exe
 
	# Try to remove the install directory - this will only happen if it is empty
	RMDIR $INSTDIR
	RMDIR "$PROGRAMFILES64\${COMPANY_NAME}"
	
	${unregisterExtension} ".xml" "XML File"
	
	# Remove uninstaller information from the registry
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}"
sectionEnd

!verbose 3
