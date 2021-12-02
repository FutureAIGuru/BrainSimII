#include "pch.h"


#include "SynapseBase.h"
#include "NeuronBase.h"

namespace NeuronEngine
{
	NeuronBase* SynapseBase::GetTarget()
	{
		return targetNeuron;
	}
	void SynapseBase::SetTarget(NeuronBase* target)
	{
		targetNeuron = target;
	}
	float SynapseBase::GetWeight()
	{
		return weight;
	}
	void SynapseBase::SetWeight(float value)
	{
		weight = value;
	}

	SynapseBase::modelType SynapseBase::GetModel()
	{
		return model;
	}
	void SynapseBase::SetModel(SynapseBase::modelType value)
	{
		model = value;
	}
}
