#define CompilingNeuronWrapper
#pragma once
#include <Windows.h> 
#include <vector>
#include<string>
#include <tuple>

using namespace std;


namespace NeuronEngine
{
	class NeuronArrayBase;
	class NeuronBase;
	class SynapseBase;

	namespace CLI
	{
		struct Synapse { int target; float weight; int isHebbian; };

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
			long long GetGeneration();
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
			long long GetNeuronLastFired(int i);
			cli::array<byte>^ GetSynapses(int src);
			cli::array<byte>^ GetSynapsesFrom(int src);

			void AddSynapse(int src, int dest, float weight, bool isHebbian, bool noBackPtr);
			void AddSynapseFrom(int src, int dest, float weight, bool isHebbian);
			void DeleteSynapse(int src, int dest);
			void DeleteSynapseFrom(int src, int dest);

		private:
			// Pointer to our implementation
			NeuronEngine::NeuronArrayBase* theNeuronArray = NULL;
			cli::array<byte>^ ReturnArray(std::vector<SynapseBase> synapses);
		};
	}
}
