
#include "main.h"

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
	printf("Press enter\r\n");
	std::cin.get();

	printf("Running tests...run with debugger to verify\r\n");
	start_time = my_clock::now();

	random_device rd;
	int sizeOfNeuron = sizeof(NeuronBase);
	int sizeOfVector = sizeof(vector<int>);
	int neuronCount = 100;// '000;//'000; // million neurons
	int synapsesPerNeuron = 10;

	//memory leak check
	NeuronArrayBase* neuronArray = NULL;
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
#if _DEBUG
	neuronCount = 100'000;
	synapsesPerNeuron = 100;
#else
	neuronCount = 1'000'000;
	synapsesPerNeuron = 100;
#endif // DEBUG

	neuronArray = new NeuronArrayBase();
	neuronArray->Initialize(neuronCount);
	neuronArray->SetThreadCount(64);

	outputElapsedTime(to_string(neuronCount)+" neurons allocated");
	string s = "allocating synapses using " + to_string(neuronArray->GetThreadCount()) + " threads. Each dot is "+to_string(100'000*synapsesPerNeuron)+" synapses \n";
	cout << s;

	std::atomic<long long> count = 0;
	parallel_for(0, neuronArray->GetThreadCount(), [&](int value) {
		int start, end;
		neuronArray->GetBounds1(value, start, end);
		for (int i = start; i < end; i++)
		{
			NeuronBase* n = neuronArray->GetNeuron(i);
			for (int j = 0; j < synapsesPerNeuron; j++)
			{
				//int target = i + rd() % 1000 - 500;
				int target = i + j;
				if (target >= neuronArray->GetArraySize()) target -= neuronArray->GetArraySize();
				if (target < 0) target += neuronArray->GetArraySize();
				n->AddSynapse(neuronArray->GetNeuron(target), 1, false, true);
			}
			count++;
			if (count % 100'000 == 0) printf(".");
		}
		});

	s = "\n"+ to_string((long long)neuronCount * (long long)synapsesPerNeuron) + " random synapses requested  Actual: " + to_string(neuronArray->GetTotalSynapseCount()) + " synapses  alocated";
	outputElapsedTime(s);


	for (int i = 0; i < neuronCount / 100; i++)
	{
		int target = rd() % neuronArray->GetArraySize();
		//int target = i;
		neuronArray->GetNeuron(target)->SetCurrentCharge(1);
		neuronArray->GetNeuron(target)->SetLastCharge(1);
	}

	outputElapsedTime("firing loop Start");
	for (int i = 0; i < 10; i++)
	{
		int count = 0;
		//for (int j = 0; j < 10; j++)
		{
			neuronArray->Fire();
			count += neuronArray->GetFiredCount();
		}
		string s = "fired: " + to_string(count) + " neurons,  ";
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



