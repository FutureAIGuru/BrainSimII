#pragma once

//C# TO C++ CONVERTER NOTE: Forward class declarations:
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
		NeuronBase* targetNeuron; //this is used by the engine
		float weight = 0; //this is used by the engine
		bool isHebbian = false;
	};
}
