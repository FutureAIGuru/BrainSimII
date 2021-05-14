#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  
# the testtoolkit requires the following python3 packages to be installed:
# - robotframework (for the nicely formatted output)
# - pyautogui (for the automated testing)
# - pyscreeze (for the ImageNotFoundException)
# - opencv-python (for the confidence setting on locate functions)

import time
import pyautogui
from pyscreeze import ImageNotFoundException
from robot.libraries.BuiltIn import BuiltIn


def _scr(fragname):
    return '.\\screenfrags\\' + fragname + '.png'

def screen_size_correct():
    screenWidth, screenHeight = pyautogui.size()
    if screenWidth != 1920 or screenHeight != 1080:
        return False
    return True

def locate_center(screen_frag):
    try:
        center = pyautogui.locateCenterOnScreen(_scr(screen_frag), confidence=0.99)
    except ImageNotFoundException as e:
        return None
    return center
   
def wait_for_center(screen_frag):
    max = 50
    location = None
    while max > 0 and location is None:
        location = locate_center(screen_frag)
        time.sleep(0.01)
        max -= 1
    if location is None:
        print('wait_for_center()', screen_frag, 'not found')
        return False
    return True

def click(screen_location):
    if screen_location is None:
        print('click()', screen_location, 'not set')
        return False
    pyautogui.click(screen_location)
    return True
    
def wait_and_click(screen_frag):
    if not wait_for_center(screen_frag):
        print('wait_and_click()', screen_frag, 'not found')
        return False
    pyautogui.click(locate_center(screen_frag))
    return True

def wait_and_hover(screen_frag):
    if not wait_for_center(screen_frag):
        print('wait_and_hover()', screen_frag, 'not found')
        return False
    pyautogui.moveTo(locate_center(screen_frag))
    return True

def wait_and_check_tooltip(screen_frag, tool_tip):
    if not wait_for_center(screen_frag):
        print('wait_and_hover()', screen_frag, 'not found')
        return False
    pyautogui.moveTo(locate_center(screen_frag))
    return wait_for_center(tool_tip)
    
def wait_and_doubleclick(screen_frag):
    if not wait_for_center(screen_frag): 
        print('wait_and_doubleclick()', screen_frag, 'not found')
        return False
    pyautogui.doubleClick(locate_center(screen_frag))    
    return True
