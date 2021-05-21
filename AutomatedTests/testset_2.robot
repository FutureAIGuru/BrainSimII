#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

*** Settings ***
Documentation		This testset runs with Brain Simulator II 
...					started with the shift key down, so no network is loaded.

Library   			testtoolkit.py
Library   			teststeps.py

Suite Setup			Start Brain Simulator Without Network
Suite Teardown		Stop Brain Simulator

*** Test Cases ***

Is BrainSim File Menu Showing?
	[Tags]              Wip
	${Result}    		Check File Menu
	Should Be True		${Result}
	
Is BrainSim Edit Menu Showing?
	[Tags]              Wip
	${Result}    		Check Edit Menu
	Should Be True		${Result}
	
Is BrainSim Neuron Engine Menu Showing?
	[Tags]              Wip
	${Result}    		Check Engine Menu
	Should Be True		${Result}
	
Is BrainSim View Menu Showing?
	[Tags]              Wip
	${Result}    		Check View Menu
	Should Be True		${Result}
	
Is BrainSim Help Menu Showing?
	[Tags]              Wip
	${Result}    		Check Help Menu
	Should Be True		${Result}
			
Is BrainSim Icon Bar Showing?
	[Tags]              Wip
	${Result}			Check Icon Bar
	Should Be True		${Result}

Are Icon Tooltips Showing?
	[Tags]              Wip
	${Result}			Check Icon Tooltips
	Should Be True		${Result}

Are Icon Bar Checkboxes Showing?
	[Tags]              Wip
	${Result}			Check Icon Checkboxes
	Should Be True		${Result}

Is Add Module Combobox Showing?
	[Tags]              Wip
	${Result}			Check Add Module Combobox
	Should Be True		${Result}

Is Synapse Weight Combobox Showing?
	[Tags]              Wip
	${Result}			Check Synapse Weight Combobox
	Should Be True		${Result}

Is Synapse Model Combobox Showing?
	[Tags]              Wip
	${Result}			Check Synapse Model Combobox
	Should Be True		${Result}


