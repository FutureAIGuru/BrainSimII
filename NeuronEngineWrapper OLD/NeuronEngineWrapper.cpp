
#include "NeuronEngineWrapper.h"
#include "..\cppSpeedTest\NeuronArrayBase.h"

using namespace std;


namespace NeuronEngine
{
	namespace CLI
	{
		NeuronArrayBase::NeuronArrayBase()
		{
			if (theNeuronArray != NULL)
				delete theNeuronArray;
			theNeuronArray = new NeuronEngine::NeuronArrayBase();
		}
		NeuronArrayBase::~NeuronArrayBase()
		{}
		void NeuronArrayBase::Initialize(int neuronCount)
		{
			theNeuronArray->Initialize(neuronCount);
		}
		void NeuronArrayBase::Fire()
		{
			theNeuronArray->Fire();
		}
		int NeuronArrayBase::GetArraySize()
		{
			return theNeuronArray->GetArraySize();
		}
		long NeuronArrayBase::GetGeneration()
		{
			return theNeuronArray->GetGeneration();
		}
		int NeuronArrayBase::GetFiredCount()
		{
			return theNeuronArray->GetFiredCount();
		}
		void NeuronArrayBase::SetThreadCount(int theCount)
		{
			theNeuronArray->SetThreadCount(theCount);
		}
		int NeuronArrayBase::GetThreadCount()
		{
			return theNeuronArray->GetThreadCount();
		}

		float NeuronArrayBase::GetNeuronLastCharge(int i)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			return n->GetLastCharge();
		}
		void NeuronArrayBase::SetNeuronCharge(int i, float value)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			n->SetLastCharge(value);
		}
		void NeuronArrayBase::AddSynapse(int src, int dest, float weight, bool isHebbian)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			n->AddSynapse(theNeuronArray->GetNeuron(dest), weight, isHebbian);
		}
		void NeuronArrayBase::DeleteSynapse(int src, int dest, float weight)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			n->DeleteSynapse(theNeuronArray->GetNeuron(dest), weight);
		}

		struct Synapse { int target; float weight; bool isHebbian; };
		cli::array<byte>^ NeuronArrayBase::GetSynapses(int src)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			std::vector<SynapseBase>* tempVec = n->GetSynapses();
			byte* firstElem = (byte*)&tempVec->at(0);
			const int SIZE = tempVec->size(); //#of synapses
			const int byteCount = SIZE * sizeof(SynapseBase);
			cli::array<byte>^ tempArr = gcnew cli::array<byte>(byteCount);
			int k = 0;
			//this is complicated by the fact that the synapsebase contains a raw point but we want to return an ID
			for (int j = 0; j < tempVec->size(); j++)
			{
				Synapse s;
				s.isHebbian = tempVec->at(j).IsHebbian();
				s.weight = tempVec->at(j).GetWeight();
				s.target = tempVec->at(j).GetTarget()->GetId();
				byte* firstElem = (byte*)&s;
				for (int i = 0; i < sizeof(Synapse); i++)
				{
					tempArr[k++] = *(firstElem + i);
				}
			}
			return tempArr;
		}

		struct Neuron { int id; float lastCharge; float currentCharge; float leakRate; long lastFired; NeuronBase::modelType model; };
		cli::array<byte>^ NeuronArrayBase::GetNeuron(int src)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			const int byteCount = sizeof(Neuron);
			cli::array<byte>^ tempArr = gcnew cli::array<byte>(byteCount);
			Neuron n1;
			n1.id = n->GetId();
			n1.lastCharge = n->GetLastCharge();
			n1.currentCharge = n->GetCurrentCharge();
			n1.leakRate = n->GetLeakRate();
			n1.lastFired = n->GetLastFired();
			n1.model = n->GetModel();
			byte* firstElem = (byte*)&n1;
			for (int i = 0; i < sizeof(Neuron); i++)
			{
				tempArr[i] = *(firstElem + i);
			}
			return tempArr;
		}
		long NeuronArrayBase::GetTotalSynapses()
		{
			return theNeuronArray->GetTotalSynapseCount();
		}
	}
}