#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

import os
import time
import shutil
import pyautogui
from pyscreeze import ImageNotFoundException

import testtoolkit as tk     # the very basic routines we use from PyAutoGUI again and again.

def harmless_click_to_focus():
    return tk.click([145, 1028])

def check_test_requirements():
    # This function checks the requirements the test framework depends on.
    if not tk.screen_size_correct():
        return False
    if not tk.wait_for_center('brainsim_start'):
        return False
    if not tk.wait_for_center('neuronserver_start'):
        return False
    return True
        
def clear_appdata():
    shutil.rmtree('C:\\Users\\Moorelife\\AppData\\Local\\FutureAI')
    return True
        
def start_brain_simulator():
    if tk.wait_and_click('brainsim_start') == False:
        return False
    if tk.wait_for_center('brainsim_started') == False:
        return False
    if not tk.wait_for_center('brainsim_splash'):
        return False
    timeout = 0
    while tk.locate_center('brainsim_splash') and timeout < 20:
        time.sleep(1)
        timeout += 1
    if tk.locate_center('brainsim_splash') is not None:
        return False
    return True

def start_brain_simulator_with_new_network():
    start_brain_simulator()
    do_icon_choice('bs2_icon_new_enabled')
    if tk.wait_and_click('new_network_dialog_ok_default') == False:
        return False
    
def start_brain_simulator_with_getting_started():
    start_brain_simulator()
    if tk.wait_for_center('getting_started') == False:
        return False
    pyautogui.hotkey('alt', 'F4')
    do_menu_choice('bs2_help_menu', 'bs2_getting_started_checked')
    harmless_click_to_focus()
    return True
    
def start_brain_simulator_without_network():
    result = True
    if tk.wait_and_click('brainsim_start') == False:
        return False
    if tk.wait_for_center('brainsim_started') == False:
        return False
    pyautogui.keyDown('shift')
    if not tk.wait_for_center('brainsim_splash'):
        result = False
    timeout = 0
    while tk.locate_center('brainsim_splash') is None and timeout < 20:
        time.sleep(1)
        timeout += 1
    if tk.wait_for_center('brainsim_splash') is None:
        result = False
    pyautogui.keyUp('shift')
    return result

def select_no_on_save_prompt():
    time.sleep(0.2)
    if tk.wait_and_hover('save_question'):
        time.sleep(0.2)
        tk.click([975, 600])
    return True

def stop_brain_simulator():
    result = True
    if not harmless_click_to_focus():
        result = False
    if not tk.wait_and_click('close_icon'):
        result = False
    select_no_on_save_prompt()
    if not tk.wait_for_center('brainsim_start'):
        result = False
    return result

def start_neuronserver():
    if not tk.wait_and_click('neuronserver_start'):
        return False
    if not tk.wait_for_center('neuronserver_started'):
        return False
    return True

def stop_neuronserver():
    if not tk.wait_and_click('close_icon'):
        return False
    if not tk.wait_for_center('neuronserver_start'):
        return False
    return True
    
def check_file_menu():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_and_click('bs2_file_menu'):
        return False
    if not tk.wait_for_center('bs2_file_new_item'):
        return False
    if not tk.wait_for_center('bs2_file_open_item'):
        return False
    if not tk.wait_for_center('bs2_file_save_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_file_save_as_item'):
        return False
    if not tk.wait_for_center('bs2_file_properties_item'):
        return False
    if not tk.wait_for_center('bs2_file_no_recent_item'):
        return False
    if not tk.wait_for_center('bs2_file_library_item'):
        return False
    if not tk.wait_for_center('bs2_file_exit_item'):
        return False
    return True

def check_edit_menu():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_and_click('bs2_edit_menu'):
        return False
    if not tk.wait_for_center('bs2_edit_clear_selection_item'):
        return False
    if not tk.wait_for_center('bs2_edit_cut_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_edit_delete_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_edit_find_module_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_edit_find_neuron_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_edit_load_clipboard_item'):
        return False
    if not tk.wait_for_center('bs2_edit_move_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_edit_notes_item'):
        return False
    if not tk.wait_for_center('bs2_edit_paste_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_edit_save_clipboard_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_edit_undo_item_disabled'):
        return False
    return True

