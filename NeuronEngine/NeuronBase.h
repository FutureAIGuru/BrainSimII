#pragma once

#include <string>
#include <vector>
#include <atomic>
#include "SynapseBase.h"


namespace NeuronEngine { class SynapseBase; }
namespace NeuronEngine { class NeuronArrayBase; }

namespace NeuronEngine
{
	class NeuronBase
	{
	public:
		enum class modelType {Std,Color,FloatValue,LIF,Random,Burst,Always};

		//the ending value of a neuron 
		float lastCharge = 0;

		//an empty vector takes up memory so this is a pointer to the vector which is allocated only if needed
		std::vector<SynapseBase>* synapses = NULL;

	private:
		//the accumulating value of a neuron
		std::atomic<float> currentCharge = 0;

		modelType model = modelType::Std;
		
		float leakRate = 0.1f; //used only by LIF model
		int nextFiring = 0; //used only by Random model && continuous model
		long long lastFired = 0; //timestamp of last firing
		int id = -1; //an illegal value which will trap
		wchar_t* label = NULL;
		int axonDelay = 0;
		int axonCounter = 0;
		
		std::vector<SynapseBase>* synapsesFrom = NULL;

		//this is a roll-your-own mutex because mutex doesn't exist in CLI code and causes compile fails
		std::atomic<int> vectorLock = 0;
		//std::mutex aLock;
		

	private:
		const float  threshold = 1.0f;


	public:
		__declspec(dllexport)  NeuronBase(int ID);
		__declspec(dllexport)  ~NeuronBase();

		__declspec(dllexport)  int GetId();
		__declspec(dllexport)  modelType GetModel();
		__declspec(dllexport)  void SetModel(modelType value);
		__declspec(dllexport)  float GetLastCharge();
		__declspec(dllexport)  void SetLastCharge(float value);
		__declspec(dllexport)  float GetCurrentCharge();
		__declspec(dllexport)  void SetCurrentCharge(float value);

		__declspec(dllexport)  void AddSynapse(NeuronBase* n, float weight, SynapseBase::modelType model = SynapseBase::modelType::Fixed, bool noBackPtr = true);
		__declspec(dllexport)  void AddSynapseFrom(NeuronBase* n, float weight, SynapseBase::modelType model = SynapseBase::modelType::Fixed);
		__declspec(dllexport)  void DeleteSynapse(NeuronBase* n);
		__declspec(dllexport)  void GetLock();
		__declspec(dllexport)  void ClearLock();
		__declspec(dllexport)  std::vector<SynapseBase> GetSynapses();
		__declspec(dllexport)  std::vector<SynapseBase> GetSynapsesFrom();
		__declspec(dllexport)  int GetSynapseCount();

		__declspec(dllexport)  bool GetInUse();
		__declspec(dllexport)  wchar_t* GetLabel();
		__declspec(dllexport)  void SetLabel(const wchar_t*);


		__declspec(dllexport)  float GetLeakRate();
		__declspec(dllexport)  void SetLeakRate(float value);
		__declspec(dllexport)  int GetAxonDelay();
		__declspec(dllexport)  void SetAxonDelay(int value);
		__declspec(dllexport)  long long GetLastFired();

		__declspec(dllexport)  void AddToCurrentValue(float weight);

		__declspec(dllexport)  bool Fire1(long long generation);
		void Fire2();
		void Fire3();

		float NewHebbianWeight(float y, float offset, SynapseBase::modelType model, int numberOfSynapses);

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

