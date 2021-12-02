#include "pch.h"

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
		void NeuronArrayBase::SetGeneration(long long i)
		{
			theNeuronArray->SetGeneration(i);
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
		void NeuronArrayBase::SetRefractoryDelay(int i)
		{
			theNeuronArray->SetRefractoryDelay(i);
		}
		int NeuronArrayBase::GetRefractoryDelay()
		{
			return theNeuronArray->GetRefractoryDelay();
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
		void NeuronArrayBase::AddToNeuronCurrentCharge(int i, float value)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			n->AddToCurrentValue(value);
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
		int NeuronArrayBase::GetNeuronAxonDelay(int i)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			return n->GetAxonDelay();
		}
		void NeuronArrayBase::SetNeuronAxonDelay(int i, int value)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(i);
			n->SetAxonDelay(value);
		}
		long long NeuronArrayBase::GetNeuronLastFired(int i)
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
		String^ NeuronArrayBase::GetRemoteFiring()
		{
			std::string remoteFiring = theNeuronArray->GetRemoteFiringString();
			String^ str = gcnew String(remoteFiring.c_str());
			return str;
		}
		cli::array<byte>^ NeuronArrayBase::GetRemoteFiringSynapses()
		{
			std::vector<SynapseBase> tempVec;
			SynapseBase s1 = theNeuronArray->GetRemoteFiringSynapse();
			while (s1.GetTarget() != NULL)
			{
				tempVec.push_back(s1);
				s1 = theNeuronArray->GetRemoteFiringSynapse();
			}
			return ReturnArray(tempVec);
		}

		void NeuronArrayBase::AddSynapse(int src, int dest, float weight, int model, bool noBackPtr)
		{
			if (src < 0)return;
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			if (dest < 0)
				n->AddSynapse((NeuronBase*)(long long)dest, weight, (SynapseBase::modelType) model, noBackPtr);
			else
				n->AddSynapse(theNeuronArray->GetNeuron(dest), weight, (SynapseBase::modelType)model, noBackPtr);
		}
		void NeuronArrayBase::AddSynapseFrom(int src, int dest, float weight, int model)
		{
			if (dest < 0)return;
			NeuronBase* n = theNeuronArray->GetNeuron(dest);
			if (src < 0)
				n->AddSynapseFrom((NeuronBase*)(long long)src, weight, (SynapseBase::modelType)model);
			else
				n->AddSynapseFrom(theNeuronArray->GetNeuron(src), weight, (SynapseBase::modelType)model);
		}
		void NeuronArrayBase::DeleteSynapse(int src, int dest)
		{
			if (src < 0) return;
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			if (dest < 0)
				n->DeleteSynapse((NeuronBase*)(long long)dest);
			else
				n->DeleteSynapse(theNeuronArray->GetNeuron(dest));
		}
		void NeuronArrayBase::DeleteSynapseFrom(int src, int dest)
		{
			if (dest < 0)return;
			NeuronBase* n = theNeuronArray->GetNeuron(dest);
			if (src < 0)
				n->DeleteSynapse((NeuronBase*)(long long)src);
			else
				n->DeleteSynapse(theNeuronArray->GetNeuron(src));
		}


		cli::array<byte>^ NeuronArrayBase::GetSynapses(int src)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			n->GetLock();
			std::vector<SynapseBase> tempVec = n->GetSynapses();
			n->ClearLock();
			return ReturnArray(tempVec);

		}
		cli::array<byte>^ NeuronArrayBase::GetSynapsesFrom(int src)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			n->GetLock();
			std::vector<SynapseBase> tempVec = n->GetSynapsesFrom();
			n->ClearLock();
			return ReturnArray(tempVec);
		}

		cli::array<byte>^ NeuronArrayBase::ReturnArray(std::vector<SynapseBase> tempVec)
		{
			if (tempVec.size() == 0)
			{
				return gcnew cli::array<byte>(0);
			}
			byte* firstElem = (byte*)&tempVec.at(0);
			const size_t SIZE = tempVec.size(); //#of synapses
			const int byteCount = (int)(SIZE * sizeof(Synapse));
			cli::array<byte>^ tempArr = gcnew cli::array<byte>(byteCount);
			int k = 0;
			for (int j = 0; j < tempVec.size(); j++)
			{
				Synapse s;
				s.model = (int)tempVec.at(j).GetModel();
				s.weight = tempVec.at(j).GetWeight();
				//if the top bit of the target is not set, it's a raw pointer
				//if it is set, this is the negative of a global neuron ID
				NeuronBase* target = tempVec.at(j).GetTarget();
				if (((long long)target >> 63) != 0 || target == NULL)
					s.target = (int)(long long)(tempVec.at(j).GetTarget());
				else
					s.target = tempVec.at(j).GetTarget()->GetId();
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
			float leakRate; int axonDelay; NeuronBase::modelType model; long long lastFired;
		};
		cli::array<byte>^ NeuronArrayBase::GetNeuron(int src)
		{
			NeuronBase* n = theNeuronArray->GetNeuron(src);
			const int byteCount = sizeof(Neuron);
			cli::array<byte>^ tempArr = gcnew cli::array<byte>(byteCount);
			Neuron n1;
			memset(&n1, 0, byteCount); //clear out the space between struct elements
			n1.id = n->GetId();
			n1.inUse = n->GetInUse();
			n1.lastCharge = n->GetLastCharge();
			n1.currentCharge = n->GetCurrentCharge();
			n1.leakRate = n->GetLeakRate();
			n1.lastFired = n->GetLastFired();
			n1.model = n->GetModel();
			n1.axonDelay = n->GetAxonDelay();
			byte* firstElem = (byte*)&n1;
			for (int i = 0; i < sizeof(Neuron); i++)
			{
				tempArr[i] = *(firstElem + i);
			}
			return tempArr;
		}
		long long NeuronArrayBase::GetTotalSynapses()
		{
			return theNeuronArray->GetTotalSynapseCount();
		}
		long NeuronArrayBase::GetTotalNeuronsInUse()
		{
			return theNeuronArray->GetNeuronsInUseCount();
		}
	}
}