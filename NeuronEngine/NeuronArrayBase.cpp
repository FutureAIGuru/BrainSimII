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
	long long NeuronArrayBase::GetGeneration()
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

	long long NeuronArrayBase::GetTotalSynapseCount()
	{
		std::atomic<long long> count = 0;
		parallel_for(0, threadCount, [&](int value) {
			int start, end;
			GetBounds1(value, start, end);
			for (int i = start; i < end; i++)
			{
				count += (long long) GetNeuron(i)->GetSynapseCount();;
			}
			});
		return count;;
	}
	long long NeuronArrayBase::GetNeuronsInUseCount()
	{
		std::atomic<long> count = 0;
		parallel_for(0, threadCount, [&](int value) {
			ProcessNeurons1(value);
			int start, end;
			GetBounds1(value, start, end);
			for (int i = start; i < end; i++)
			{
				if (GetNeuron(i)->GetInUse())
				count ++;
			}
			});
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
			if (GetNeuron(i)->Fire1(generation))
				firedCount++;
	}
	void NeuronArrayBase::ProcessNeurons2(int taskID)
	{
		int start, end;
		GetBounds1(taskID, start, end);
		for (int i = start; i < end; i++)
			GetNeuron(i)->Fire2();
	}
}
