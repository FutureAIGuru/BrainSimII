#include "NeuronBase.h"
#include "SynapseBase.h"
#include "NeuronArrayBase.h"

namespace NeuronEngine
{
	NeuronBase::NeuronBase(int ID)
	{
		leakRate = 0;
		nextFiring = 0;
		id = ID;
	}
	
	NeuronBase::~NeuronBase()
	{
		if (synapses != NULL)
		{
			delete synapses;
		}
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
		currentCharge = value;
	}
	float NeuronBase::GetCurrentCharge()
	{
		return currentCharge;
	}
	float NeuronBase::GetLeakRate()
	{
		return leakRate;
	}
	void NeuronBase::SetLeakRate(float value)
	{
		leakRate = value;
	}
	long NeuronBase::GetLastFired()
	{
		return lastFired;
	}

	void NeuronBase::AddSynapse(NeuronBase* n, float weight, bool isHebbian)
	{
		SynapseBase s;
		s.SetWeight(weight);
		s.SetTarget(n);
		s.SetIsHebbian(isHebbian);
		if (synapses == NULL)
		{
			synapses = new std::vector<SynapseBase>();
		}

		for (int i = 0; i < synapses->size(); i++)
		{
			if (synapses->at(i).GetTarget() == n)
			{
				synapses->at(i).SetWeight(synapses->at(i).GetWeight() + weight);
				goto alreadyInList;
			}
		}
		synapses->push_back(s);
	alreadyInList:
		return;
	}

	void NeuronBase::DeleteSynapse(NeuronBase* n, float weight)
	{
		for (int i = 0; i < synapses->size(); i++)
		{
			if (synapses->at(i).GetTarget() == n)
			{
				if (weight == 0)
					synapses->erase(synapses->begin() + i);
				else
				{
					synapses->at(i).AddToWeight(-weight);
					if (synapses->at(i).GetWeight() == 0)
						synapses->erase(synapses->begin() + i);
				}
				break;
			}
		}
		if (synapses->size() == 0)
		{
			delete synapses;
			synapses = NULL;
		}
	}
	std::vector<SynapseBase>* NeuronBase::GetSynapses()
	{
		return synapses;
	}


	void NeuronBase::Fire1NoQ()
	{
		if (lastCharge < threshold)return;
		if (synapses != NULL)
		{
			for (long i = 0; i < synapses->size(); i++) //process all the synapses sourced by this neuron
			{
				SynapseBase s = synapses->at(i);
				NeuronBase* n = s.GetTarget();
				n->currentCharge = n->currentCharge + s.GetWeight();
				if (s.IsHebbian())
				{
					if (n->currentCharge > threshold)
						s.AddToWeight(.001);
					else
						s.AddToWeight(-.0001);
				}
			}
		}
	}

	bool NeuronBase::Fire2NoQ(long generation)
	{ //check for firing
		if (currentCharge < 0)currentCharge = 0;
		lastCharge = currentCharge;
		if (lastCharge >= threshold) {
			lastFired = generation;
			currentCharge = 0;
			return true;
		}
		return false;
	}



	//void NeuronBase::Fire1(std::vector<NeuronBase*> &neuronArray, std::vector<int>& nextQueue, std::vector<int>& zeroQueue)
	//{
	//	if (lastCharge < threshold)return;
	//	if (alreadyProcessedSynapses) return; //this prevents duplicates in the firing queue from being processed twice
	//	alreadyProcessedSynapses = true;
	//	for (int i = 0; i < synapses.size(); i++) //process all the synapses sourced by this neuron
	//	{
	//		SynapseBase* s = synapses[i];
	//		NeuronBase* n = s->N;
	//		//Interlocked.Add(ref n.currentCharge, s.IWeight);
	//		n->currentCharge += s->IWeight;
	//		//if the target neuron needs processing, add it to the firing queue
	//		if (!n->alreadyInQueue)
	//		{
	//			if (n->currentCharge >= threshold) // || n.currentCharge < 0))
	//			{
	//				n->alreadyInQueue = true; //a few duplicates are no problem because of the alreadyprocessed flag
	//				nextQueue.push_back(n->getId()); //we don't have to lock because this tread owns the queue
	//			}
	//		}
	//		if (n->currentCharge < 0)
	//		{
	//			zeroQueue.push_back(s->N->getId());
	//		}
	//	}
	//}

	//void NeuronBase::Fire2(std::vector<int>& firedQueue)
	//{ //check for firing
	//	alreadyInQueue = false;
	//	alreadyProcessedSynapses = 0;
	//	if (currentCharge < 0)
	//	{
	//		currentCharge = 0;
	//	}
	//	lastCharge = currentCharge;
	//	if (lastCharge >= threshold)
	//	{
	//		currentCharge = 0;
	//		//add yourself to the firing queue for next time to reset lastCharge on the next generation
	//		firedQueue.push_back(getId());
	//		alreadyInQueue = true;
	//	}
	//}


}
