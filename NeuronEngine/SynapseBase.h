#pragma once

namespace NeuronEngine { class NeuronBase; }

namespace NeuronEngine
{
	class  __declspec(dllexport) SynapseBase
	{
	public:
		void SetTarget(NeuronBase * target);
		NeuronBase *GetTarget();
		float GetWeight();
		void SetWeight(float value);
		void SetIsHebbian(bool value);
		bool IsHebbian();

	private:
		NeuronBase* targetNeuron = 0; //pointer to the target neuron
		float weight = 0; //weight of the synapse
		bool isHebbian = false; //can the synapse adjust its own weight
	};
}