def check_engine_menu():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_and_click('bs2_engine_menu'):
        return False
    if not tk.wait_for_center('bs2_engine_pause_item'):
        return False
    if not tk.wait_for_center('bs2_engine_refractory_item'):
        return False
    if not tk.wait_for_center('bs2_engine_reset_item'):
        return False
    if not tk.wait_for_center('bs2_engine_run_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_engine_speed_item'):
        return False
    if not tk.wait_for_center('bs2_engine_step_item'):
        return False
    if not tk.wait_for_center('bs2_engine_threads_item'):
        return False
    return True

def check_view_menu():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_and_click('bs2_view_menu'):
        return False
    if not tk.wait_for_center('bs2_view_origin_item'):
        return False
    if not tk.wait_for_center('bs2_view_show_all_item'):
        return False
    if not tk.wait_for_center('bs2_view_show_synapses_unchecked'):
        return False
    if not tk.wait_for_center('bs2_view_start_pan_item'):
        return False
    if not tk.wait_for_center('bs2_view_zoom_in_item'):
        return False
    if not tk.wait_for_center('bs2_view_zoom_out_item'):
        return False
    return True

def check_help_menu():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_and_click('bs2_help_menu'):
        return False
    if not tk.wait_for_center('bs2_help_about_item'):
        return False
    if not tk.wait_for_center('bs2_help_contents_item'):
        return False
    if not tk.wait_for_center('bs2_help_facebook_item'):
        return False
    if not tk.wait_for_center('bs2_help_getting_started_item'):
        return False
    if not tk.wait_for_center('bs2_help_register_item'):
        return False
    if not tk.wait_for_center('bs2_help_report_bugs_item'):
        return False
    if not tk.wait_for_center('bs2_help_show_at_startup_unchecked'):
        return False
    if not tk.wait_for_center('bs2_help_watch_youtube_item'):
        return False
    return True

def check_icon_bar():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_for_center('bs2_icon_new_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_open_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_save_disabled'):
        return False
    if not tk.wait_for_center('bs2_icon_save_as_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_pan_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_zoom_out_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_zoom_in_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_origin_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_all_synapses_unchecked'):
        return False
    if not tk.wait_for_center('bs2_icon_reset_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_pause_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_run_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_step_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_speed_enabled'):
        return False
    if not tk.wait_for_center('bs2_icon_speed_slider'):
        return False
    if not tk.wait_for_center('bs2_icon_add_synapse_with'):
        return False
    return True

def check_add_module_combobox():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_for_center('bs2_add_module_collapsed'):
        return False
    if not tk.wait_and_click('bs2_add_module_collapsed'):
        return False
    time.sleep(0.5)
    if not tk.wait_for_center('bs2_add_module_expanded_1'):
        return False
    tk.click([1460, 420]) # scroll down combobox...
    if not tk.wait_for_center('bs2_add_module_expanded_2'):
        return False
    tk.click([1460, 420]) # scroll down combobox...
    if not tk.wait_for_center('bs2_add_module_expanded_3'):
        return False
    return True

def check_synapse_weight_combobox():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_for_center('bs2_icon_weight_collapsed'):
        return False
    if not tk.wait_and_click('bs2_icon_weight_collapsed'):
        return False
    time.sleep(0.5)
    if not tk.wait_for_center('bs2_icon_weight_expanded'):
        return False
    return True
    
def check_synapse_model_combobox():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_for_center('bs2_icon_model_collapsed'):
        return False
    if not tk.wait_and_click('bs2_icon_model_collapsed'):
        return False
    time.sleep(0.5)
    if not tk.wait_for_center('bs2_icon_model_expanded'):
        return False
    return True
    
