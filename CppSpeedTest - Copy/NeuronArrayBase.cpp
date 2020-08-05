#include "NeuronArrayBase.h"
#include <windows.h>
#include <ppl.h>
#include <iostream>
#include <random>

using namespace concurrency;
using namespace std;

namespace NeuronEngine
{
	NeuronArrayBase::NeuronArrayBase()
	{
	}
	NeuronArrayBase::~NeuronArrayBase()
	{
	}

	void NeuronArrayBase::Initialize(int theSize, NeuronBase::modelType t)
	{
		arraySize = theSize;
		neuronArray.reserve(arraySize);
		for (int i = 0; i < arraySize; i++)
		{
			NeuronBase n(i);
			neuronArray.push_back(n);
		}
	}
	long NeuronArrayBase::GetGeneration()
	{
		return generation;
	}
	NeuronBase* NeuronArrayBase::GetNeuron(int i)
	{
		return &neuronArray[i];
	}


	int NeuronArrayBase::GetArraySize()
	{
		return arraySize;
	}

	long NeuronArrayBase::GetTotalSynapseCount()
	{
		long count = 0;
		for (int i = 0; i < arraySize; i++)
			if (GetNeuron(i)->GetSynapses() != NULL)
			{
				count += (unsigned)GetNeuron(i)->GetSynapses()->size();
			}
		return count;;
	}

	void NeuronArrayBase::GetBounds1(int taskID, int& start, int& end)
	{
		int numberToProcess = arraySize / threadCount;
		int remainder = arraySize % threadCount;
		start = numberToProcess * taskID;
		end = start + numberToProcess;
		if (taskID < remainder)
		{
			start += taskID;
			end = start + numberToProcess + 1;
		}
		else
		{
			start += remainder;
			end += remainder;
		}
	}
	void NeuronArrayBase::Fire()
	{
		generation++;
		firedCount = 0;
		parallel_for(0, threadCount, [&](int value) {
			ProcessNeurons1(value);
			});
		parallel_for(0, threadCount, [&](int value) {
			ProcessNeurons2(value);
			});
	}
	int NeuronArrayBase::GetFiredCount()
	{
		return firedCount;
	}
	int NeuronArrayBase::GetThreadCount()
	{
		return threadCount;
	}
	void NeuronArrayBase::SetThreadCount(int i)
	{
		threadCount = i;
	}
	void NeuronArrayBase::ProcessNeurons1(int taskID)
	{
		int start, end;
		GetBounds1(taskID, start, end);
		for (int i = start; i < end; i++)
			if (GetNeuron(i)->Fire2NoQ(generation))
				firedCount++;
	}
	void NeuronArrayBase::ProcessNeurons2(int taskID)
	{
		int start, end;
		GetBounds1(taskID, start, end);
		for (int i = start; i < end; i++)
			GetNeuron(i)->Fire1NoQ();
	}


	//firingQueue.clear();
	//firingQueue.insert(firingQueue.end(), manualFire.begin(), manualFire.end());
	//manualFire.clear();
	//for (int i = 0; i < taskCount; i++)
	//{
	//	firingQueue.insert(firingQueue.end(), nextQueue[i].begin(), nextQueue[i].end());
	//	nextQueue[i].clear();
	//	firingQueue.insert(firingQueue.end(), firedQueue[i].begin(), firedQueue[i].end());
	//	firedQueue[i].clear();
	//	firingQueue.insert(firingQueue.end(), zeroQueue[i].begin(), zeroQueue[i].end());
	//	zeroQueue[i].clear();
	//} //spin off the paralel tasks to run the engine on the firing queue
	//firedCount = firingQueue.size();
	//parallel_for(0, taskCount, [&](int value) {
	//	ProcessFiringQueue(value);
	//	});
	//parallel_for(0, taskCount, [&](int value) {
	//	ProcessFiringQueue1(value);
	//	});


		//for (int i = 0; i < taskCount; i++)
		//{
		//	nextQueue.push_back(std::vector<int>());
		//	firedQueue.push_back(std::vector<int>());
		//	zeroQueue.push_back(std::vector<int>());
		//	nextQueue[i].reserve(queuesize / taskCount);
		//	firedQueue[i].reserve(queuesize / taskCount);
		//	zeroQueue[i].reserve(queuesize / taskCount);
		//}
	//void NeuronArrayBase::AddToFiringQueue(int id)
	//{
	//	//manualFire.push_back(id);
	//	NeuronBase* n = GetNeuron(id);
	//	n->currentCharge = 1;
	//	n->lastCharge = 1;
	//}
	//void NeuronArrayBase::ProcessFiringQueue(int id)
	//{ //determine the portion of the firing queue this thread should handle
	//	int taskID = id;
	//	int start, end;
	//	GetBounds(taskID, start, end);
	//	vector<int>& fired = firedQueue[taskID];
	//	//first-phase neuron processing
	//	for (int i = start; i < end; i++)
	//	{
	//		GetNeuron(firingQueue[i])->Fire2(fired);
	//	}
	//}

	//void NeuronArrayBase::ProcessFiringQueue1(int id)
	//{
	//	int taskID = id;
	//	int start, end;
	//	GetBounds(taskID, start, end);
	//	vector<int>& next = nextQueue[taskID];
	//	vector<int>& zero = zeroQueue[taskID];
	//	for (int i = start; i < end; i++)
	//	{
	//		GetNeuron(firingQueue[i])->Fire1(this->neuronArray, next, zero);
	//	}
	//}

	//void NeuronArrayBase::GetBounds(int taskID, int& start, int& end)
	//{
	//	int numberToProcess = firingQueue.size() / taskCount;
	//	int remainder = firingQueue.size() % taskCount;
	//	start = numberToProcess * taskID;
	//	end = start + numberToProcess;
	//	if (taskID < remainder)
	//	{
	//		start += taskID;
	//		end = start + numberToProcess + 1;
	//	}
	//	else
	//	{
	//		start += remainder;
	//		end += remainder;
	//	}
	//}

}
