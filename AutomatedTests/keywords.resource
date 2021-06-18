*** Settings ***
Documentation           Keywords for BrainSIM II testing

Library					teststeps.py
Library                 testtoolkit.py

*** Variables ***

*** Keywords ***

Check Module Is Inserted Correctly
	[arguments]			${page}   ${index}   ${drawn_module}
    Insert Module		${page}   ${index}
    Harmless Click To Focus
    Sleep				1s
    ${result}			Wait And Hover		${drawn_module}
	Should Be True		${result}			original module not found on screen
    Remove Module
   
Check Does Module Resize And Undo Correctly
	[arguments]			${page}   ${index}   ${x_start}   ${y_start}   ${x_end}   ${y_end}   ${resized_module}   ${drawn_module}
    Insert Module		${page}   ${index}
    Sleep				2s
    Drag From To		${x_start}   ${y_start}   ${x_end}   ${y_end}   ${1}
    Harmless Click To Focus
    Sleep				1s
    ${result}			Wait And Hover		${resized_module}
	Should Be True		${result}			resized module not found on screen
    Do Menu Choice		bs2_edit_menu 		bs2_edit_undo_item_enabled
    Harmless Click To Focus
    Sleep				1s
    ${result}			Wait And Hover		${drawn_module}
	Should Be True		${result}			original module not found on screen
    Remove Module
