#pragma once

#include <string>
#include <vector>
#include <atomic>

namespace NeuronEngine { class SynapseBase; }
namespace NeuronEngine { class NeuronArrayBase; }

namespace NeuronEngine
{
	class __declspec(dllexport)  NeuronBase
	{
	public:
		enum class modelType {Std,Color,FloatValue,LIF,Random};

	private:
		//an empty vector takes up memory so this is a pointer to the vector which is allocated
		std::vector<SynapseBase> *synapses = NULL; // = new List<Synapse>();
		//the accumulating value of a neuron
		std::atomic<float> currentCharge = 0;
		//the ending value of a neuron 
		float lastCharge = 0;

		modelType model = modelType::Std;
		float leakRate = 0.1f; //used only by LIF model
		long nextFiring = 0; //used only by Random model
		long lastFired = 0;
		int id;

	private:
		const float  threshold = 1.0f;

	public:
		NeuronBase(int ID);
		~NeuronBase();

		int GetId();
		modelType GetModel();
		void SetModel(modelType value);
		float GetLastCharge();
		float GetCurrentCharge();
		void SetLastCharge(float value);

		void AddSynapse(NeuronBase* n, float weight,bool isHebbian=false);
		void DeleteSynapse(NeuronBase* n,float weight = 0);
		std::vector<SynapseBase>* GetSynapses();

		float GetLeakRate();
		void SetLeakRate(float value);
		long GetLastFired();

		void Fire1NoQ();
		bool Fire2NoQ(long generation);


		NeuronBase(const NeuronBase& t)
		{
			model = t.model;
			id = t.id;
			leakRate = t.leakRate;
		}
		NeuronBase& operator = (const NeuronBase& t)
		{
			return *this;
		}
	};
}

