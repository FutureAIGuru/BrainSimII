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
		NeuronBase *GetNeuron(int i);
		int GetArraySize();
		long long GetTotalSynapseCount();
		long long GetNeuronsInUseCount();
		void Fire();
		long long GetGeneration();
		int GetFiredCount();
		int GetThreadCount();
		void SetThreadCount(int i);

	private:
		int arraySize = 0;
		int threadCount = 120;
		std::vector<NeuronBase> neuronArray;
		std::atomic<long> firedCount = 0;
		long generation = 0;

	private:
		void GetBounds1(int taskID, int& start, int& end);
		void ProcessNeurons1(int taskID);
		void ProcessNeurons2(int taskID);
	};

	//Variables for the Neuron Firing Queue

	//private:
	//void GetBounds(int taskID, int& start, int& end);
	//	const  int queuesize = 2'000'000;
//	//how many parallel tasks and the array to hold them
//	std::vector<int> firingQueue;
//	std::vector<std::vector<int>> nextQueue;
//	std::vector<std::vector<int>> zeroQueue;
//	std::vector<std::vector<int>> firedQueue;
//	std::vector<int> manualFire;



}
