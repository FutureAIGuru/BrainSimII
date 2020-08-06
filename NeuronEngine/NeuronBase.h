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
		//an empty vector takes up memory so this is a pointer to the vector which is allocated if needed
		std::vector<SynapseBase>* synapses = NULL; 

		//the accumulating value of a neuron
		std::atomic<float> currentCharge = 0;

		//the ending value of a neuron 
		float lastCharge = 0;

		modelType model = modelType::Std;
		
		float leakRate = 0.1f; //used only by LIF model
		int nextFiring = 0; //used only by Random model && continuous model
		long long lastFired = 0; //timestamp of last firing
		int id;
		wchar_t* label = NULL;
		
		std::vector<SynapseBase>* synapsesFrom = NULL;

		//this is a roll-your-own mutex because mutex doesn't exist in CLI code and causes compile fails
		std::atomic<int> vectorLock = 0;
		//std::mutex aLock;
		

	private:
		const float  threshold = 1.0f;


	public:
		NeuronBase(int ID);
		~NeuronBase();

		int GetId();
		modelType GetModel();
		void SetModel(modelType value);
		float GetLastCharge();
		void SetLastCharge(float value);
		float GetCurrentCharge();
		void SetCurrentCharge(float value);

		void AddSynapse(NeuronBase* n, float weight, bool isHebbian = false, bool noBackPtr = true);
		void DeleteSynapse(NeuronBase* n);
		std::vector<SynapseBase> GetSynapses();
		std::vector<SynapseBase> GetSynapsesFrom();
		int GetSynapseCount();

		wchar_t* GetLabel();
		void SetLabel(const wchar_t*);

		bool InUse();

		float GetLeakRate();
		void SetLeakRate(float value);
		long long GetLastFired();

		void Fire2();
		bool Fire1(long long generation);


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

