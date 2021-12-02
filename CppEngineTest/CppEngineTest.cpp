// CppEngineTest.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include "..\NeuronEngine\NeuronBase.h"
#include "..\NeuronEngine\NeuronArrayBase.h"


#include<execution>
#include <stddef.h>
#include <stdio.h>
#include <algorithm>
#include <ppl.h>
#include <chrono>
#include <random>
#include <vector>
#include <iostream>

using namespace NeuronEngine;
using namespace std::chrono;
using namespace std;
using namespace Concurrency;

void outputElapsedTime(string msg);


chrono::steady_clock::time_point start_time;
typedef chrono::high_resolution_clock my_clock;

using std::chrono::duration;
using std::chrono::duration_cast;
using std::chrono::high_resolution_clock;
using std::milli;
using std::random_device;
using std::sort;
using std::vector;



int main(int argc, char* argv[], char* envp[])
{


	printf("Running tests...run with debugger to verify\r\n");
	start_time = my_clock::now();

	random_device rd;
	int sizeOfNeuron = sizeof(NeuronBase);
	int sizeOfSynapse = sizeof(SynapseBase);
	int sizeOfVector = sizeof(vector<int>);
	long long synapsesPerNeuron = 10;
	NeuronArrayBase* neuronArray = NULL;

	/*
	//memory leak check
	NeuronArrayBase* neuronArray2 = NULL;
	for (int i = 0; i < 10; i++)
	{
		delete neuronArray;
		neuronArray = new NeuronArrayBase();
		neuronArray->Initialize(neuronCount);
		const wchar_t* newLabel(L"Fred");
		NeuronBase* n = neuronArray->GetNeuron(0);
		wchar_t* test = n->GetLabel();
		n->SetLabel(newLabel);
		test = n->GetLabel();
		n->SetLabel(L"George");
		test = n->GetLabel();
		n->AddSynapse(neuronArray->GetNeuron(1), .53f, true, true);
		n->AddSynapse(neuronArray->GetNeuron(2), .45f, true, true);
		n->AddSynapse(neuronArray->GetNeuron(3), .34f, true);
		std::vector<SynapseBase> test2 = n->GetSynapses();
		test2 = neuronArray->GetNeuron(1)->GetSynapsesFrom();

		neuronArray2 = new NeuronArrayBase();
		neuronArray2->Initialize(neuronCount);
		newLabel = L"Harry";
		n = neuronArray2->GetNeuron(0);
		test = n->GetLabel();
		n->SetLabel(newLabel);
		test = n->GetLabel();
		n->SetLabel(L"Potter");
		test = n->GetLabel();
		n->SetLabel(L"");
		test = n->GetLabel();
		n = neuronArray->GetNeuron(0);
		test = n->GetLabel();
		n->SetCurrentCharge(1);


		parallel_for(0, neuronCount, [&](int value) {
			//for (int value = 0; value < neuronCount; value++){
			NeuronBase* n = neuronArray->GetNeuron(value);
			int id = n->GetId();
			for (int j = 0; j < synapsesPerNeuron; j++)
			{
				n->AddSynapse(neuronArray->GetNeuron(j), 1, false, true);
			}
			}
		);
		n = neuronArray->GetNeuron(0);
		std::vector<SynapseBase> tempVec = n->GetSynapses();

		int count = neuronArray->GetFiredCount();
		neuronArray->Fire();
		count = neuronArray->GetFiredCount();
		neuronArray->Fire();
		count = neuronArray->GetFiredCount();

	}
	*/

	long long neuronCount;
	int threadCount;
	long long synapsesPerDot;
	int cyclesPerFiring;
#if _DEBUG
	neuronCount = 10'240;
	synapsesPerNeuron = 100;
	threadCount = 1;
	synapsesPerDot = 1000;
	cyclesPerFiring = 10;
#else
	neuronCount = 10'000'000;
	synapsesPerNeuron = 100;
	threadCount = 124;
	synapsesPerDot = 1'000'000;
	cyclesPerFiring = 1;
#endif // DEBUG

	neuronArray = new NeuronArrayBase();
	neuronArray->Initialize(neuronCount);
	neuronArray->SetThreadCount(threadCount);

	outputElapsedTime(to_string(neuronCount) + " neurons allocated");

	string s = "Using " + to_string(neuronArray->GetThreadCount()) + " threads. \n";
	cout << s;

	s = "Allocating " + to_string(neuronCount * synapsesPerNeuron) + " synapses . Each dot is " + to_string(synapsesPerDot) + " synapses \n";
	cout << s;

	std::atomic<long long> count = 0;
	parallel_for(0, neuronArray->GetThreadCount(), [&](int value) {
		int start, end;
		neuronArray->GetBounds(value, start, end);
		for (int i = start; i < end; i++)
		{
			NeuronBase* n = neuronArray->GetNeuron(i);
			//n->AddSynapse(n, 1, false, true);
			for (int j = 1; j < synapsesPerNeuron; j++)
			{
				int target = i + rd() % 1'000;
				//int target = j;
				if (target >= neuronArray->GetArraySize()) target -= neuronArray->GetArraySize();
				if (target < 0) target += neuronArray->GetArraySize();
				n->AddSynapse(neuronArray->GetNeuron(target), 1, SynapseBase::modelType::Fixed, true);
				count++;
				if (count % synapsesPerDot == 0) printf(".");
			}
		}
		});

	s = "\n" + to_string((long long)neuronCount * (long long)synapsesPerNeuron) + " random synapses requested  Actual: " + to_string(neuronArray->GetTotalSynapseCount()) + " synapses  alocated";

	outputElapsedTime(s);

	//select some neurons to be firing (all the time based on synapses
	for (int i = 0; i < neuronCount / cyclesPerFiring; i++)
	{
		int target = rd() % neuronArray->GetArraySize();
		//int target = i;
		neuronArray->GetNeuron(target)->SetCurrentCharge(1);
		neuronArray->GetNeuron(target)->SetLastCharge(1);
	}

	outputElapsedTime("firing loop Start");

	//Run the firing loop
	for (int i = 0; i < 20; i++)
	{
		int count = 0;
		neuronArray->Fire();
		string s = "fired: " + to_string(neuronArray->GetFiredCount()) + " neurons,  ";
		outputElapsedTime(s);
	}
	outputElapsedTime("Done");

	printf("Press enter\r\n");
	std::cin.get();
	return 0;
}

void outputElapsedTime(string msg)
{
	cout << msg << " ";
	auto end_time = my_clock::now();
	auto diff = end_time - start_time;
	auto milliseconds = chrono::duration_cast<chrono::milliseconds>(diff);
	auto millisecond_count = milliseconds.count();
	cout << millisecond_count << "ms\n";

	start_time = my_clock::now();;
}



