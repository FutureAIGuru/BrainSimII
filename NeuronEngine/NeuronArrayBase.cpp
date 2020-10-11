#include "NeuronArrayBase.h"
#include <windows.h>
#include <ppl.h>
#include <iostream>
#include <random>

using namespace concurrency;
using namespace std;

namespace NeuronEngine
{
	Concurrency::concurrent_queue<SynapseBase> NeuronArrayBase::remoteQueue;
	Concurrency::concurrent_queue<NeuronBase*> NeuronArrayBase::fire2Queue;
	std::vector<unsigned long long> NeuronArrayBase::fireList1;
	std::vector<unsigned long long> NeuronArrayBase::fireList2;
	bool NeuronArrayBase::clearFireListNeeded;
	int NeuronArrayBase::fireListCount;

	std::string NeuronArrayBase::GetRemoteFiringString()
	{
		std::string retVal("");
		SynapseBase s;
		int count = 0;
		while (remoteQueue.try_pop(s) && count++ < 90) //splits up long strings for transmission
		{
			retVal += std::to_string(-(int)s.GetTarget()) + " ";
			retVal += std::to_string((float)s.GetWeight()) + " ";
			retVal += std::to_string((bool)s.IsHebbian()) + " ";
		}
		return retVal;
	}
	SynapseBase NeuronArrayBase::GetRemoteFiringSynapse()
	{
		SynapseBase s;
		if (remoteQueue.try_pop(s))
		{
			return s;
		}
		s.SetTarget(NULL);
		return s;
	}

	NeuronArrayBase::NeuronArrayBase()
	{
	}
	NeuronArrayBase::~NeuronArrayBase()
	{
	}

	void NeuronArrayBase::Initialize(int theSize, NeuronBase::modelType t)
	{
		arraySize = theSize;
		int expandedSize = arraySize;
		if (expandedSize % 64 != 0) expandedSize += 64;

		neuronArray.reserve(expandedSize);
		for (int i = 0; i < expandedSize; i++)
		{
			NeuronBase n(i);
			//n.SetModel(NeuronBase::modelType::LIF);  /for testing
			neuronArray.push_back(n);
		}
		fireList1.reserve(expandedSize / 64);
		fireList2.reserve(expandedSize / 64);
		fireListCount = expandedSize / 64;
		for (int i = 0; i < fireListCount; i++)
		{
			fireList1.push_back(0xffffffffffffffff);
			fireList2.push_back(0);
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
			GetBounds(value, start, end);
			for (int i = start; i < end; i++)
			{
				count += (long long)GetNeuron(i)->GetSynapseCount();;
			}
			});
		return count;;
	}
	long NeuronArrayBase::GetNeuronsInUseCount()
	{
		std::atomic<long> count = 0;
		parallel_for(0, threadCount, [&](int value) {
			ProcessNeurons1(value);
			int start, end;
			GetBounds(value, start, end);
			for (int i = start; i < end; i++)
			{
				if (GetNeuron(i)->GetInUse())
					count++;
			}
			});
		return count;;
	}

	void NeuronArrayBase::GetBounds(int taskID, int& start, int& end)
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
		if (clearFireListNeeded) ClearFireLists();
		clearFireListNeeded = false;
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
	void NeuronArrayBase::AddNeuronToFireList1(int id)
	{
		int index = id / 64;
		int offset = id % 64;
		unsigned long long bitMask = 0x1;
		bitMask = bitMask << offset;
		fireList1[index] |= bitMask;
	}
	void NeuronArrayBase::ClearFireLists()
	{
		for (int i = 0; i < fireListCount; i++)
		{
			fireList1[i] = 0xffffffffffffffff;
			fireList2[i] = 0;
		}
	}

	void NeuronArrayBase::ProcessNeurons1(int taskID)
	{
		int start, end;
		GetBounds(taskID, start, end);
		start /= 64;
		end /= 64;
		for (int i = start; i < end; i++)
		{
			unsigned long long tempVal = fireList1[i];
			fireList1[i] = 0;
			unsigned long long bitMask = 0x1;
			for (int j = 0; j < 64; j++)
			{
				if (tempVal & bitMask)
				{
					NeuronBase* theNeuron = GetNeuron(i * 64 + j);
					if (!theNeuron->Fire1(generation))
					{
						tempVal &= ~bitMask; //clear the bit if not firing for 2nd phase
					}
					else
						firedCount++;
				}
				bitMask = bitMask << 1;
			}
			fireList2[i] = tempVal;
		}
	}
	void NeuronArrayBase::ProcessNeurons2(int taskID)
	{
		int start, end;
		GetBounds(taskID, start, end);
		start /= 64;
		end /= 64;
		for (int i = start; i < end; i++)
		{
			unsigned long long tempVal = fireList2[i];
			unsigned long long bitMask = 0x1;
			for (int j = 0; j < 64; j++)
			{
				if (tempVal & bitMask)
				{
					NeuronBase* theNeuron = GetNeuron(i * 64 + j);
					theNeuron->Fire2();
				}
				bitMask = bitMask << 1;
			}
		}
	}
}
