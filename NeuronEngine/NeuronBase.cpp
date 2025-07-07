#include "pch.h"

#include "NeuronBase.h"
#include "SynapseBase.h"
#include "NeuronArrayBase.h"
#include <cmath>

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
		NeuronArrayBase::clearFireListNeeded = true;
		lastCharge = value;
	}
	float NeuronBase::GetCurrentCharge()
	{
		return currentCharge;
	}
	void NeuronBase::SetCurrentCharge(float value)
	{
		NeuronArrayBase::clearFireListNeeded = true;
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
	int NeuronBase::GetAxonDelay()
	{
		return axonDelay;
	}
	void NeuronBase::SetAxonDelay(int value)
	{
		axonDelay = value;
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
		if (len > 0)
		{
			label = new wchar_t[len + 2];
			wcscpy_s(label, len + 2, newLabel);
		}
	}
	bool NeuronBase::GetInUse()
	{
		bool retVal = (label != NULL) || (synapses != NULL && synapses->size() != 0) || (synapsesFrom != NULL && synapsesFrom->size() != 0) || (model != modelType::Std);

		return retVal;
	}

	void NeuronBase::AddSynapseFrom(NeuronBase* n, float weight, SynapseBase::modelType model)
	{
		while (vectorLock.exchange(1) == 1) {}

		SynapseBase s1;
		s1.SetWeight(weight);
		s1.SetTarget(n);
		s1.SetModel(model);

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
				synapsesFrom->at(i).SetModel(model);
				goto alreadyInList;
			}
		}
		//else create a new synapse
		synapsesFrom->push_back(s1);
	alreadyInList:
		vectorLock = 0;
	}

	void NeuronBase::AddSynapse(NeuronBase* n, float weight, SynapseBase::modelType model, bool noBackPtr)
	{
		while (vectorLock.exchange(1) == 1) {}

		SynapseBase s1;
		s1.SetWeight(weight);
		s1.SetTarget(n);
		s1.SetModel(model);

		if (synapses == NULL)
		{
			synapses = new std::vector<SynapseBase>();
			synapses->reserve(100);
		}
		for (int i = 0; i < synapses->size(); i++)
		{
			if (synapses->at(i).GetTarget() == n)
			{
				//update an existing synapse
				synapses->at(i).SetWeight(weight);
				synapses->at(i).SetModel(model);
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
		s2.SetModel(model);

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
				n->synapsesFrom->at(i).SetModel(model);
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
		if (((long long)n >> 63) != 0) return;
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
		std::vector<SynapseBase> tempVec = std::vector<SynapseBase>(*synapses);
		return tempVec;
	}
	std::vector<SynapseBase> NeuronBase::GetSynapsesFrom()
	{
		if (synapsesFrom == NULL)
		{
			std::vector<SynapseBase> tempVec = std::vector<SynapseBase>();
			return tempVec;
		}
		std::vector<SynapseBase> tempVec = std::vector<SynapseBase>(*synapsesFrom);
		return tempVec;
	}
	void NeuronBase::GetLock()
	{
		while (vectorLock.exchange(1) == 1) {}
	}
	void NeuronBase::ClearLock()
	{
		vectorLock = 0;
	}

	void NeuronBase::AddToCurrentValue(float weight)
	{
		currentCharge = currentCharge + weight;
		if (currentCharge >= threshold)
			NeuronArrayBase::AddNeuronToFireList1(id);

	}

	//get a random number with a normal distribution around 
	double rand_normal(double mean, double stddev)
	{//Box muller method
		static double n2 = 0.0;
		static int n2_cached = 0;
		if (!n2_cached)
		{
			double x, y, r;
			do
			{
				x = 2.0 * rand() / RAND_MAX - 1;
				y = 2.0 * rand() / RAND_MAX - 1;

				r = x * x + y * y;
			} while (r == 0.0 || r > 1.0);
			{
				double d = sqrt(-2.0 * log(r) / r);
				double n1 = x * d;
				n2 = y * d;
				double result = n1 * stddev + mean;
				n2_cached = 1;
				return result;
			}
		}
		else
		{
			n2_cached = 0;
			return n2 * stddev + mean;
		}
	}

	//neuron firing is two-phase so that the network is independent of neuron order
	//When you call this, the neuron is added to fireList2 by the caller.
	bool NeuronBase::Fire1(long long cycle)
	{
		//a negative leakrate means "disabled"
		if (leakRate == -1 && currentCharge == 0)
		{
			lastCharge = currentCharge;
			return false;
		}
		if (model == modelType::Color)
		{
			NeuronArrayBase::AddNeuronToFireList1(id);
			return true;
		}
		//if (model == modelType::FloatValue) return false;
		if (model == modelType::Always)
		{
			if (leakRate >= 0)
				nextFiring--;
			if (leakRate >= 0 && nextFiring <= 0) 
				currentCharge = currentCharge + threshold;
			//if (leakRate >= 0)
				NeuronArrayBase::AddNeuronToFireList1(id);
		}
		if (model == modelType::Random)
		{
			if (leakRate >= 0)
				nextFiring--;
			if (leakRate >= 0 && nextFiring <= 0) //leakrate is the std.deviation
			{
				currentCharge = currentCharge + threshold;
			}
			if (leakRate >= 0) //a negative leakrate means "disabled"
				NeuronArrayBase::AddNeuronToFireList1(id);
		}
		if (model == modelType::Burst)
		{
			if (currentCharge < 0)
			{
				axonCounter = 0;
			}
			//force internal firing
			if (axonCounter > 0)
			{
				nextFiring--;
				if (nextFiring <= 0) //Firing Rate
				{
					axonCounter--;
					currentCharge = currentCharge + threshold;
					if (axonCounter > 0)
						nextFiring = (int)leakRate;
				}
				NeuronArrayBase::AddNeuronToFireList1(id);
			}
			else if (axonCounter == 0) axonCounter--;
		}

		//code to implement a refractory period
		if (cycle < lastFired + NeuronArrayBase::GetRefractoryDelay())
		{
			currentCharge = 0;
			NeuronArrayBase::AddNeuronToFireList1(id);
		}

		//check for firing
		if (model != modelType::FloatValue && currentCharge < 0)currentCharge = 0;
		if (currentCharge != lastCharge)
		{
			lastCharge = currentCharge;
			NeuronArrayBase::AddNeuronToFireList1(id);
		}

		if (model == modelType::LIF && axonCounter != 0)
		{
			axonCounter = axonCounter >> 1;
			NeuronArrayBase::AddNeuronToFireList1(id);
			if ((axonCounter & 0x001) != 0)
			{
				return true;
			}
		}

		if (currentCharge >= threshold)
		{
			if (model == modelType::LIF && axonDelay != 0)
			{
				axonCounter |= (1 << axonDelay);
				lastFired = cycle;
				currentCharge = 0;
				NeuronArrayBase::AddNeuronToFireList1(id);
				return false;
			}
			if (model == modelType::Burst && axonCounter < 0)
			{
				nextFiring = (int)leakRate;
				if (nextFiring < 1) nextFiring = 1;
				axonCounter = axonDelay - 1;
			}
			if (model == modelType::Always)
			{
				nextFiring = axonDelay;
				currentCharge = 0;
			}
			if (model == modelType::Random)
			{
				double newNormal = rand_normal((double)axonDelay, (double)leakRate);
				if (newNormal < 1) newNormal = 1;
				nextFiring = (int)newNormal;
			}
			if (model != modelType::FloatValue)
				currentCharge = 0;
			lastFired = cycle;
			return true;
		}
		if (model == modelType::LIF)
		{
			currentCharge = currentCharge * (1 - leakRate);
			NeuronArrayBase::AddNeuronToFireList1(id);
		}
		return false;
	}


	void NeuronBase::Fire2()
	{
		if (model == modelType::FloatValue) return;
		if (model == modelType::Color && lastCharge != 0)
			return;
		else if (model != modelType::Color && lastCharge < threshold && (axonCounter & 0x1) == 0)
			return; //did the neuron fire?
		NeuronArrayBase::AddNeuronToFireList1(id);
		if (synapses != NULL)
		{
			while (vectorLock.exchange(1) == 1) {} //prevent the vector of synapses from changing while we're looking at it
			for (int i = 0; i < synapses->size(); i++) //process all the synapses sourced by this neuron
			{
				SynapseBase s = synapses->at(i);
				NeuronBase* nTarget = s.GetTarget();
				if (((long long)nTarget >> 63) != 0) //does this synapse go to another server
				{
					NeuronArrayBase::remoteQueue.push(s);
				}
				else
				{	//nTarget->currentCharge += s.GetWeight(); //not supported until C++20
					auto current = nTarget->currentCharge.load(std::memory_order_relaxed);
					float desired = current + s.GetWeight();
					while (!nTarget->currentCharge.compare_exchange_weak(current, desired))
					{
						current = nTarget->currentCharge.load(std::memory_order_relaxed);
						desired = current + s.GetWeight();
					}

					//if (desired >= threshold) //this conditional improves performance but 
					//introduces a potental bug where accumulated charge might be negative
					NeuronArrayBase::AddNeuronToFireList1(nTarget->id);
				}
			}
			vectorLock = 0;
		}
	}

	void NeuronBase::HandleHebbian2Synapses()
	{
		//go through all the synapses targetd by this neuron and update the weights of Hebbian2 synapses
		if (synapsesFrom == NULL) return;

		while (vectorLock.exchange(1) == 1) {} //prevent the vector of synapses from changing while we're looking at it

		//max value is 1 over the number of incoming Hebbian2 synapses which come from neurons which are recently firing 
		float maxValue = 1;
		int numFiring = 0;
		for (int i = 0; i < synapsesFrom->size(); i++) //process all the synapses sourced by this neuron
		{
			SynapseBase s = synapsesFrom->at(i);
			NeuronBase* nTarget = s.GetTarget();
			if (s.GetModel() == SynapseBase::modelType::Hebbian2)
			{
				int deltaFiring = GetLastFired() - nTarget->GetLastFired();
				if (deltaFiring < 5)
					numFiring++;
			}
		}
		if (numFiring > 0)
		{
			maxValue = 1 / (float)numFiring;
			for (int i = 0; i < synapsesFrom->size(); i++) //process all the synapses sourced by this neuron
			{
				SynapseBase s = synapsesFrom->at(i);
				NeuronBase* nTarget = s.GetTarget();
				float newWeight = 0;
				if (s.GetModel() == SynapseBase::modelType::Hebbian2)
				{
					int deltaFiring = GetLastFired() - nTarget->GetLastFired();
					if (deltaFiring < 5)
					{
						newWeight = (s.GetWeight() + maxValue) / 2.0f;
						if (newWeight * 1.01 > maxValue) newWeight = maxValue;
						synapsesFrom->at(i).SetWeight(newWeight);
					}
					else
					{
						newWeight = (s.GetWeight() - maxValue) / 2.0f;
						if (newWeight * 1.01 < -maxValue) newWeight = -maxValue;
						synapsesFrom->at(i).SetWeight(newWeight);
					}
					//update the synapse in "To"
					for (int i = 0; i < nTarget->synapses->size(); i++)
					{
						if (nTarget->synapses->at(i).GetTarget() == this)
						{
							while (nTarget->vectorLock.exchange(1) == 1) {}
							nTarget->synapses->at(i).SetWeight(newWeight);
							nTarget->vectorLock = 0;
						}
					}
				}
			}
		}
		vectorLock = 0;
	}

	void NeuronBase::Fire3(long long cycle)
	{
		if (model == modelType::FloatValue) return;
		if (model == modelType::Color && lastCharge != 0)
			return;
		if (cycle != GetLastFired()) return;
		if (synapses != NULL)
		{
			while (vectorLock.exchange(1) == 1) {} //prevent the vector of synapses from changing while we're looking at it
			for (int i = 0; i < synapses->size(); i++) //process all the synapses sourced by this neuron
			{
				SynapseBase s = synapses->at(i);
				NeuronBase* nTarget = s.GetTarget();
				if (s.GetModel() == SynapseBase::modelType::Hebbian3)
				{
					int deltaFiring = GetLastFired() - nTarget->GetLastFired();
					if (deltaFiring > 0 && deltaFiring < 4)
					{
						//target fired first, decrease the weight
						float currentWeight = s.GetWeight();
						float pctChange = .5f / deltaFiring;
						float newWeight = currentWeight - currentWeight * pctChange;
						if (newWeight < .001) newWeight = .001;
						synapses->at(i).SetWeight(newWeight);
						//update the reverse entry
						for (int j = 0; j < nTarget->synapsesFrom->size(); j++)
						{
							if (nTarget->synapsesFrom->at(j).GetTarget() == this)
								nTarget->synapsesFrom->at(j).SetWeight(newWeight);
						}
					}
				}

			}
			vectorLock = 0;
		}
		if (synapsesFrom != NULL)
		{
			HandleHebbian2Synapses();
			while (vectorLock.exchange(1) == 1) {} //prevent the vector of synapses from changing while we're looking at it
			for (int i = 0; i < synapsesFrom->size(); i++) //process all the synapses sourced by this neuron
			{
				SynapseBase s = synapsesFrom->at(i);
				NeuronBase* nTarget = s.GetTarget();
				if (s.GetModel() == SynapseBase::modelType::Hebbian3)
				{
					int deltaFiring = GetLastFired() - nTarget->GetLastFired();
					if (deltaFiring > 0 && deltaFiring < 4)
					{
						//Source fired first, increase the weight
						float currentWeight = s.GetWeight();
						float pctChange = .5f / deltaFiring;
						float newWeight = currentWeight / (1 - pctChange);
						if (newWeight > 1) newWeight = 1;
						synapsesFrom->at(i).SetWeight(newWeight);
						//update the reverse entry
						for (int j = 0; j < nTarget->synapses->size(); j++)
						{
							if (nTarget->synapses->at(j).GetTarget() == this)
								nTarget->synapses->at(j).SetWeight(newWeight);
						}
					}
				}
			}
			vectorLock = 0;
		}

	}



}

//another way of handling hebbian synapses
//it workes from the target so it can weaken synapses from neurons which don't fire
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
