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

Test Setup			Start Brain Simulator With New Network
Test Teardown		Stop Brain Simulator

*** Test Cases ***

Does File New Show New Network Dialog?
	[Tags]              Complete	
	Check File New Shows New Network Dialog

Does Icon New Show New Network Dialog?
	[Tags]              Complete
	Check Icon New Shows New Network Dialog

Does File Open Show Network Load Dialog?
	[Tags]              Complete
	Check File Open Shows Network Load Dialog

Does Icon Open Show Network Load Dialog?
	[Tags]              Complete
	Check Icon Open Shows Network Load Dialog

Does File Save As Show Network Save As Dialog?
	[Tags]              Complete
	Check File Save As Shows Network Save As Dialog

Does Icon Save As Show Network Save As Dialog?
	[Tags]              Complete
	Check Icon Save As Shows Network Save As Dialog

Do Library Networks Load?
	[Tags]              Complete
	[Template]          Check Network Library Entry
    Network_BasicNeurons	  	fragment_basicneurons
    Network_HebbianSynapses   	fragment_hebbiansynapses
    Network_SimVision         	fragment_simvision
    Network_Imagination       	fragment_imagination
    Network_BabyTalk          	fragment_babytalk
    Network_Maze              	fragment_maze
    Network_SpeechTest        	fragment_speechtests
    Network_NeuralGraph       	fragment_neuralgraph
    # Network_Sallie            	fragment_sallie
    Network_ObjectMotion      	fragment_objectmotion
    Network_CameraTest        	fragment_cameratest
    Network_3DSim             	fragment_3dsim
    
Do Recent Networks Load?
	# This test assumes "Do Library Networks Load?" has been executed succesfully.
	[Tags]              Complete
	[Template]          Check Recent Network Entry
    Network_BasicNeurons	  	tool_tip_basicneurons			fragment_basicneurons
    Network_HebbianSynapses   	tool_tip_hebbiansynapses		fragment_hebbiansynapses
    Network_SimVision         	tool_tip_simvision				fragment_simvision
    Network_Imagination       	tool_tip_imagination			fragment_imagination
    Network_BabyTalk          	tool_tip_babytalk				fragment_babytalk
    Network_Maze              	tool_tip_maze					fragment_maze
    Network_SpeechTest        	tool_tip_speechtest 			fragment_speechtests
    Network_NeuralGraph       	tool_tip_neuralgraph			fragment_neuralgraph
    # Network_Sallie            	tool_tip_sallie					fragment_sallie
    Network_ObjectMotion      	tool_tip_objectmotion			fragment_objectmotion
    Network_CameraTest        	tool_tip_cameratest				fragment_cameratest
    Network_3DSim             	tool_tip_3dsim					fragment_3dsim
