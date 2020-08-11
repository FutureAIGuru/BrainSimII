using System;
using System.Collections.Generic;

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

        public static bool NeuronIsInFiringHistory(int id)
        {
            for (int i = 0; i < history.Count; i++)
            {
                if (history[i].NeuronID == id)
                {
                    return true;
                }
            }
            return false;
        }
        public static void AddNeuronToHistoryWindow(int id)
        {
            if (NeuronIsInFiringHistory(id)) return;
            NeuronHistory entry = new NeuronHistory();
            entry.NeuronID = id;
            history.Add(entry);
        }
        public static void RemoveNeuronFromHistoryWindow(int id)
        {
            for (int i = 0; i < history.Count; i++)
            {
                if (history[i].NeuronID == id)
                {
                    history.RemoveAt(i);
                    return;
                }
            }
        }

        public static void UpdateFiringHistory()
        {
            foreach (NeuronHistory active in history)
            {
                float lastCharge = MainWindow.theNeuronArray.GetNeuronLastCharge(active.NeuronID);
                if (lastCharge >= 1)
                {
                    active.Samples.Add(MainWindow.theNeuronArray.Generation);
                }
            }
        }

        public static void Clear()
        {
            for (int i = 0; i < history.Count; i++)
            {
                history[i].Samples.Clear();
            }
        }
    }
}
