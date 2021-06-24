*** Settings ***
Documentation           Keywords for BrainSIM II testing

Library					teststeps.py
Library                 testtoolkit.py

*** Variables ***

*** Keywords ***

Check Test Requirements
    ${ret_code}	 		Is Screen Size Correct
    Should Be Equal		${ret_code}			${true}			Incorrect screen size
    ${ret_code}			Wait For Image		brainsim_start			
    Should Be Equal 	${ret_code}         ${true}         Brain Simulator II icon not on start bar
    ${ret_code}			Wait For Image		neuronserver_start  	
    Should Be Equal		${ret_code}         ${true}         Neuron Server icon not on start bar

Start Brain Simulator
    ${ret_code}			Wait And Click		brainsim_start
    Should Be Equal 	${ret_code}         ${true}         Brain Simulator II icon not on start bar
    ${ret_code}			Wait For Image     brainsim_started
    Should Be Equal 	${ret_code}         ${true}         Brain Simulator II did not start
    ${ret_code}			Wait For Image     brainsim_splash
    Should Be Equal 	${ret_code}         ${true}         Splash screen is not showing
    Wait Until Keyword Succeeds				30s     5x		Not On Screen		brainsim_splash

Start Brain Simulator Without Network
	Key Down			shift
    ${ret_code}			Wait And Click		brainsim_start
    Should Be Equal 	${ret_code}         ${true}         Brain Simulator II icon not on start bar
    ${ret_code}			Wait For Image      brainsim_started
    Should Be Equal 	${ret_code}         ${true}         Brain Simulator II did not start
    ${ret_code}			Wait For Image      brainsim_splash
    Should Be Equal 	${ret_code}         ${true}         Splash screen is not showing
	Key Up 				shift
    Wait Until Keyword Succeeds				30s     5x		Not On Screen		brainsim_splash
	Sleep				10s

Start Brain Simulator With New Network
    Start Brain Simulator
    ${ret_code}			Do Icon Choice		bs2_icon_new_enabled
    Should Be Equal 	${ret_code}         ${true}         File => New not triggered
    ${ret_code}			Wait And Click		new_network_dialog_ok_default
    Should Be Equal 	${ret_code}         ${true}         New dialog not OK-ed
	
Start Brain Simulator With Getting Started
    Start Brain Simulator
    ${ret_code}			Wait For Image      getting_started
    Should Be Equal 	${ret_code}         ${true}         Getting Started not displayed
    Press Alt F4
    Do Menu Choice      bs2_help_menu       bs2_getting_started_checked
    Harmless Click To Focus

Stop Brain Simulator
    Harmless Click To Focus
    ${ret_code}			Wait And Click		close_icon
    Should Be Equal 	${ret_code}         ${true}         Close icon could not be clicked
    Select No On Save Prompt
    ${ret_code}			Wait For Image      brainsim_start
    Should Be Equal 	${ret_code}         ${true}         Brain Simulator II did not stop
    	
Start Neuronserver
    ${ret_code}			Wait And Click		neuronserver_start
    Should Be Equal 	${ret_code}         ${true}         Neuronserver icon not on start bar
    Harmless Click To Focus
    ${ret_code}			Wait For Image      neuronserver_started
    Should Be Equal 	${ret_code}         ${true}         Neuronserver did not start

Stop Neuronserver
    Harmless Click To Focus
    ${ret_code}			Wait And Click		close_icon
    Should Be Equal 	${ret_code}         ${true}         Close icon could not be clicked
    Harmless Click To Focus
    ${ret_code}			Wait For Image      neuronserver_start
    Should Be Equal 	${ret_code}         ${true}         Neuronserver did not stop
		
Check Module Is Inserted Correctly
	[arguments]			${page}   ${index}   ${drawn_module}
    Insert Module		${page}   ${index}
    Harmless Click To Focus
    Sleep				1s
    ${ret_code}			Wait And Hover		${drawn_module}
	Should Be True		${ret_code}			original module not found on screen
    Remove Module
   
Check Does Module Resize And Undo Correctly
	[arguments]			${page}   ${index}   ${x_start}   ${y_start}   ${x_end}   ${y_end}   ${sized_module}   ${drawn_module}
    Insert Module		${page}   ${index}
    Sleep				2s
    Drag From To		${x_start}   ${y_start}   ${x_end}   ${y_end}   ${1}
    Harmless Click To Focus
    Sleep				1s
    ${ret_code}			Wait And Hover		${sized_module}
	Should Be True		${ret_code}			resized module not found on screen
    Do Menu Choice		bs2_edit_menu 		bs2_edit_undo_item_enabled
    Harmless Click To Focus
    Sleep				1s
    ${ret_code}			Wait And Hover		${drawn_module}
	Should Be True		${ret_code}			original module not found on screen
    Remove Module

Check Do Selections Convert To Modules Correctly
	[arguments]			${page}   	${x_start}  ${y_start}  ${x_end}   ${y_end}   ${mod_name}    ${sized_module}
    Sleep				0.5s
	Mouse Down			${x_start}		${y_start}
    Mouse Move To		${x_end}        ${y_end}
	Mouse Up 			${x_end}        ${y_end}
    Harmless Click To Focus
	Sleep				0.5s
	Right Click			${x_start}  	${y_start}
	Wait And Click		module_convert_combobox
	Select Module		${page}			${mod_name}
	Click				${170}			${350}
    ${ret_code}			Wait And Hover		${sized_module}
	Should Be True		${ret_code}			resized module not found on screen
	Sleep				0.5s
    Harmless Click To Focus
    Remove Module
