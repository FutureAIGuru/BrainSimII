!define LIC_NAME "licdata.txt"
!define COMPANY_NAME  "FutureAI"
!define APP_NAME "Brain Simulator II"

!define INSTALLSIZE  10000
!define VERSIONMAJOR 1
!define VERSIONMINOR 0
!define VERSIONBUILD 20210416

!define HELPURL "https://futureai.guru/TheBrainSimBook.aspx"
!define UPDATEURL "https://futureai.guru/BrainSimDownload.aspx"
!define ABOUTURL "https://futureai.guru/BooksByCharlesSimon.aspx"

!define DESCRIPTION "The experiment kit for AGI"

Name "${APP_NAME}"
OutFile "$DESKTOP\${APP_NAME} Setup.exe"
Icon bsicon.ico

ShowInstDetails show
XPStyle on

!include "LogicLib.nsh"

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
   ;SendMessage $0 ${EM_SETLIMITTEXT} $1 0
   ;SendMessage $0 ${WM_SETTEXT} 0 $2
   System::Free $2
exit:
 
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
        advsplash::show 1000 600 400 -1 $PLUGINSDIR\splash
        Pop $0
        Delete $PLUGINSDIR\splash.bmp
        Delete $PLUGINSDIR\splash.wav

        File /oname=$EXEDIR\licdata.txt "licdata.txt"
FunctionEnd

Page directory /ENABLECANCEL
Page components "" "" ComponentsLeave /ENABLECANCEL
Page instfiles /ENABLECANCEL

Section
  SetOutPath $INSTDIR
  createDirectory "$SMPROGRAMS\${COMPANY_NAME}"

  # Uninstaller - See function un.onInit and section "uninstall" for configuration
  writeUninstaller "$INSTDIR\uninstall.exe"
  
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
SectionEnd

Section    "Brain Simulator II" BRAINSIM2
  File /oname=$INSTDIR\bsicon.ico "bsicon.ico"
  File /oname=$INSTDIR\BrainSimulator.exe "..\BrainSimulator\bin\x64\Debug\BrainSimulator.exe"
  File /r /x /oname=$INSTDIR\Resources ..\BrainSimulator\bin\x64\Debug\Resources
  File /oname=$INSTDIR\NeuronEngine.dll "..\BrainSimulator\bin\x64\Debug\NeuronEngine.dll"
  File /oname=$INSTDIR\NeuronEngine.pdb "..\BrainSimulator\bin\x64\Debug\NeuronEngine.pdb"
  File /oname=$INSTDIR\NeuronEngineWrapper.dll "..\BrainSimulator\bin\x64\Debug\NeuronEngineWrapper.dll"
  File /oname=$INSTDIR\NeuronEngineWrapper.pdb "..\BrainSimulator\bin\x64\Debug\NeuronEngineWrapper.pdb"
  createShortCut "$SMPROGRAMS\${COMPANY_NAME}\Brain Simulator.lnk" "$INSTDIR\BrainSimulator.exe" "" "$INSTDIR\bsicon.ico"
SectionEnd

Section "NeuronServer" NEURONSERVER
  File /oname=$INSTDIR\nsicon.ico "nsicon.ico"
  File /oname=$INSTDIR\NeuronServer.exe "..\BrainSimulator\bin\x64\Debug\NeuronServer.exe"
  File /oname=$INSTDIR\NeuronEngine.dll "..\BrainSimulator\bin\x64\Debug\NeuronEngine.dll"
  File /oname=$INSTDIR\NeuronEngine.pdb "..\BrainSimulator\bin\x64\Debug\NeuronEngine.pdb"
  File /oname=$INSTDIR\NeuronEngineWrapper.dll "..\BrainSimulator\bin\x64\Debug\NeuronEngineWrapper.dll"
  File /oname=$INSTDIR\NeuronEngineWrapper.pdb "..\BrainSimulator\bin\x64\Debug\NeuronEngineWrapper.pdb"
  createShortCut "$SMPROGRAMS\${COMPANY_NAME}\NeuronServer.lnk" "$INSTDIR\NeuronServer.exe" "" "$INSTDIR\nsicon.ico"
SectionEnd

Section "EngineTest" ENGINETEST
SectionEnd

Section "Network Examples" NETWORKEXAMPLES
  File /r /x /oname=$INSTDIR\Networks ..\BrainSimulator\bin\x64\Debug\Networks
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
  ${If} ${SectionIsSelected} ${ENGINETEST}
    StrCpy $R1 1
  ${EndIf}
  ${Unless} $R1 == 1	
    MessageBox MB_OK "Please select at least one component"
    Abort
  ${EndIf}
FunctionEnd

# Uninstaller
 
function un.onInit
	SetShellVarContext all
 
	#Verify the uninstaller - last chance to back out
	MessageBox MB_OKCANCEL "Permanantly remove ${APP_NAME}?" IDOK next
		Abort
	next:
	!insertmacro VerifyUserIsAdmin
functionEnd
 
section "uninstall"
 
	# Remove Start Menu launchers
	delete "$SMPROGRAMS\${COMPANY_NAME}\Brain Simulator.lnk"
	delete "$SMPROGRAMS\${COMPANY_NAME}\NeuronServer.lnk"
	# Try to remove the Start Menu folder - this will only happen if it is empty
	rmDir "$SMPROGRAMS\${COMPANY_NAME}"
 
	# Remove files
	delete $INSTDIR\app.exe
	delete $INSTDIR\logo.ico
 
	# Always delete uninstaller as the last action
	delete $INSTDIR\uninstall.exe
 
	# Try to remove the install directory - this will only happen if it is empty
	rmDir $INSTDIR
 
	# Remove uninstaller information from the registry
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${COMPANY_NAME} ${APP_NAME}"
sectionEnd

!verbose 3
