using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleHebbian : ModuleBase
    {
        //we can optionally work with clock time stamps so things work regardless of how fast the engine is running
        public bool bUseClockTime = false;
        public int maxSecondsOfJitter = 2;
        public int maxCyclesOfJitter = 2;
        DateTime[,] lastFiringTime = null;
        long[,] lastFiringGeneration = null;
        bool bDoAnd = true;

        enum cols { in1, out1, in1D, and, out2, in2 };


        bool bCantInitialize = false;
        public bool Fired(int i, int j)
        {
            bool fired = false;
            if (!bUseClockTime && theNeuronArray.Generation - lastFiringGeneration[i, j] < maxCyclesOfJitter)
                fired = true; ;
            if (bUseClockTime && DateTime.Now.Subtract(lastFiringTime[i, j]).TotalSeconds < maxSecondsOfJitter)
                fired = true; ;
            return fired;
        }
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (bCantInitialize) return;

            //Count the number of neurons which fired so we can shortcut out.
            int[] fireCount = new int[na.Width];
            for (int col = 0; col < 2; col++)
                for (int j = 0; j < na.Height; j++)
                {
                    int i = (int)cols.in1;
                    if (col != 0) i = (int)cols.in2;
                    Neuron n = na.GetNeuronAt(i, j);
                    if (n.LastCharge > .90)
                    {
                        lastFiringGeneration[i, j] = theNeuronArray.Generation;
                        lastFiringTime[i, j] = DateTime.Now;
                    }
                    if (Fired(i, j))
                        fireCount[i]++;
                }
            if (fireCount[(int)cols.in1] == 0 || fireCount[(int)cols.in2] == 0) return;

            for (int j = 0; j < na.Height; j++)
            {
                for (int l = 0; l < na.Height; l++)
                {
                    float delta = 0;
                    if (Fired((int)cols.in1, j) || Fired((int)cols.in2, l))
                    {
                        if (Fired((int)cols.in1, j))
                            na.GetNeuronAt((int)cols.in1, j).AddSynapse(na.GetNeuronAt((int)cols.in1D, j).Id, 1, theNeuronArray, false);
                        if (Fired((int)cols.in1, j) && Fired((int)cols.in2, l))
                        {
                            delta = 1;
                        }
                        else
                        {
                            delta = -.2f;
                        }

                        //add the synapse from the first to the output of the second
                        Neuron source = na.GetNeuronAt((int)cols.in1D, j);
                        Neuron target = na.GetNeuronAt((int)cols.out2, l);
                        Synapse s1 = source.FindSynapse(target.Id);
                        float newWeight = delta;
                        if (s1 != null)
                        {
                            newWeight = s1.Weight + delta;
                            newWeight = (float)Math.Round(newWeight, 3); //don't know why this is needed but roundoffs occur otherwise
                        }
                        source.AddSynapse(target.Id, newWeight, theNeuronArray, false);

                        //add the synapse from the second to the output of the first
                        source = na.GetNeuronAt((int)cols.in2, l);
                        target = na.GetNeuronAt((int)cols.out1, j);
                        s1 = source.FindSynapse(target.Id);
                        newWeight = delta;
                        if (s1 != null)
                        {
                            newWeight = s1.Weight + delta;
                            newWeight = (float)Math.Round(newWeight, 3);
                        }
                        source.AddSynapse(target.Id, newWeight, theNeuronArray, false);

                    }
                }
            }

            //handle the AND case
            if (!bDoAnd || fireCount[(int)cols.in1] < 2) return;
            for (
                int l = 0; l < na.Height; l++)
            {
                if (Fired((int)cols.in2, l)) //if this neuron fired...connect it to any which fired recently 
                {
                    for (int j = 0; j < na.Height; j++)
                    {
                        //add the synapses
                        Neuron source = na.GetNeuronAt((int)cols.in1, j);
                        Neuron target = na.GetNeuronAt((int)cols.and, l);
                        if (Fired((int)cols.in1, j))
                        {
                            Synapse s1 = source.FindSynapse(target.Id);
                            float newWeight = .6f;
                            if (s1 != null)
                            {
                                newWeight = s1.Weight + .1f;
                                newWeight = (float)Math.Round(newWeight, 3);
                                if (newWeight > 0.6f) newWeight = 0.6f;
                            }

                            source.AddSynapse(target.Id, newWeight, theNeuronArray, false);
                            target.AddSynapse(na.GetNeuronAt((int)cols.out2, l).Id, 1, theNeuronArray, false);
                            //suppress the individual output
                            Neuron sourceD = na.GetNeuronAt((int)cols.in1D, j);
                            float weight = 0;
                            if (sourceD.synapses.Count > 0)
                                weight = sourceD.synapses.Max(t => t.Weight);
                            if (weight > 0)
                            {
                                foreach (Synapse s in sourceD.synapses)
                                {
                                    if (s.Weight == weight)
                                    {
                                        target.AddSynapse(s.TargetNeuron, -1, theNeuronArray, false);
                                    }
                                }
                            }

                        }
                        else
                        {
                            Synapse s1 = source.FindSynapse(target.Id);
                            float newWeight = -.2f;
                            if (s1 != null)
                            {
                                newWeight = s1.Weight - .2f;
                                newWeight = (float)Math.Round(newWeight, 3);
                            }
                            source.AddSynapse(target.Id, newWeight, theNeuronArray, false);
                        }
                    }
                }
            }
        }

        public override void Initialize()
        {
            //set up the last-firing array
            lastFiringTime = new DateTime[na.Width, na.Height];
            bCantInitialize = false;
            var colCount = Enum.GetNames(typeof(cols)).Length;
            if (na.Width != colCount)
            {
                System.Windows.Forms.MessageBox.Show("Hebbian Module must have " + colCount + " columns");
                bCantInitialize = true;
                return;
            }
            lastFiringGeneration = new long[na.Width, na.Height];
            for (int i = 0; i < na.Width; i++)
            {
                na.GetNeuronAt(i, 0).Label = Enum.GetName(typeof(cols), i).ToString();
            }

            for (int j = 0; j < na.Height; j++)
            {
                Neuron n = na.GetNeuronAt((int)cols.in1, j);
                n.NeuronModel = Neuron.modelType.std;
                n.DeleteAllSynapes();

                n = na.GetNeuronAt((int)cols.in1D, j);
                n.NeuronModel = Neuron.modelType.hebbian;
                n.DeleteAllSynapes();

                n = na.GetNeuronAt((int)cols.in2, j);
                n.NeuronModel = Neuron.modelType.hebbian;
                n.DeleteAllSynapes();

                n = na.GetNeuronAt((int)cols.and, j);
                n.DeleteAllSynapes();
                n.NeuronModel = Neuron.modelType.oneTime;
            }
        }
    }


}
