
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
	printf("hello world\r\n");
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
		const char* newLabel = "Fred";
		NeuronBase *n = neuronArray->GetNeuron(0);
		char* test = n->GetLabel();
		n->SetLabel(newLabel);
		test = n->GetLabel();
		n->SetLabel("George");
		test = n->GetLabel();
		n->AddSynapse(neuronArray->GetNeuron(1), .53f, true,true);
		n->AddSynapse(neuronArray->GetNeuron(2), .45f, true,true);
		n->AddSynapse(neuronArray->GetNeuron(3), .34f, true);
		std::vector<SynapseBase> test2 = n->GetSynapses();
		test2 = neuronArray->GetNeuron(1)->GetSynapsesFrom();

		neuronArray2 = new NeuronArrayBase();
		neuronArray2->Initialize(neuronCount);
		newLabel = "Harry";
		n = neuronArray2->GetNeuron(0);
		test = n->GetLabel();
		n->SetLabel(newLabel);
		test = n->GetLabel();
		n->SetLabel("Potter");
		test = n->GetLabel();
		n->SetLabel("");
		test = n->GetLabel();
		n = neuronArray->GetNeuron(0);
		test = n->GetLabel();


		parallel_for(0, neuronCount, [&](int value) {
		//for (int value = 0; value < neuronCount; value++){
			NeuronBase* n = neuronArray->GetNeuron(value);
			int id = n->GetId();
			for (int j = 0; j < synapsesPerNeuron; j++)
			{
				n->AddSynapse(neuronArray->GetNeuron(j), 1,false,true);
			}
		}
		);

		neuronArray->Fire();
		neuronArray->Fire();
	}

	neuronCount = 1'000'000;
	synapsesPerNeuron = 1000;
	neuronArray = new NeuronArrayBase();
	neuronArray->Initialize(neuronCount);
	outputElapsedTime("neurons allocated");
	//	for (int i = 0; i < neuronCount; i++)
	parallel_for(0, neuronCount, [&](int value) {
		NeuronBase* n = neuronArray->GetNeuron(value);
		for (int j = 0; j < synapsesPerNeuron; j++)
		{
			//int target = value + rd() % 3000 - 1500;
			int target = value + j;
			if (target >= neuronArray->GetArraySize()) target -= neuronArray->GetArraySize();
			if (target < 0) target += neuronArray->GetArraySize();
			n->AddSynapse(neuronArray->GetNeuron(target), 1,false,true);
		}
		});

	outputElapsedTime("synapses allocated");


	for (int i = 0; i < 10'000; i++)
	{
		int target = rd() % neuronArray->GetArraySize();
		neuronArray->GetNeuron(target)->SetLastCharge(1);
	}

	outputElapsedTime("firing loop Start");
	neuronArray->SetThreadCount(64);
	for (int i = 0; i < 20; i++)
	{
		for (int j = 0; j < 10; j++)
			neuronArray->Fire();
		string s = "fired:" + to_string(neuronArray->GetFiredCount()) + " " + to_string(neuronArray->GetTotalSynapseCount()) + "s " + to_string(neuronArray->GetThreadCount()) + " t ";
		outputElapsedTime(s);
	}
	outputElapsedTime("Done");
	return 0;
}

void outputElapsedTime(string msg)
{
	cout << msg << " ";
	auto end_time = my_clock::now();
	auto diff = end_time - start_time;
	auto milliseconds = chrono::duration_cast<chrono::milliseconds>(diff);
	auto millisecond_count = milliseconds.count();
	cout << millisecond_count << '\n';

	start_time = my_clock::now();;
}

