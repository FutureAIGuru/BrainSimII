using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleHebbianAND : ModuleBase
    {
        //DateTime[,] lastFiring = null;
        long[,] lastFiring = null;
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

            //we can optionally work with clock time stamps so things work regardless of how fast the engine is running
            int[] fireCount = new int[na.Width];
            for (int i = 0; i < na.Width; i += 2)
                for (int j = 0; j < na.Height; j++)
                {
                    Neuron n = na.GetNeuronAt(i, j);
                    if (n.LastCharge > .90) //if this neuron fired...connect it to any which fired recently 
                    {
                        fireCount[i]++;
                        lastFiring[i, j] = theNeuronArray.Generation;
                    }
                    if (theNeuronArray.Generation - lastFiring[i, j] < 2)
                        fireCount[i]++;
                }
            int xx = fireCount.Count(t => t != 0);
            int yy = fireCount.Count(t => t > 1 );
            if (xx < 2 || yy < 1) return;

            for (int i = 0; i < na.Width; i += 2)
                for (int j = 0; j < na.Height; j++)
                {
                    Neuron n = na.GetNeuronAt(i, j);
                    if (n.LastCharge > .90) //if this neuron fired...connect it to any which fired recently 
                    {
                        //DateTime currentFiring = DateTime.Now;
                        //lastFiring[i, j] = currentFiring;
                        for (int k = 0; k < na.Width; k += 2)
                            for (int l = 0; l < na.Height; l++)
                                if (k != i)
                                {
                                    float delta = 0;
                                    //TimeSpan ts = currentFiring.Subtract(lastFiring[k, l]);
                                    //if (ts.TotalSeconds < 15)
                                    if (theNeuronArray.Generation - lastFiring[k, l] < 2)
                                    {
                                        delta = 1.0f / fireCount[i];
                                    }
                                    else
                                    {
                                        delta = -.1f;
                                    }
                                    //add the synapse from the first to the output of the second
                                    //Neuron source = na.GetNeuronAt(i, j);
                                    //Neuron target = na.GetNeuronAt(k + 1, l);
                                    //Synapse s1 = source.FindSynapse(target.Id);
                                    //float newWeight = delta;
                                    //if (s1 != null)
                                    //{
                                    //    newWeight = s1.Weight + delta;
                                    //    newWeight = (float)Math.Round(newWeight, 3); //don't know why this is needed but roundoffs occur otherwise
                                    //}
                                    ////if (newWeight >= 0)
                                    //source.AddSynapse(target.Id, newWeight, theNeuronArray, false);

                                    //add the synapse from the second to the output of the first
                                    Neuron source = na.GetNeuronAt(k, l);
                                    Neuron target = na.GetNeuronAt(i + 1, j);
                                    Synapse s1 = source.FindSynapse(target.Id);
                                    float newWeight = delta;
                                    if (s1 != null)
                                    {
                                        newWeight = s1.Weight + delta;
                                        newWeight = (float)Math.Round(newWeight, 3);
                                    }
                                    //                                    if (newWeight >= 0)
                                    source.AddSynapse(target.Id, newWeight, theNeuronArray, false);

                                }
                    }
                }
        }


        public override void Initialize()
        {
            //set up the last-firing array
            if (lastFiring == null)
            {
                //lastFiring = new DateTime[na.Width, na.Height];
                lastFiring = new long[na.Width, na.Height];
            }
            for (int i = 0; i < na.Width; i += 2)
                for (int j = 0; j < na.Height; j++)
                {
                    Neuron n = na.GetNeuronAt(i, j);
                    n.Model = Neuron.modelType.Std;
                    n.DeleteAllSynapes();
                    na.GetNeuronAt(i + 1, j).Model = Neuron.modelType.OneTime;
                }

        }
    }


}
