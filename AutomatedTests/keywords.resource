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
    Should Be Equal 	${ret_code}         ${true}         no File => New not triggered
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
	
Check Menu Item
	[arguments]			${menu_item}        ${error_message}
    ${ret_code}			Wait For Image      ${menu_item}
    Should Be Equal 	${ret_code}         ${true}         ${error_message}
	
Check File Menu
    Harmless Click To Focus
    ${ret_code}			Wait And Click		bs2_file_menu
    Should Be Equal 	${ret_code}         ${true}     File menu could not be clicked
    Check Menu Item		bs2_file_new_item				no File => New present
    Check Menu Item		bs2_file_open_item				no File => Open present
    Check Menu Item		bs2_file_save_item_disabled 	no File => Save present
    Check Menu Item		bs2_file_save_as_item			no File => Save As present
    Check Menu Item		bs2_file_properties_item		no File => Properties present
    Check Menu Item		bs2_file_no_recent_item			no File => Recent Items sub present
    Check Menu Item		bs2_file_library_item			no File => Library sub present
    Check Menu Item		bs2_file_exit_item				no File => Exit present

Check Edit Menu
    Harmless Click To Focus
    ${ret_code}			Wait And Click		bs2_edit_menu
    Should Be Equal 	${ret_code}         ${true}         	Edit menu could not be clicked
    Check Menu Item		bs2_edit_find_module_item_disabled		no Edit => Find Module present
    Check Menu Item		bs2_edit_find_neuron_item_disabled		no Edit => Find Neuron present
    Check Menu Item		bs2_edit_insert_module_item             no Edit => Insert Module present
    Check Menu Item		bs2_edit_undo_item_disabled				no Edit => Undo present
    Check Menu Item		bs2_edit_cut_item_disabled				no Edit => Cut present
    Check Menu Item		bs2_edit_copy_item_disabled	    		no Edit => Copy present
    Check Menu Item		bs2_edit_paste_item_disabled			no Edit => Paste present
    Check Menu Item		bs2_edit_delete_item_disabled			no Edit => Delete present
    Check Menu Item		bs2_edit_move_item_disabled				no Edit => Move present
    Check Menu Item		bs2_edit_clear_selection_item			no Edit => Clear Selection present
    Check Menu Item		bs2_edit_select_all_disabled		    no Edit => Select All present
    Check Menu Item		bs2_edit_load_clipboard_item			no Edit => Load Clipboard present
    Check Menu Item		bs2_edit_save_clipboard_item_disabled	no Edit => Save Clipboard present
    Check Menu Item		bs2_edit_notes_item						no Edit => Network Notes present
    Check Menu Item		bs2_edit_module_descriptions_item		no Edit => Module Descriptions present

Check Engine Menu
    Harmless Click To Focus
    ${ret_code}			Wait And Click		bs2_engine_menu
    Check Menu Item		bs2_engine_reset_item					no Engine => Reset present
    Check Menu Item		bs2_engine_run_item_disabled		    no Engine => Run present
    Check Menu Item		bs2_engine_pause_item					no Engine => Pause present
    Check Menu Item		bs2_engine_step_item					no Engine => Step present
    Check Menu Item		bs2_engine_speed_item					no Engine => Speed present
    Check Menu Item		bs2_engine_refractory_item				no Engine => Refractory present
    Check Menu Item		bs2_engine_threads_item					no Engine => Threads present

Check View Menu
    Harmless Click To Focus
    ${ret_code}			Wait And Click		bs2_view_menu
    Check Menu Item		bs2_view_start_pan_item					no View => Start Pan present
    Check Menu Item		bs2_view_zoom_in_item					no View => Zoom In present
    Check Menu Item		bs2_view_zoom_out_item					no View => Zoom Out present
    Check Menu Item		bs2_view_show_all_item					no View => Show All present
    Check Menu Item		bs2_view_origin_item					no View => Origin present
    Check Menu Item		bs2_view_show_synapses_unchecked	    no View Show Synapses present

Check Help Menu
    Harmless Click To Focus
    ${ret_code}			Wait And Click		bs2_help_menu
    Check Menu Item		bs2_help_getting_started_item			no Help => Getting Started present
    Check Menu Item		bs2_help_register_item					no Help => Register present
    Check Menu Item		bs2_help_contents_item					no Help => Help Contents present
    Check Menu Item		bs2_help_report_bugs_item				no Help => Report Bugs present
    Check Menu Item		bs2_help_facebook_item					no Help => Join Facebook present
    Check Menu Item		bs2_help_watch_youtube_item				no Help => Watch Youtube present
    Check Menu Item		bs2_help_about_item						no Help => About present
    Check Menu Item		bs2_help_check_updates_item				no Help => Check Updates present
    Check Menu Item		bs2_help_show_at_startup_unchecked		no Help => Show at Startup present

Check Icon Bar
    Harmless Click To Focus
    Check Menu Item		bs2_icon_new_enabled					no New icon present
    Check Menu Item		bs2_icon_open_enabled					no Open icon present
    Check Menu Item		bs2_icon_save_disabled					no Save icon present
    Check Menu Item		bs2_icon_save_as_enabled				no Save As icon present	
    Check Menu Item		bs2_icon_pan_enabled					no Pan icon present
    Check Menu Item		bs2_icon_zoom_out_enabled				no Zoom Out icon present	
    Check Menu Item		bs2_icon_zoom_in_enabled				no Zoom In icon present	
    Check Menu Item		bs2_icon_origin_enabled					no Origin icon present
    Check Menu Item		bs2_icon_all_synapses_unchecked			no All Synapses icon present		
    Check Menu Item		bs2_icon_reset_enabled					no Reset icon present
    Check Menu Item		bs2_icon_pause_enabled					no Pause icon present
    Check Menu Item		bs2_icon_run_enabled					no Run icon present
    Check Menu Item		bs2_icon_step_enabled					no Step icon present
    Check Menu Item		bs2_icon_speed_enabled					no Speed icon present
    Check Menu Item		bs2_icon_speed_slider					no Speed Slider icon present
    Check Menu Item		bs2_icon_add_synapse_with				no Add Synapse With section present

