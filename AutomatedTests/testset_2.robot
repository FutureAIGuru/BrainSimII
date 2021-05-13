#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

*** Settings ***
Documentation		This testset runs with Brain Simulator II started with the shift key down, so no network is loaded.

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
			
Is BrainSim Icon Bar Complete?
	[Tags]				Wip
	${Result}			Check Icon Bar
	Should Be True		${Result}

Are Icon Tooltips Working?
	[Tags]				Wip
	${Result}			Check Icon Tooltips
	Should Be True		${Result}

Are Icon Bar Checkboxes Working?
	[Tags]				Wip
	${Result}			Check Icon Checkboxes
	Should Be True		${Result}

Does File New Show New Network Dialog?
	[Tags]				Wip
	${Result}			Check File New Shows New Network Dialog
	Should Be True		${Result}

Does Icon New Show New Network Dialog?
	[Tags]				Wip
	${Result}			Check Icon New Shows New Network Dialog
	Should Be True		${Result}

Does File Open Show Network Load Dialog?
	[Tags]				Wip
	${Result}			Check File Open Shows Network Load Dialog
	Should Be True		${Result}

Does Icon Open Show Network LoadDialog?
	[Tags]				Wip
	${Result}			Check Icon Open Shows Network Load Dialog
	Should Be True		${Result}
