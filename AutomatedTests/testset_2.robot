#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

*** Settings ***
Documentation		This testset runs with Brain Simulator II started with the shift key down, so no network is loaded.

Resource			testkeywords.resource
Resource 			automatedtests.resource

Library   			testtoolkit.py
Library   			teststeps.py

Suite Setup			Start Brain Simulator Without Network
Suite Teardown		Stop Brain Simulator

*** Test Cases ***

Is BrainSim File Menu Complete?
	[Tags]				Wip
	${Result}    		Check File Menu
	Should Be True		${Result}
	
Is BrainSim Edit Menu Complete?
	[Tags]				Wip
	${Result}    		Check Edit Menu
	Should Be True		${Result}
	
Is BrainSim Neuron Engine Menu Complete?
	[Tags]				Wip
	${Result}    		Check Engine Menu
	Should Be True		${Result}
	
Is BrainSim View Menu Complete?
	[Tags]				Wip
	${Result}    		Check View Menu
	Should Be True		${Result}
	
Is BrainSim Help Menu Complete?
	[Tags]				Wip
	${Result}    		Check Help Menu
	Should Be True		${Result}
			



