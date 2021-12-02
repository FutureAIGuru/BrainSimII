#define CompilingNeuronWrapper
#pragma once

#include <Windows.h> 
#include <vector>
#include <string>
#include <tuple>
#include <array>


using namespace std;

namespace NeuronEngine
{
	class NeuronArrayBase;
	class NeuronBase;
	class SynapseBase;

	typedef unsigned char byte;

	namespace CLI
	{
		struct Synapse { int target; float weight; int model; };

		public ref class NeuronArrayBase
		{
		public:
			NeuronArrayBase();
			~NeuronArrayBase();
			void Initialize(int numberOfNeurons);

			void Fire();
			int GetArraySize();
			int GetThreadCount();
			void SetThreadCount(int i);
			int GetRefractoryDelay();
			void SetRefractoryDelay(int i);
			long long GetGeneration();
			void SetGeneration(long long i);
			int GetFiredCount();
			long long GetTotalSynapses();
			long GetTotalNeuronsInUse();

			cli::array<byte>^ GetNeuron(int src);
			float GetNeuronLastCharge(int i);
			void SetNeuronLastCharge(int i, float value);
			void SetNeuronCurrentCharge(int i, float value);
			void AddToNeuronCurrentCharge(int i, float value);
			bool GetNeuronInUse(int i);
			System::String^ GetNeuronLabel(int i);
			
			System::String^ GetRemoteFiring();
			cli::array<byte>^ GetRemoteFiringSynapses();

			void SetNeuronLabel(int i, System::String^ newLabel);
			int GetNeuronModel(int i);
			void SetNeuronModel(int i, int model);
			float GetNeuronLeakRate(int i);
			void SetNeuronLeakRate(int i, float value);
			int GetNeuronAxonDelay(int i);
			void SetNeuronAxonDelay(int i, int value);
			long long GetNeuronLastFired(int i);
			cli::array<byte>^ GetSynapses(int src);
			cli::array<byte>^ GetSynapsesFrom(int src);

			void AddSynapse(int src, int dest, float weight, int model, bool noBackPtr);
			void AddSynapseFrom(int src, int dest, float weight, int model);
			void DeleteSynapse(int src, int dest);
			void DeleteSynapseFrom(int src, int dest);

		private:
			// Pointer to our implementation
			NeuronEngine::NeuronArrayBase* theNeuronArray = NULL;
			cli::array<byte>^ ReturnArray(std::vector<SynapseBase> synapses);
		};
	}
}
