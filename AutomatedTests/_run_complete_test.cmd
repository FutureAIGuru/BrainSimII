@echo off
rem This automated test requires installation of the following:
rem - Python 3.8 or later
rem - robotframework (for the nicely formatted output)
rem - pyautogui (for the automated testing)
rem - pyscreeze (for the ImageNotFoundException)
rem - opencv-python (for the confidence setting on locate functions)
rem
rem tags often used in tests are:
rem - Complete (to signify tests that are considered to work correctly)
rem - Wip (to signify Work in Progress)
rem - Faulty (to indicate a test that still has a bug that needs fixing)
rem
rem robot --exclude Faulty .
robot --include Complete .
pause