Check Icon Tooltips
    Harmless Click To Focus
    Wait And Check Tooltip		bs2_icon_new_enabled			bs2_tooltip_new
    Wait And Check Tooltip		bs2_icon_open_enabled			bs2_tooltip_open
    Wait And Check Tooltip		bs2_icon_save_as_enabled		bs2_tooltip_save_as
    Wait And Check Tooltip		bs2_icon_pan_enabled			bs2_tooltip_pan
    Wait And Check Tooltip		bs2_icon_zoom_out_enabled		bs2_tooltip_zoom_out
    Wait And Check Tooltip		bs2_icon_zoom_in_enabled		bs2_tooltip_zoom_in
    Wait And Check Tooltip		bs2_icon_origin_enabled			bs2_tooltip_origin
    Wait And Check Tooltip		bs2_icon_reset_enabled			bs2_tooltip_reset
    Wait And Check Tooltip		bs2_icon_pause_enabled			bs2_tooltip_pause
    Wait And Check Tooltip		bs2_icon_run_enabled			bs2_tooltip_run
    Wait And Check Tooltip		bs2_icon_step_enabled			bs2_tooltip_step
    Wait And Check Tooltip		bs2_icon_speed_slider			bs2_tooltip_speed
    
Check Icon Checkboxes
    Harmless Click To Focus
    Wait And Click				bs2_icon_all_synapses_unchecked
    Harmless Click To Focus
    Wait And Click				bs2_icon_all_synapses_checked
    Wait And Click				bs2_icon_update_from_click_unchecked
    Harmless Click To Focus
    Wait And Click				bs2_icon_update_from_click_checked

Check Add Module Combobox
    Harmless Click To Focus
	Wait For Image				bs2_add_module_collapsed
    Wait And Click				bs2_add_module_collapsed
    Sleep						0.5s
    Wait For Image				bs2_add_module_expanded_1
    Click						${1460}		${420}    # scroll down combobox...
    Wait And Click				bs2_add_module_expanded_2
    Click						${1460}		${420}    # scroll down combobox...
    Wait And Click				bs2_add_module_expanded_3

Check Synapse Weight Combobox
    Harmless Click To Focus
    Wait And Click				bs2_icon_weight_collapsed
    Sleep						0.5s
    Wait For Image				bs2_icon_weight_expanded
    
Check Synapse Model Combobox
    Harmless Click To Focus
    Wait And Click				bs2_icon_model_collapsed
    Sleep						0.5s
    Wait For Image				bs2_icon_model_expanded
    
Check New Network Complete
    Wait For Image				new_network_dialog_title
    Wait For Image				new_network_dialog_textblock
    Wait For Image				new_network_dialog_width
    Wait For Image				new_network_dialog_height
    Wait For Image				new_network_dialog_refractory_cycles
    Wait For Image				new_network_dialog_use_servers_unchecked
    Wait For Image				new_network_dialog_ok_default
    Wait For Image				new_network_dialog_title
    Wait And Click				new_network_dialog_cancel_enabled
    
Check File New Shows New Network Dialog
    Do Menu Choice				bs2_file_menu		bs2_file_new_item
    Check New Network Complete

Check Icon New Shows New Network Dialog
    Do Icon Choice				bs2_icon_new_enabled
    Check New Network Complete

Check Open Network Dialog Complete
    Wait For Image				file_open_dialog_title
    Wait For Image				file_open_dialog_filename
    Wait For Image				file_open_dialog_filetype
    Wait For Image				file_open_dialog_open_default
    Wait And Click				file_open_dialog_cancel_enabled
    
Check File Open Shows Network Load Dialog
    Do Menu Choice				bs2_file_menu		bs2_file_open_item
    Check Open Network Dialog Complete

Check Icon Open Shows Network Load Dialog
    Do Icon Choice				bs2_icon_open_enabled
    Check Open Network Dialog Complete

Check Save As Dialog Complete
    Wait For Image				save_as_dialog_title
    Wait For Image				save_as_dialog_filename
    Wait For Image				save_as_dialog_filetype
    Wait For Image				save_as_dialog_save_default
    Wait And Click				save_as_dialog_cancel_enabled
    
Check File Save As Shows Network Save As Dialog
    Do Menu Choice				bs2_file_menu		bs2_file_save_as_item
    Check Save As Dialog Complete

Check Icon Save As Shows Network Save As Dialog
    Do Icon Choice				bs2_icon_save_as_enabled
    Check Save As Dialog Complete
    
Check Network Library Entry
	[arguments]					${menu_item}		${relevant_part}
    Harmless Click To Focus
    Wait And Click				bs2_file_menu
    Wait And Click				bs2_file_library_item
    Wait And Click				${menu_item}
    Wait And Click				notes_ok_button_enabled
    Wait For Image				${relevant_part}

Check Recent Network Entry
	[arguments]					${menu_item} 		${tool_tip}			${relevant_part}
    Harmless Click To Focus
    Wait And Click				bs2_file_menu
    Wait And Click				bs2_file_recent_item
    Wait And Hover				${menu_item}
    Wait For Image    			${tool_tip}
    Wait And Click				${menu_item}
    Wait And Click				notes_ok_button_enabled
    Wait For Image				${relevant_part}

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
