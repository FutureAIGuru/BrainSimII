using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    class FiringHistory
    {
        public class NeuronHistory
        {
            public int NeuronID;
            public List<long> Samples = new List<long>();
        }
        public static List<NeuronHistory> history = new List<NeuronHistory>();

        static int MaxSamples = 1000;
        
        public static long EarliestValue()
        {
            long retVal = long.MaxValue;
            for (int i = 0; i < history.Count; i++)
            {
                if (history[i].Samples.Count > 0)       
                  retVal = Math.Min(retVal, history[i].Samples[0]);
            }
            return retVal;
        }


        public static void AddFiring(int NeuronID, long TimeStamp)
        {
            for (int i = 0; i < history.Count; i++)
            {
                if (NeuronID == history[i].NeuronID)
                {
                    if (history[i].Samples.Count > MaxSamples)
                        history[i].Samples.RemoveAt(0);
                    history[i].Samples.Add(TimeStamp);
                    return;
                }
            }
            history.Add(new NeuronHistory { NeuronID = NeuronID, });
            AddFiring(NeuronID, TimeStamp);
        }

        public static void Clear()
        {
            for (int i = 0; i < history.Count; i++)
            {
                history[i].Samples.Clear();
            }
        }

        public static void DeleteHistory(int NeuronID)
        {
            for (int i = 0; i < history.Count; i++)
            {
                if (NeuronID == history[i].NeuronID)
                {
                    history.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