def check_icon_tooltips():
    if not harmless_click_to_focus():
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_new_enabled', 'bs2_tooltip_new'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_open_enabled', 'bs2_tooltip_open'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_save_as_enabled', 'bs2_tooltip_save_as'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_pan_enabled', 'bs2_tooltip_pan'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_zoom_out_enabled', 'bs2_tooltip_zoom_out'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_zoom_in_enabled', 'bs2_tooltip_zoom_in'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_origin_enabled', 'bs2_tooltip_origin'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_reset_enabled', 'bs2_tooltip_reset'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_pause_enabled', 'bs2_tooltip_pause'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_run_enabled', 'bs2_tooltip_run'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_step_enabled', 'bs2_tooltip_step'):
        return False
    if not tk.wait_and_check_tooltip('bs2_icon_speed_slider', 'bs2_tooltip_speed'):
        return False
    return True
    
def check_icon_checkboxes():
    harmless_click_to_focus()
    if not tk.wait_and_click('bs2_icon_all_synapses_unchecked'):
        return False
    harmless_click_to_focus()
    if not tk.wait_and_click('bs2_icon_all_synapses_checked'):
        return False
    if not tk.wait_and_click('bs2_icon_update_from_click_unchecked'):
        return False
    harmless_click_to_focus()
    if not tk.wait_and_click('bs2_icon_update_from_click_checked'):
        return False
    return True

def do_menu_choice(menu, item):
    if not harmless_click_to_focus():
        return False
    if not tk.wait_and_click(menu):
        return False
    if not tk.wait_and_click(item):
        return False
    
def do_icon_choice(icon_choice):
    if not harmless_click_to_focus():
        return False
    if not tk.wait_and_click(icon_choice):
        return False
    return True

def check_new_network_complete():
    if not tk.wait_for_center('new_network_dialog_title'):
        return False 
    if not tk.wait_for_center('new_network_dialog_textblock'):
        return False 
    if not tk.wait_for_center('new_network_dialog_width'):
        return False 
    if not tk.wait_for_center('new_network_dialog_height'):
        return False 
    if not tk.wait_for_center('new_network_dialog_refractory_cycles'):
        return False 
    if not tk.wait_for_center('new_network_dialog_use_servers_unchecked'):
        return False 
    if not tk.wait_for_center('new_network_dialog_ok_default'):
        return False 
    if not tk.wait_for_center('new_network_dialog_title'):
        return False 
    if not tk.wait_and_click('new_network_dialog_cancel_enabled'):
        return False 
    return True
    
def check_file_new_shows_new_network_dialog():
    do_menu_choice('bs2_file_menu', 'bs2_file_new_item')
    return check_new_network_complete()

def check_icon_new_shows_new_network_dialog():
    do_icon_choice('bs2_icon_new_enabled')
    return check_new_network_complete()
 
def check_open_network_dialog_complete():
    if not tk.wait_for_center('file_open_dialog_title'):
        return False 
    if not tk.wait_for_center('file_open_dialog_filename'):
        return False 
    if not tk.wait_for_center('file_open_dialog_filetype'):
        return False 
    if not tk.wait_for_center('file_open_dialog_open_default'):
        return False 
    if not tk.wait_and_click('file_open_dialog_cancel_enabled'):
        return False 
    return True
    
def check_file_open_shows_network_load_dialog():
    do_menu_choice('bs2_file_menu', 'bs2_file_open_item')
    return check_open_network_dialog_complete()

def check_icon_open_shows_network_load_dialog():
    do_icon_choice('bs2_icon_open_enabled')
    return check_open_network_dialog_complete()

def check_save_as_dialog_complete():
    if not tk.wait_for_center('save_as_dialog_title'):
        return False 
    if not tk.wait_for_center('save_as_dialog_filename'):
        return False 
    if not tk.wait_for_center('save_as_dialog_filetype'):
        return False 
    if not tk.wait_for_center('save_as_dialog_save_default'):
        return False 
    if not tk.wait_and_click('save_as_dialog_cancel_enabled'):
        return False 
    return True
    
