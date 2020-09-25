

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
		size_t len = wcslen(newLabel);
		if (len > 1)
		{
			label = new wchar_t[len+2];
			wcscpy_s(label, len+2, newLabel);
		}
	}
	bool NeuronBase::GetInUse()
	{
		bool retVal = (label != NULL) || (synapses != NULL && synapses->size() != 0) || (synapsesFrom != NULL && synapsesFrom->size() != 0) || (model != modelType::Std);

		return retVal;
	}

	void NeuronBase::AddSynapseFrom(NeuronBase* n, float weight, bool isHebbian)
	{
		while (vectorLock.exchange(1) == 1) {}

		SynapseBase s1;
		s1.SetWeight(weight);
		s1.SetTarget(n);
		s1.SetIsHebbian(isHebbian);

		if (synapsesFrom == NULL)
		{
			synapsesFrom = new std::vector<SynapseBase>();
			synapsesFrom->reserve(10);
		}
		for (int i = 0; i < synapsesFrom->size(); i++)
		{
			if (synapsesFrom->at(i).GetTarget() == n)
			{
				//update an existing synapse
				synapsesFrom->at(i).SetWeight(weight);
				synapsesFrom->at(i).SetIsHebbian(isHebbian);
				goto alreadyInList;
			}
		}
		//else create a new synapse
		synapsesFrom->push_back(s1);
	alreadyInList:
		vectorLock = 0;
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
			if (n->synapsesFrom->at(i).GetTarget() == this)
			{
				n->synapsesFrom->at(i).SetWeight(weight);
				n->synapsesFrom->at(i).SetIsHebbian(isHebbian);
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
		if (synapses != NULL)
		{
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
		}
		vectorLock = 0;
		if (((long long)n >>63) != 0) return;
		while (n->vectorLock.exchange(1) == 1) {}
		if (n->synapsesFrom != NULL)
		{
			for (int i = 0; i < n->synapsesFrom->size(); i++)
			{
				SynapseBase s = n->synapsesFrom->at(i);
				if (s.GetTarget() == this)
				{
					n->synapsesFrom->erase(n->synapsesFrom->begin() + i);
					if (n->synapsesFrom->size() == 0)
					{
						delete n->synapsesFrom;
						n->synapsesFrom = NULL;
					}
					break;
				}
			}
		}
		n->vectorLock = 0;
	}
	int NeuronBase::GetSynapseCount()
	{
		if (synapses == NULL) return 0;
		return (int)synapses->size();
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

	void NeuronBase::AddToCurrentValue(float weight)
	{
		currentCharge = currentCharge + weight;
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
				nextFiring = (int)(rand() % randomRate);
			}
		}
		//check for firing
		if (currentCharge < 0)currentCharge = 0;
		lastCharge = currentCharge;
		if (lastCharge >= threshold) 
		{
			lastFired = generation;
			currentCharge = 0;
			return true;
		}
		if (model == modelType::LIF || model == modelType::Random)
		{
			currentCharge = currentCharge * (1 - leakRate);
		}
		return false;
	}

	void NeuronBase::Fire2()
	{
		if (model == modelType::Color) return;
		if (model == modelType::FloatValue) return;
		if (lastCharge < threshold)return; //did the neuron fire?
		while (vectorLock.exchange(1) == 1) {} //prevent the vector of synapses from changing while we're looking at it
		if (synapses != NULL)
		{
			for (int i = 0; i < synapses->size(); i++) //process all the synapses sourced by this neuron
			{
				SynapseBase s = synapses->at(i);
				NeuronBase* nTarget = s.GetTarget();
				if (((long long)nTarget >>63 ) != 0) //does this synapse go to another server
				{
					NeuronArrayBase::remoteQueue.push(s);
				}
				else
				{	//nTarget->currentCharge += s.GetWeight(); //not supported until C++20
					auto current = nTarget->currentCharge.load(std::memory_order_relaxed);
					float desired = current + s.GetWeight();
					while (!nTarget->currentCharge.compare_exchange_weak(current, desired));

					//for a random neuron, decrease the randomness if synapses caused a firing
					if (nTarget->model == modelType::Random && desired >= threshold)
					{
						nTarget->randomRate = nTarget->randomRate * 2;
						//nextFiring = (int)(rand() % randomRate);
						nextFiring = 10000;
					}
					if (s.IsHebbian()) //old method needed for network graph demo
					{
						//did this neuron fire coincident with the target
						if (nTarget->currentCharge >= threshold) 
						{
							//strengthen the synapse
							float newWeight = s.GetWeight() + 0.1f;
							synapses->at(i).SetWeight(newWeight);
						}
						else
						{
							//weaken the synapse
							float newWeight = s.GetWeight() - 0.01f;
							synapses->at(i).SetWeight(newWeight);
						}
					}
				}
			}
		}
		//if (synapsesFrom != NULL)
		//{
		//	int hebbianCount = 0;
		//	for (int i = 0; i < synapsesFrom->size(); i++)
		//	{
		//		SynapseBase s = synapsesFrom->at(i);
		//		if (s.IsHebbian()) hebbianCount++;
		//	}
		//	for (int i = 0; i < synapsesFrom->size(); i++)
		//	{
		//		SynapseBase s = synapsesFrom->at(i);
		//		if (s.IsHebbian())
		//		{
		//			NeuronBase* nTarget = s.GetTarget();
		//			while (nTarget->vectorLock.exchange(1) == 1) {}
		//			float newWeight = s.GetWeight(); //target = .25 
		//			if (nTarget->lastCharge >= threshold)
		//			{
		//				//hit
		//				float target = 1.1f / hebbianCount;
		//				newWeight = (newWeight+target)/2; 
		//			}
		//			else
		//			{
		//				//miss
		//				float target = -.2f;
		//				newWeight = (newWeight + target) / 2;
		//			}
		//			synapsesFrom->at(i).SetWeight(newWeight);
		//			if (nTarget->synapses != NULL) //set the forward synapse weight
		//			{
		//				for (int j = 0; j < nTarget->synapses->size(); j++)
		//				{
		//					if (nTarget->synapses->at(j).GetTarget() == this)
		//					{
		//						nTarget->synapses->at(j).SetWeight(newWeight);
		//						break;
		//					}
		//				}
		//			}
		//			nTarget->vectorLock = 0;
		//		}
		//	}
		//}
		vectorLock = 0;
	}
}
