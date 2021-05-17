#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

*** Settings ***
Documentation		This testset runs with Brain Simulator II 
...					started with a new network.

Library   			testtoolkit.py
Library   			teststeps.py

Suite Setup			Start Brain Simulator With New Network
Suite Teardown		Stop Brain Simulator

*** Test Cases ***

Are Fixed Synapses Drawn Correctly?
	[Tags]              Wip
	[Template]			Check Synapse Is Drawn Correctly
	weight_1			model_fixed			fixed_1.0
	weight_0.90			model_fixed		    fixed_0.9
	weight_0.50			model_fixed			fixed_0.5
	weight_0.334		model_fixed			fixed_0.334
	weight_0.25			model_fixed			fixed_0.25
	weight_0.20			model_fixed			fixed_0.20
	weight_0.167		model_fixed			fixed_0.167
	weight_0.10			model_fixed			fixed_0.10
	weight_0.00			model_fixed			fixed_0.00
    weight_-1 			model_fixed			fixed_-1

Are Binary Synapses Drawn Correctly?
	[Tags]              Wip
	[Template]			Check Synapse Is Drawn Correctly
	weight_1			model_binary		binary_1.0
	weight_0.90			model_binary		binary_0.9
	weight_0.50			model_binary		binary_0.5
	weight_0.334		model_binary		binary_0.334
	weight_0.25			model_binary		binary_0.25
	weight_0.20			model_binary		binary_0.20
	weight_0.167		model_binary		binary_0.167
	weight_0.10			model_binary		binary_0.10
	weight_0.00			model_binary		binary_0.00
    weight_-1 			model_binary		binary_-1

Are Hebbian1 Synapses Drawn Correctly?
	[Tags]              Wip
	[Template]			Check Synapse Is Drawn Correctly
	weight_1			model_hebbian1		binary_1.0
	weight_0.90			model_hebbian1		binary_0.9
	weight_0.50			model_hebbian1		binary_0.5
	weight_0.334		model_hebbian1		binary_0.334
	weight_0.25			model_hebbian1		binary_0.25
	weight_0.20			model_hebbian1		binary_0.20
	weight_0.167		model_hebbian1		binary_0.167
	weight_0.10			model_hebbian1		binary_0.10
	weight_0.00			model_hebbian1		binary_0.00
    weight_-1 			model_hebbian1		binary_-1

Are Hebbian2 Synapses Drawn Correctly?
	[Tags]              Wip
	[Template]			Check Synapse Is Drawn Correctly
	weight_1			model_hebbian2		binary_1.0
	weight_0.90			model_hebbian2		binary_0.9
	weight_0.50			model_hebbian2		binary_0.5
	weight_0.334		model_hebbian2		binary_0.334
	weight_0.25			model_hebbian2		binary_0.25
	weight_0.20			model_hebbian2		binary_0.20
	weight_0.167		model_hebbian2		binary_0.167
	weight_0.10			model_hebbian2		binary_0.10
	weight_0.00			model_hebbian2		binary_0.00
    weight_-1 			model_hebbian2		binary_-1