def check_file_save_as_shows_network_save_as_dialog():
    do_menu_choice('bs2_file_menu', 'bs2_file_save_as_item')
    return check_save_as_dialog_complete()

def check_icon_save_as_shows_network_save_as_dialog():
    do_icon_choice('bs2_icon_save_as_enabled')
    return check_save_as_dialog_complete()
    
def check_network_library_entry(menu_item, relevant_part):
    harmless_click_to_focus()
    if not tk.wait_and_click('bs2_file_menu'):
        return False
    if not tk.wait_and_click('bs2_file_library_item'):
        return False
    if not tk.wait_and_click(menu_item):
        return False
    if not tk.wait_and_click('notes_ok_button_enabled'):
        return False
    if not tk.wait_for_center(relevant_part):
        return False
    return True

def check_recent_network_entry(menu_item, tool_tip, relevant_part):
    harmless_click_to_focus()
    if not tk.wait_and_click('bs2_file_menu'):
        return False
    if not tk.wait_and_click('bs2_file_recent_item'):
        return False
    if not tk.wait_and_hover(menu_item):
        return False
    if not tk.wait_for_center(tool_tip):
        return False
    if not tk.wait_and_click(menu_item):
        return False
    if not tk.wait_and_click('notes_ok_button_enabled'):
        return False
    if not tk.wait_for_center(relevant_part):
        return False
    return True

def check_synapse_is_drawn_correctly(weight, model, drawn_synapse):
    select_weight_combobox(int(weight))
    select_model_combobox(int(model))
    tk.drag_from_to(30, 115, 95, 115, 0.2)
    harmless_click_to_focus()
    if not tk.wait_and_hover(drawn_synapse):
        return False  
    pyautogui.hotkey('control', 'Z')
    return True

def select_module_combobox(page, option):
    ys = [92, 110, 130, 150, 170, 190, 210, 230, 250, 270, 290,
          310, 330, 350, 370, 390, 410]
    tk.click([1411, 67])
    time.sleep(0.1)
    # go to first page
    for i in range(2):
        tk.click([1460, 103])
    # go to correct page
    for i in range(page):
        tk.click([1460, 420])
    tk.click([1411, ys[option]])
    
def select_weight_combobox(option):
    ys = [92, 110, 130, 150, 170, 190, 210, 230, 250, 270, 290]
    tk.click([1630, 67])
    time.sleep(0.1)
    tk.click([1630, ys[option]])
    
def select_model_combobox(option):
    ys = [92, 110, 130, 150]
    tk.click([1756, 67])
    time.sleep(0.1)
    tk.click([1756, ys[option]])

def insert_module(page, index):
    select_module_combobox(int(page), int(index))
    pyautogui.moveTo([95, 185])
    pyautogui.click([95, 185])
    harmless_click_to_focus()

def remove_module():
    pyautogui.rightClick([70, 150])
    return tk.wait_and_click('module_delete')
    
def check_module_is_inserted_correctly(page, index, drawn_module):
    result = True
    insert_module(page, index)
    if not tk.wait_and_hover(drawn_module):
        result = False  
    remove_module()
    return result
   
def check_module_is_inserted_correctly_with_warning(page, index, drawn_module, warning):
    result = check_module_is_inserted_correctly(page, index, drawn_module)
    if not tk.wait_and_click(warning):
        result = False  
    pyautogui.press('escape')
    pyautogui.press('escape')
    remove_module()
    return result
   
def check_does_module_resize_and_undo_correctly(page, index, x_start, y_start, x_end, y_end, resized_module, drawn_module):
    result = True
    insert_module(page, index)
    tk.drag_from_to(x_start, y_start, x_end, y_end, 1)
    harmless_click_to_focus()
    # time.sleep(3)
    if not tk.wait_and_hover(resized_module):
        result = False  
    do_menu_choice('bs2_edit_menu', 'bs2_edit_undo_item_enabled')
    harmless_click_to_focus()
    if not tk.wait_and_hover(drawn_module):
        result = False  
    remove_module()
    return result