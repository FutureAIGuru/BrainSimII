#pragma once
#include <Windows.h> //for byte


using namespace std;

namespace NeuronEngine
{
	class NeuronArrayBase;
	class NeuronBase;

	namespace CLI
	{

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
			long GetGeneration();
			int GetFiredCount();

			cli::array<byte>^ GetNeuron(int src);
			float GetNeuronLastCharge(int i);
			void SetNeuronCharge(int i,float value);
			cli::array<byte>^ GetSynapses(int src);

			void AddSynapse(int src, int dest, float weight, bool isHebbian);
			void DeleteSynapse(int src, int dest, float weight);
			long GetTotalSynapses();
			
		private:
			// Pointer to our implementation
			static NeuronEngine::NeuronArrayBase* theNeuronArray = NULL;
		};
	}
}
