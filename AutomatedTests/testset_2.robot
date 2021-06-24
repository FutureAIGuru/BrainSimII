#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

*** Settings ***
Documentation		This testset runs with Brain Simulator II 
...					started with the shift key down, so no network is loaded.

Library   			testtoolkit.py
Library   			teststeps.py

Resource			keywords.resource

Suite Setup			Start Brain Simulator With New Network
Suite Teardown		Stop Brain Simulator

*** Test Cases ***

Is BrainSim File Menu Showing?
	[Tags]              Complete
	Check File Menu
	
Is BrainSim Edit Menu Showing?
	[Tags]              Complete
	Check Edit Menu
	
Is BrainSim Neuron Engine Menu Showing?
	[Tags]              Complete
	Check Engine Menu
	
Is BrainSim View Menu Showing?
	[Tags]              Complete
	Check View Menu
	
Is BrainSim Help Menu Showing?
	[Tags]              Complete
	Check Help Menu
			
Is BrainSim Icon Bar Showing?
	[Tags]              Complete
	Check Icon Bar

Are Icon Tooltips Showing?
	[Tags]              Complete
	Check Icon Tooltips

Are Icon Bar Checkboxes Showing?
	[Tags]              Complete
	Check Icon Checkboxes

Is Add Module Combobox Showing?
	[Tags]              Complete
	Check Add Module Combobox

Is Synapse Weight Combobox Showing?
	[Tags]              Complete
	Check Synapse Weight Combobox

Is Synapse Model Combobox Showing?
	[Tags]              Complete
	Check Synapse Model Combobox
