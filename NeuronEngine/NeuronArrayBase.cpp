#include "pch.h"

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
	int NeuronArrayBase::refractoryDelay = 0;

	std::string NeuronArrayBase::GetRemoteFiringString()
	{
		std::string retVal("");
		SynapseBase s;
		int count = 0;
		while (remoteQueue.try_pop(s) && count++ < 90) //splits up long strings for transmission
		{
			retVal += std::to_string(-(long long)s.GetTarget()) + " ";
			retVal += std::to_string((float)s.GetWeight()) + " ";
			retVal += std::to_string((int)s.GetModel()) + " ";
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
		if (expandedSize % 64 != 0)
			expandedSize = ((expandedSize / 64) + 1) * 64;
		arraySize = expandedSize;
		neuronArray.reserve(expandedSize);
		for (int i = 0; i < expandedSize; i++)
		{
			NeuronBase n(i);
			//n.SetModel(NeuronBase::modelType::LIF);  /for testing
			neuronArray.push_back(n);
		}
		fireList1.reserve(expandedSize / 64);
		fireList2.reserve(expandedSize / 64);

		int fireListCount = expandedSize / 64;
		for (int i = 0; i < fireListCount; i++)
		{
			fireList1.push_back(0xffffffffffffffff);
			fireList2.push_back(0);

		}
	}
	long long NeuronArrayBase::GetGeneration()
	{
		return cycle;
	}
	void NeuronArrayBase::SetGeneration(long long i)
	{
		cycle = i;
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

	void NeuronArrayBase::
		GetBounds(int taskID, int& start, int& end)
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

	//this is just like getBounds except that start and end must be even multiples of 64
	//so there won't be collisions on the firelists
	void NeuronArrayBase::GetBounds64(int taskID, int& start, int& end)
	{
		int numberToProcess = arraySize / threadCount;
		if (numberToProcess % 64 == 0)
		{
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
		else
		{
			numberToProcess = (numberToProcess / 64 + 1) * 64;
			int numUseableThreads = arraySize / numberToProcess;
			if (taskID > numUseableThreads)
			{
				start = 0;
				end = 0;
				return;
			}
			int remainder = arraySize % numberToProcess;
			start = numberToProcess * taskID;
			end = start + numberToProcess;
			if (taskID == numUseableThreads)
			{
				end = start + remainder;
			}
		}
	}
	void NeuronArrayBase::Fire()
	{
		if (clearFireListNeeded)
			ClearFireLists();
		clearFireListNeeded = false;
		cycle++;
		firedCount = 0;

		parallel_for(0, threadCount, [&](int value) {
			ProcessNeurons1(value);
			});
		parallel_for(0, threadCount, [&](int value) {
			ProcessNeurons2(value);
			});
		
			ProcessNeurons3(0);
			
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
	int NeuronArrayBase::GetRefractoryDelay()
	{
		return refractoryDelay;
	}
	void NeuronArrayBase::SetRefractoryDelay(int i)
	{
		refractoryDelay = i;
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
		for (int i = 0; i < fireList1.size(); i++)
		{
			fireList1[i] = 0xffffffffffffffff;
			fireList2[i] = 0;

		}
	}

	void NeuronArrayBase::ProcessNeurons1(int taskID)
	{
		int start, end;
		GetBounds64(taskID, start, end);
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
					int neuronID = i * 64 + j;
					NeuronBase* theNeuron = GetNeuron(neuronID);
					if (!theNeuron->Fire1(cycle))
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
		GetBounds64(taskID, start, end);
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

	void NeuronArrayBase::ProcessNeurons3(int taskID)
	{
		for (int i = 0; i < arraySize; i++)
		{
			NeuronBase* theNeuron = GetNeuron(i);
			theNeuron->Fire3();
		}
	}
}
