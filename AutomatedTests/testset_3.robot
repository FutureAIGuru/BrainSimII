#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

*** Settings ***
Documentation		This testset runs with Brain Simulator II 
...					started with the shift key down, so no network is loaded.

Library   			testtoolkit.py
Library   			teststeps.py

Test Setup			Start Brain Simulator Without Network
Test Teardown		Stop Brain Simulator

*** Test Cases ***

Does File New Show New Network Dialog?
	[Tags]              Wip
	${Result}			Check File New Shows New Network Dialog
	Should Be True		${Result}

Does Icon New Show New Network Dialog?
	[Tags]              Wip
	${Result}			Check Icon New Shows New Network Dialog
	Should Be True		${Result}

Does File Open Show Network Load Dialog?
	[Tags]              Wip
	${Result}			Check File Open Shows Network Load Dialog
	Should Be True		${Result}

Does Icon Open Show Network Load Dialog?
	[Tags]              Wip
	${Result}			Check Icon Open Shows Network Load Dialog
	Should Be True		${Result}

Does File Save As Show Network Save As Dialog?
	[Tags]              Wip
	${Result}			Check File Save As Shows Network Save As Dialog
	Should Be True		${Result}

Does Icon Save As Show Network Save As Dialog?
	[Tags]              Wip
	${Result}			Check Icon Save As Shows Network Save As Dialog
	Should Be True		${Result}

Do Library Networks Load?
	[Tags]              Wip
	[Template]          Check Network Library Entry
    Library_BasicNeurons	  	fragment_basicneurons
    Library_HebbianSynapses   	fragment_hebbiansynapses
    Library_SimVision         	fragment_simvision
    Library_Imagination       	fragment_imagination
    Library_BabyTalk          	fragment_babytalk
    Library_Maze              	fragment_maze
    Library_SpeechTest        	fragment_speechtests
    Library_NeuralGraph       	fragment_neuralgraph
    Library_Sallie            	fragment_sallie
    Library_CameraTest        	fragment_cameratest
    Library_ObjectMotion      	fragment_objectmotion
    Library_3DSim             	fragment_3dsim
    