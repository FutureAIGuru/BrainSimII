#pragma once

#include "NeuronBase.h"
#include "SynapseBase.h"
#include <vector>
#include <atomic>


namespace NeuronEngine
{
	class __declspec(dllexport) NeuronArrayBase
	{
	public:
		NeuronArrayBase();
		~NeuronArrayBase();
		void Initialize(int theSize, NeuronBase::modelType t = NeuronBase::modelType::Std);
		NeuronBase* GetNeuron(int i);
		int GetArraySize();
		long long GetTotalSynapseCount();
		long long GetNeuronsInUseCount();
		void Fire();
		long long GetGeneration();
		int GetFiredCount();
		int GetThreadCount();
		void SetThreadCount(int i);
		void GetBounds1(int taskID, int& start, int& end);

	private:
		int arraySize = 0;
		int threadCount = 120;
		std::vector<NeuronBase> neuronArray;
		std::atomic<long> firedCount = 0;
		long generation = 0;

	private:
		void ProcessNeurons1(int taskID);
		void ProcessNeurons2(int taskID);
	};

}