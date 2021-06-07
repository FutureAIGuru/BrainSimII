#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

*** Settings ***
Documentation		This testset runs with Brain Simulator II 
...					started with a new network.

Library   			testtoolkit.py
Library   			teststeps.py

Test Setup			Start Brain Simulator With New Network
Test Teardown		Stop Brain Simulator

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
	[Tags]          Complete
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
	0		10		module_boundary1
	## NOT DONE Because Camera Module seems to hang the test
	##0		11		module_camera
	0		12		module_chain
	0		13		module_chaincounter
	0		14		module_colorcomponent
	0		15		module_command
	0		16   	module_event
	1		0		module_fireoldest
	1		1		module_gotodest
	1		2		module_graph
	1		3		module_grayscale
	1		4		module_hearwords
	1		5		module_imagefile
	1		6		module_kbdebug
	1		7		module_learning
	1		8		module_life
	1		9		module_motor
	1		10		module_move
	1		11		module_moveobject
	1		12		module_navigate
	1		13		module_null
	1		14		module_patterngenerator
			15		module_patternrecognizer
	1		16      module_ratedecoder2
	2		3		module_realitymodel
	2		4		module_shorttermmemory
	2		5		module_speakphonemes
	2		6		module_speakphonemes2
	2		7	 	module_speakwords
	2		8		module_speechin
	2		9		module_speechout
	2		10		module_strokecenter
	2		11		module_strokefinder
	2		12		module_turn
	2		13		module_uks
	2		14		module_uks2
	2		15		module_uksn
	
Are Modules Inserted Correctly With Warning?
	[Tags]          Complete
	[Template]		Check Module Is Inserted Correctly With Warning
	0		9		module_boundary  		requires_image_file
    2		16		module_words  			requires_file
	
Check Do Modules Resize And Undo Correctly?
	[Tags]          Complete
	[Template]		Check Does Module Resize And Undo Correctly
	0		0		${175}	${262}	${485}	${567}	sized_module_2dmodel			module_2dmodel
	0		1		${175}	${262}	${485}	${567}	sized_module_2dsim				module_2dsim
	0		2		${300}	${200}	${485}	${567}	sized_module_2dsmell			module_2dsmell
	0		3		${175}	${262}	${485}	${567}	sized_module_2dtouch			module_2dtouch
	0		4		${175}	${262}	${485}	${567}	sized_module_2dvision			module_2dvision
	0		5		${175}	${262}	${485}	${567}	sized_module_3dsim				module_3dsim
	0		6		${300}	${200}	${485}	${567}	sized_module_arm				module_arm
	0		7		${175}	${262}	${485}	${567}	sized_module_audible			module_audible
	0		8		${175}	${200}	${485}	${567}	sized_module_behavior			module_behavior
	0		10		${175}	${262}	${485}	${567}	sized_module_boundary1			module_boundary1
	## NOT DONE Because Camera Module seems to hang the test
	##0		11		${175}	${262}	${290}	${380}	sized_module_camera				module_camera
	0		12		${112}	${198}	${485}	${567}	sized_module_chain				module_chain
	0		13		${234}	${318}	${485}	${567}	sized_module_chaincounter		module_chaincounter
	0		14		${112}	${378}	${485}	${567}	sized_module_colorcomponent		module_colorcomponent
	0		15		${175}	${262}	${485}	${567}	sized_module_command			module_command
	0		16   	${175}	${262}	${485}	${567}	sized_module_event				module_event
	1		0		${175}	${262}	${485}	${567}	sized_module_fireoldest			module_fireoldest
	1		1		${238}	${262}	${485}	${567}	sized_module_gotodest			module_gotodest
	## NOT DONE Because of fixed, large size...
	##1		2	    ${175}	${262}	${485}	${567}	sized_module_graph				module_graph
	1		3		${175}	${262}	${485}	${567}	sized_module_grayscale			module_grayscale
	1		4		${175}	${262}	${485}	${567}	sized_module_hearwords			module_hearwords
	1		5		${175}	${262}	${485}	${567}	sized_module_imagefile			module_imagefile
	1		6		${175}	${262}	${485}	${567}	sized_module_kbdebug			module_kbdebug
	1		7		${175}	${262}	${485}	${567}	sized_module_learning			module_learning
	1		8		${175}	${262}	${485}	${567}	sized_module_life				module_life
	1		9		${175}	${262}	${485}	${567}	sized_module_motor				module_motor
	1		10		${234}	${438}	${485}	${567}	sized_module_move				module_move
	1		11		${175}	${262}	${485}	${567}	sized_module_moveobject			module_moveobject
	1		12		${175}	${262}	${485}	${567}	sized_module_navigate			module_navigate
	1		13		${175}	${262}	${485}	${567}	sized_module_null				module_null
	1		14		${175}	${262}	${485}	${567}	sized_module_patterngenerator	module_patterngenerator
			15		${238}	${324}	${485}	${567}	sized_module_patternrecognizer	module_patternrecognizer
	1		16      ${234}	${262}	${485}	${567}	sized_module_ratedecoder2		module_ratedecoder2
	2		3		${175}	${262}	${485}	${567}	sized_module_realitymodel		module_realitymodel
	2		4		${175}	${262}	${485}	${567}	sized_module_shorttermmemory	module_shorttermmemory
	2		5		${175}	${262}	${485}	${567}	sized_module_speakphonemes		module_speakphonemes
	2		6		${175}	${262}	${485}	${567}	sized_module_speakphonemes2		module_speakphonemes2
	2		7	 	${175}	${262}	${485}	${567}	sized_module_speakwords			module_speakwords
	2		8		${175}	${262}	${485}	${567}	sized_module_speechin			module_speechin
	2		9		${175}	${262}	${485}	${567}	sized_module_speechout			module_speechout
	2		10		${175}	${262}	${485}	${567}	sized_module_strokecenter		module_strokecenter
	2		11		${175}	${262}	${485}	${567}	sized_module_strokefinder		module_strokefinder
	2		12		${360}	${204}	${485}	${567}	sized_module_turn				module_turn
	2		13		${175}	${262}	${485}	${567}	sized_module_uks				module_uks
	2		14		${175}	${262}	${485}	${567}	sized_module_uks2				module_uks2
	2		15		${175}	${262}	${485}	${567}	sized_module_uksn				module_uksn
