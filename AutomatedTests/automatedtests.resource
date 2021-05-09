#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

*** Settings ***
Documentation		This testset runs with no setup or teardown whatsoever, basic tests for starting and stopping programs.

Resource			testkeywords.resource

Library   			testtoolkit.py
Library   			teststeps.py

*** Keywords ***

Can We Start And Stop Brain Simulator 2
	${Result1}    		Start Brain Simulator
	Should Be True		${Result1}
	${Result2}    		Stop Brain Simulator
	Should Be True		${Result2}
	
Can We Start And Stop Neuron Server
	${Result1}    		Start Neuron Server
	Should Be True		${Result1}
	${Result2}    		Stop Neuron Server
	Should Be True		${Result2}
