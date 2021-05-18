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
	[Tags]          Complete
	[Template]		Check Synapse Is Drawn Correctly
	0		0		fixed_1.0
	1		0		fixed_0.9
	2		0		fixed_0.5
	3		0		fixed_0.334
	4		0		fixed_0.25
	5		0		fixed_0.20
	6		0		fixed_0.167
	7		0		fixed_0.10
	8		0		fixed_0.00
    9		0		fixed_-1

Are Binary Synapses Drawn Correctly?
	[Tags]          Complete
	[Template]		Check Synapse Is Drawn Correctly
	0		1		binary_1.0
	1		1		binary_0.9
	2		1		binary_0.5
	3		1		binary_0.334
	4		1		binary_0.25
	5		1		binary_0.20
	6		1		binary_0.167
	7		1		binary_0.10
	8		1		binary_0.00
    9		1		binary_-1

Are Hebbian1 Synapses Drawn Correctly?
	[Tags]          Complete
	[Template]		Check Synapse Is Drawn Correctly
	0		2		binary_1.0
	1		2		binary_0.9
	2		2		binary_0.5
	3		2		binary_0.334
	4		2		binary_0.25
	5		2		binary_0.20
	6		2		binary_0.167
	7		2		binary_0.10
	8		2		binary_0.00
    9		2		binary_-1

Are Hebbian2 Synapses Drawn Correctly?
	[Tags]          Wip
	[Template]		Check Synapse Is Drawn Correctly
	0		3		binary_1.0
	1		3		binary_0.9
	#2		3		binary_0.5
	#3		3		binary_0.334
	#4		3		binary_0.25
	#5		3		binary_0.20
	#6		3		binary_0.167
	#7		3		binary_0.10
	#8		3		binary_0.00
    #9		3		binary_-1
