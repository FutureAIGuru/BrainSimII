//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

namespace BrainSimulator.Modules
{
    public class Module2DSmell : ModuleBase
    {
        private float average = 0;
        private bool increasing = false;
        private float last0, last1;

        public Module2DSmell()
        {
            minHeight = 1;
            minWidth = 4;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            float a0 = GetNeuronValue(null, 0, 0);
            float a1 = GetNeuronValue(null, 1, 0);

            if (a0 == last0 && a1 == last1) return;

            last0 = a0;
            last1 = a1;
            float ave = (a0 + a1) / 2;
            float diff = a0 - a1;
            if (ave > average)
            {
                increasing = true;
                SetNeuronValue(null, 2, 0, 1);
                SetNeuronValue(null, 3, 0, diff);
            }
            else
            {
                increasing = false;
                SetNeuronValue(null, 2, 0, 0);
                SetNeuronValue(null, 3, 0, diff);
            }

            average = ave;
            increasing = !increasing; //we might use this soon
        }

        public override void Initialize()
        {
            mv.GetNeuronAt(0, 0).Model = Neuron.modelType.FloatValue;
            mv.GetNeuronAt(1, 0).Model = Neuron.modelType.FloatValue;
            mv.GetNeuronAt(3, 0).Model = Neuron.modelType.FloatValue;
        }
    }
}
