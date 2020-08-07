
#include "NeuronEngineWrapper.h"
#include "..\NeuronEngine\NeuronArrayBase.h"
#include <Windows.h>


using namespace System;
using namespace std;
using namespace System::Runtime::InteropServices;


namespace NeuronEngine
{
	namespace CLI
	{
		NeuronArrayBase::NeuronArrayBase()
		{
		}
		NeuronArrayBase::~NeuronArrayBase()
		{
			delete theNeuronArray;
		}
		void NeuronArrayBase::Initialize(int neuronCount)
		{
			if (theNeuronArray != NULL)
				delete theNeuronArray;
			theNeuronArray = new NeuronEngine::NeuronArrayBase();
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
		long long NeuronArrayBase::GetGeneration()
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
		void NeuronArrayBase::SetNeuronLastCharge(int i, float value)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			n->SetLastCharge(value);
		}
		void NeuronArrayBase::SetNeuronCurrentCharge(int i, float value)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			n->SetCurrentCharge(value);
		}
		float NeuronArrayBase::GetNeuronLeakRate(int i)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			return n->GetLeakRate();
		}
		void NeuronArrayBase::SetNeuronLeakRate(int i, float value)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			n->SetLeakRate(value);
		}
		long NeuronArrayBase::GetNeuronLastFired(int i)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			return n->GetLastFired();
		}
		int NeuronArrayBase::GetNeuronModel(int i)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			return (int)n->GetModel();
		}
		void NeuronArrayBase::SetNeuronModel(int i, int model)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			n->SetModel((NeuronBase::modelType) model);
		}
		bool NeuronArrayBase::GetNeuronInUse(int i)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			return n->GetInUse();
		}
		void NeuronArrayBase::SetNeuronLabel(int i, String^ newLabel)
		{
			const wchar_t* chars = (const wchar_t*)(Marshal::StringToHGlobalAuto(newLabel)).ToPointer();
			theNeuronArray->GetNeuron(i)->SetLabel(chars);
			Marshal::FreeHGlobal(IntPtr((void*)chars));
		}
		String^ NeuronArrayBase::GetNeuronLabel(int i)
		{
			wchar_t* labelChars = theNeuronArray->GetNeuron(i)->GetLabel();
			if (labelChars != NULL)
			{
				std::wstring label(labelChars);
				String^ str = gcnew String(label.c_str());
				return str;
			}
			else
			{
				String^ str = gcnew String("");
				return str;
			}
		}

		void NeuronArrayBase::AddSynapse(int src, int dest, float weight, bool isHebbian, bool noBackPtr)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			n->AddSynapse(theNeuronArray->GetNeuron(dest), weight, isHebbian, noBackPtr);
		}
		void NeuronArrayBase::DeleteSynapse(int src, int dest)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			n->DeleteSynapse(theNeuronArray->GetNeuron(dest));
		}

		struct Synapse { int target; float weight; int isHebbian; };

		cli::array<byte>^ NeuronArrayBase::GetSynapses(int src)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			std::vector<SynapseBase> tempVec = n->GetSynapses();
			return ReturnArray(tempVec);

		}
		cli::array<byte>^ NeuronArrayBase::GetSynapsesFrom(int src)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			std::vector<SynapseBase> tempVec = n->GetSynapsesFrom();
			return ReturnArray(tempVec);
		}

		cli::array<byte>^ NeuronArrayBase::ReturnArray(std::vector<SynapseBase> tempVec)
		{
			if (tempVec.size() == 0)
			{
				return gcnew cli::array<byte>(0);
			}
			byte* firstElem = (byte*)&tempVec.at(0);
			const int SIZE = tempVec.size(); //#of synapses
			const int byteCount = SIZE * sizeof(Synapse);
			cli::array<byte>^ tempArr = gcnew cli::array<byte>(byteCount);
			int k = 0;
			//this is complicated by the fact that the synapsebase contains a raw point but we want to return an ID
			for (int j = 0; j < tempVec.size(); j++)
			{
				Synapse s;
				s.isHebbian = tempVec.at(j).IsHebbian();
				s.weight = tempVec.at(j).GetWeight();
				s.target = tempVec.at(j).GetTarget()->GetId();
				if (tempVec.at(j).IsHebbian()) //this makes a bool clear all four bytes
					s.isHebbian = 1;
				else
					s.isHebbian = 0;
				byte* firstElem = (byte*)&s;
				for (int i = 0; i < sizeof(Synapse); i++)
				{
					tempArr[k++] = *(firstElem + i);
				}
			}
			return tempArr;
		}

		struct Neuron {
			int id;  bool inUse; float lastCharge; float currentCharge;
			float leakRate; NeuronBase::modelType model; long long lastFired;
		};
		cli::array<byte>^ NeuronArrayBase::GetNeuron(int src)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			const int byteCount = sizeof(Neuron);
			cli::array<byte>^ tempArr = gcnew cli::array<byte>(byteCount);
			Neuron n1;
			n1.id = n->GetId();
			n1.inUse = n->GetInUse();
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
		long NeuronArrayBase::GetTotalNeuronsInUse()
		{
			return theNeuronArray->GetNeuronsInUseCount();
		}
	}
}