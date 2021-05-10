#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

import time
import pyautogui
from pyscreeze import ImageNotFoundException

import testtoolkit as tk     # the very basic routines we use from PyAutoGUI again and again.

def check_test_requirements():
    # This function checks the requirements the test framework depends on.
    if not tk.screen_size_correct():
        return False
    if not tk.wait_for_center('brainsim_start'):
        return False
    if not tk.wait_for_center('neuronserver_start'):
        return False
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

def start_brain_simulator_without_network():
    if tk.wait_and_click('brainsim_start') == False:
        return False
    if tk.wait_for_center('brainsim_started') == False:
        return False
    pyautogui.keyDown('shift')
    if not tk.wait_for_center('brainsim_splash'):
        return False
    timeout = 0
    while tk.locate_center('brainsim_splash') is None and timeout < 20:
        time.sleep(1)
        timeout += 1
    if tk.wait_for_center('brainsim_splash') is None:
        return False
    pyautogui.keyUp('shift')
    return True

def stop_brain_simulator():
    if not tk.wait_and_click('brainsim_title'):
        return False
    if not tk.wait_and_click('close_icon'):
        return False
    if not tk.wait_for_center('brainsim_start'):
        return False
    return True

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
    if not tk.wait_and_click('brainsim_title'):
        return False
    if not tk.wait_and_click('bs2_file_menu'):
        return False
    if not tk.wait_for_center('bs2_file_new_item'):
        return False
    if not tk.wait_for_center('bs2_file_open_item'):
        return False
    if not tk.wait_for_center('bs2_file_save_item_disabled'):
        return False
    if not tk.wait_for_center('bs2_file_saveas_item'):
        return False
    if not tk.wait_for_center('bs2_file_properties_item'):
        return False
    if not tk.wait_for_center('bs2_file_recent_item'):
        return False
    if not tk.wait_for_center('bs2_file_library_item'):
        return False
    if not tk.wait_for_center('bs2_file_exit_item'):
        return False
    return True

def check_edit_menu():
    if not tk.wait_and_click('brainsim_title'):
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
    if not tk.wait_and_click('brainsim_title'):
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
    if not tk.wait_and_click('brainsim_title'):
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
    if not tk.wait_and_click('brainsim_title'):
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

