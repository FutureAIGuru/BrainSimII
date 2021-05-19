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
	[Tags]          Complete
	[Template]		Check Synapse Is Drawn Correctly
	0		3		binary_1.0
	1		3		binary_0.9
	2		3		binary_0.5
	3		3		binary_0.334
	4		3		binary_0.25
	5		3		binary_0.20
	6		3		binary_0.167
	7		3		binary_0.10
	8		3		binary_0.00
    9		3		binary_-1

Are Modules Inserted Correctly?
	[Tags]          Wip
	[Template]		Check Module Is Inserted Correctly
	0		0		module_2dmodel
	0		1		module_2dsim
	0		2		module_2dsmell
	0		3		module_2dtouch
	0		4		module_2dvision
	0		5		module_3dsim
	0		6		module_arm
	0		7		module_audible
	0		8		module_behavior
	#0		9		module_boundary  # still crashes the program
	0		10		module_boundary1
	0		11		module_camera
	#0		12		module_chain  # still crashes the program
	0		13		module_chaincounter
	0		14		module_colorcomponent
	0		15		module_coloridentifier
	0		16		module_command
	1		0		module_event
	1		1		module_fireoldest
	1		2		module_gotodest
	#1		3		module_graph  # still crashes the program
	1		4		module_grayscale
	1		5		module_hearwords
	1		6		module_imagefile
	1		7		module_kbdebug
	1		8		module_learning
	1		9		module_life
	#1		10		module_motor  # still crashes the program
	1		11		module_move
	1		12		module_moveobject
	1		13		module_navigate
	1		14		module_null
	1		15		module_patterngenerator
	1		16		module_ratedecoder
	2		2		module_ratedecoder2
	2		3		module_realitymodel
	2		4		module_shorttermmemory
	2		5		module_speakphonemes
	2		6		module_speakphonemes2
	2		7		module_speakwords
	2		8		module_speechin
	2		9		module_speechout
	2		10		module_strokecenter
	2		11		module_strokefinder
	2		12		module_turn
	2		13		module_uks
	2		14		module_uks2
	2		15		module_uksn
    #2		16		module_words  # we don't check words because it open a file selector
	