

#include "NeuronBase.h"
#include "SynapseBase.h"
#include "NeuronArrayBase.h"

namespace NeuronEngine
{
	NeuronBase::NeuronBase(int ID)
	{
		leakRate = 0.1f;
		nextFiring = 0;
		id = ID;
	}

	NeuronBase::~NeuronBase()
	{
		delete synapses;
		delete synapsesFrom;
		delete label;
	}

	int NeuronBase::GetId()
	{
		return id;
	}

	NeuronBase::modelType NeuronBase::GetModel()
	{
		return model;
	}

	void NeuronBase::SetModel(modelType value)
	{
		model = value;
	}
	float NeuronBase::GetLastCharge()
	{
		return lastCharge;
	}
	void NeuronBase::SetLastCharge(float value)
	{
		lastCharge = value;
	}
	float NeuronBase::GetCurrentCharge()
	{
		return currentCharge;
	}
	void NeuronBase::SetCurrentCharge(float value)
	{
		currentCharge = value;
	}
	float NeuronBase::GetLeakRate()
	{
		return leakRate;
	}
	void NeuronBase::SetLeakRate(float value)
	{
		leakRate = value;
	}
	long long NeuronBase::GetLastFired()
	{
		return lastFired;
	}
	wchar_t* NeuronBase::GetLabel()
	{
		return label;
	}
	void NeuronBase::SetLabel(const wchar_t* newLabel)
	{
		delete label;
		label = NULL;
		int len = wcslen(newLabel) + 2;
		if (len > 1)
		{
			label = new wchar_t[len];
			wcscpy_s(label, len, newLabel);
		}
	}
	bool NeuronBase::InUse()
	{
		bool retVal = (label != NULL) || (synapses != NULL) || (synapsesFrom != NULL || model != modelType::Std);

		return retVal;
	}

	void NeuronBase::AddSynapse(NeuronBase* n, float weight, bool isHebbian, bool noBackPtr)
	{
		while (vectorLock.exchange(1) == 1) {}

		SynapseBase s1;
		s1.SetWeight(weight);
		s1.SetTarget(n);
		s1.SetIsHebbian(isHebbian);

		if (synapses == NULL)
		{
			synapses = new std::vector<SynapseBase>();
			synapses->reserve(10);
		}
		for (int i = 0; i < synapses->size(); i++)
		{
			if (synapses->at(i).GetTarget() == n)
			{
				//update an existing synapse
				synapses->at(i).SetWeight(weight);
				synapses->at(i).SetIsHebbian(isHebbian);
				goto alreadyInList;
			}
		}
		//else create a new synapse
		synapses->push_back(s1);
	alreadyInList:
		vectorLock = 0;

		if (noBackPtr) return;

		//now add the synapsesFrom entry to the target neuron
		//this requires locking because multiply neurons may link to a single neuron simultaneously requiring backpointers.
		//The previous does not lock because you don't write to the same neuron from multiple threads

		while (n->vectorLock.exchange(1) == 1) {}
		//n->aLock.lock();
		SynapseBase s2;
		s2.SetTarget(this);
		s2.SetWeight(weight);
		s2.SetIsHebbian(isHebbian);

		if (n->synapsesFrom == NULL)
		{
			n->synapsesFrom = new std::vector<SynapseBase>();
			n->synapsesFrom->reserve(10);
		}
		for (int i = 0; i < n->synapsesFrom->size(); i++)
		{
			SynapseBase s = n->synapsesFrom->at(i);
			if (s.GetTarget() == this)
			{
				s.SetWeight(weight);
				s.SetIsHebbian(isHebbian);
				goto alreadyInList2;
			}
		}
		n->synapsesFrom->push_back(s2);
	alreadyInList2:
		n->vectorLock = 0;

		return;
	}

	void NeuronBase::DeleteSynapse(NeuronBase* n)
	{
		while (vectorLock.exchange(1) == 1) {}
		for (int i = 0; i < synapses->size(); i++)
		{
			if (synapses->at(i).GetTarget() == n)
			{
				synapses->erase(synapses->begin() + i);
				break;
			}
		}
		if (synapses->size() == 0)
		{
			delete synapses;
			synapses = NULL;
		}
		vectorLock = 0;
		while (n->vectorLock.exchange(1) == 1) {}
		if (n->synapsesFrom != NULL)
		{
			for (int i = 0; i < n->synapsesFrom->size(); i++)
			{
				SynapseBase s = n->synapsesFrom->at(i);
				if (s.GetTarget() == this)
				{
					n->synapsesFrom->erase(n->synapsesFrom->begin() + i);
					break;
				}
			}
		}
		n->vectorLock = 0;
	}

	std::vector<SynapseBase> NeuronBase::GetSynapses()
	{
		if (synapses == NULL)
		{
			std::vector<SynapseBase> tempVec = std::vector<SynapseBase>();
			return tempVec;
		}
		while (vectorLock.exchange(1) == 1) {}
		std::vector<SynapseBase> tempVec = std::vector<SynapseBase>(*synapses);
		vectorLock = 0;
		return tempVec;
	}
	std::vector<SynapseBase> NeuronBase::GetSynapsesFrom()
	{
		if (synapsesFrom == NULL)
		{
			std::vector<SynapseBase> tempVec = std::vector<SynapseBase>();
			return tempVec;
		}
		while (vectorLock.exchange(1) == 1) {}
		std::vector<SynapseBase> tempVec = std::vector<SynapseBase>(*synapsesFrom);
		vectorLock = 0;
		return tempVec;
	}

	//neuron firing is two-phase so that the network is independent of neuron order
	bool NeuronBase::Fire1(long long generation)
	{
		if (model == modelType::Color) return false;
		if (model == modelType::FloatValue) return false;
		if (model == modelType::Random)
		{
			nextFiring--;
			if (nextFiring <= 0)
			{
				currentCharge = threshold;
				nextFiring = rand() % 100 * leakRate;
			}
		}
		//check for firing
		if (currentCharge < 0)currentCharge = 0;
		lastCharge = currentCharge;
		if (lastCharge >= threshold) {
			lastFired = generation;
			currentCharge = 0;
			return true;
		}
		if (model == modelType::LIF)
		{
			currentCharge = currentCharge * (1 - leakRate);
			if (currentCharge < .1f)
				currentCharge = 0;
		}
		return false;
	}

	void NeuronBase::Fire2()
	{
		if (model == modelType::Color) return;
		if (model == modelType::FloatValue) return;
		if (lastCharge < threshold)return;
		if (synapses != NULL)
		{
			while (vectorLock.exchange(1) == 1) {}
			for (int i = 0; i < synapses->size(); i++) //process all the synapses sourced by this neuron
			{
				SynapseBase s = synapses->at(i);
				NeuronBase* n = s.GetTarget();
				n->currentCharge = n->currentCharge + s.GetWeight();
				if (s.IsHebbian())
				{
					//we have to use lastcharge here because currentCharge is indeterminate in multithread environment
					if (n->lastCharge >= threshold) 
					{
						//strengthen the synapse
						if (s.GetWeight() <= 1) synapses->at(i).SetWeight(1);
					}
					else
					{
						//weaken the synapse
					}
				}
			}
			vectorLock = 0;
		}
	}

}
