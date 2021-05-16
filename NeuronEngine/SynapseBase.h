#pragma once

namespace NeuronEngine { class NeuronBase; }

namespace NeuronEngine
{
	class  __declspec(dllexport) SynapseBase
	{
	public:
		enum class modelType { Fixed, Binary, Hebbian1, Hebbian2,Hebbian3};

		void SetTarget(NeuronBase * target);
		NeuronBase* GetTarget();
		float GetWeight();
		void SetWeight(float value);
		void SetModel(modelType value);
		modelType GetModel();

	private:
		NeuronBase* targetNeuron = 0; //pointer to the target neuron
		float weight = 0; //weight of the synapse
		modelType model = modelType::Fixed;
	};
}
