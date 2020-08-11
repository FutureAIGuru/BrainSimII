#pragma once

#include "NeuronBase.h"
#include "SynapseBase.h"
#include <vector>
#include <atomic>


namespace NeuronEngine
{
	class NeuronArrayBase
	{
	public:
		__declspec(dllexport) NeuronArrayBase();
		__declspec(dllexport) ~NeuronArrayBase();
		__declspec(dllexport) void Initialize(int theSize, NeuronBase::modelType t = NeuronBase::modelType::Std);
		__declspec(dllexport) NeuronBase* GetNeuron(int i);
		__declspec(dllexport) int GetArraySize();
		__declspec(dllexport) long long GetTotalSynapseCount();
		__declspec(dllexport) long GetNeuronsInUseCount();
		__declspec(dllexport) void Fire();
		__declspec(dllexport) long long GetGeneration();
		__declspec(dllexport) int GetFiredCount();
		__declspec(dllexport) int GetThreadCount();
		__declspec(dllexport) void SetThreadCount(int i);
		__declspec(dllexport) void GetBounds(int taskID, int& start, int& end);

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