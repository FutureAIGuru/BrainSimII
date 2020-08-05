using System.Collections.Generic;
using System.Threading.Tasks;
namespace BrainSimulator {
    public class NeuronArrayBase{
        public int arraySize = 10000; // ten thousand neurons to start
        public int rows = 100;
        public int Cols { get => arraySize / rows; }
        public NeuronBase[] baseNeuronArray;
        //Variables for the Neuron Firing Queue
        static int queuesize = 10000000;
        //how many parallel tasks and the array to hold them
        const int taskCount = 120;
        List<int> firingQueue = new List<int>(queuesize);
        List<List<int>> nextQueue = new List<List<int>>(taskCount);
        List<List<int>> zeroQueue = new List<List<int>>(taskCount);
        List<List<int>> firedQueue = new List<List<int>>(taskCount);
        List<int> manualFire = new List<int>();

        public long Generation = 0;
        public void Initialize(int theSize, int theRows, NeuronBase.baseModelType t = NeuronBase.baseModelType.Std)
        {
            arraySize = theSize;
            rows = theRows;
            baseNeuronArray = new NeuronBase[arraySize];
            for (int i = 0; i < arraySize; i++)
                baseNeuronArray[i] = new NeuronBase(i, t);
        }
        public int GetInterimNeuronCount() { return arraySize; }
        public NeuronBase GetNeuron(int i) { return baseNeuronArray[i]; }
        public void SetNeuron(int i, NeuronBase n) { baseNeuronArray[i] = n; }
        public int GetArraySize() { return arraySize; }

        public void AddToFiringQueue(int neuronID)
        {
            manualFire.Add(neuronID);
        }
        public int lastFireCount = 0;

        void ProcessFiringQueue(int id)
        {    //determine the portion of the firing queue this thread should handle
            int taskID = id;
            int start, end;
            GetBounds(taskID, out start, out end);
            //first-phase neuron processing
            List<int> queue = firedQueue[taskID];
            for (int i = start; i < end; i++)
            {
                GetNeuron(firingQueue[i]).Fire2(this, queue);
            }
        }
        void ProcessFiringQueue1(int id)
        {
            int taskID = id;
            int start, end;
            GetBounds(taskID, out start, out end);
            List<int> queue1 = nextQueue[taskID];
            List<int> queue2 = zeroQueue[taskID];
            for (int i = start; i < end; i++)
            {
                GetNeuron(firingQueue[i]).Fire1(this, queue1, queue2);
            }
        }
        private void GetBounds(int taskID, out int start, out int end)
        {
            int numberToProcess = firingQueue.Count / taskCount;
            int remainder = firingQueue.Count % taskCount;
            start = numberToProcess * taskID;
            end = start + numberToProcess;
            if (taskID < remainder)
            {
                start += taskID;
                end = start + numberToProcess + 1;
            }
            else
            {
                start += remainder;
                end += remainder;
            }
        }
        public void Fire()
        {
            Generation++;
            if (nextQueue.Count == 0)
            {
                for (int i = 0; i < taskCount; i++)
                {
                    nextQueue.Add(new List<int>(queuesize / taskCount));
                    firedQueue.Add(new List<int>(queuesize / taskCount));
                    zeroQueue.Add(new List<int>(queuesize / taskCount));
                }
            }
            firingQueue.Clear();
            firingQueue.AddRange(manualFire);
            manualFire.Clear();
            for (int i = 0; i < taskCount; i++)
            {
                firingQueue.AddRange(nextQueue[i]);
                nextQueue[i].Clear();
                firingQueue.AddRange(firedQueue[i]);
                firedQueue[i].Clear();
                firingQueue.AddRange(zeroQueue[i]);
                zeroQueue[i].Clear();
            }//spin off the paralel tasks to run the engine on the firing queue
            ParallelOptions x = new ParallelOptions { MaxDegreeOfParallelism = 128 };
            Parallel.For(0, taskCount, x, i => ProcessFiringQueue(i));
            Parallel.For(0, taskCount, x, i => ProcessFiringQueue1(i));
        }
    }
}
