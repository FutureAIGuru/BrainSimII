#
# Copyright (c) Andre Slabber. All rights reserved.  
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#  

*** Settings ***
Documentation		This testset runs with no setup or teardown whatsoever, basic tests for starting and stopping programs.

Resource			testkeywords.resource
Resource 			automatedtests.resource

Library   			testtoolkit.py
Library   			teststeps.py

*** Test Cases ***

Are Prerequisites Taken Care Of?
	[Tags]				Wip
	${Result}    		Check Test Requirements
	Should Be True		${Result}
	
Can We Start And Stop Brain Simulator II?
	[Tags]				Wip
	${Result}    		Can We Start And Stop Brain Simulator 2
	Should Be True		${Result}

Can We Start And Stop Neuron Server?
	[Tags]				Wip
	${Result}    		Can We Start And Stop Neuron Server
	Should Be True		${Result}

	