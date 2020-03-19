//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class ModuleBehavior : ModuleBase
    {
        public override string ShortDescription { get => "Handles a queue of behaviors"; }
        public override string LongDescription
        {
            get =>
                "This module has primitives of Move and Turn behaviors and allows them to be queued into sequences. " +
                "A sequence can be cancelled in the event of collision or other issue. By firing various input neurons " +
                "the module may query the Model to decide where to turn.";
        }

        public ModuleBehavior()
        {
            minWidth = 11;
            minHeight = 1;
        }

        public enum TheBehavior { Rest, Move, Turn };
        public class behavior
        {
            public TheBehavior theBehavior;
            public float param1;
        }

        List<behavior> pending = new List<behavior>();

        public bool IsIdle()
        {
            return (pending.Count == 0);
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            try
            {
                if (GetNeuronValue(null, "Stop") == 1) Stop();
                if (GetNeuronValue(null, "TurnTo") == 1) TurnTo();
                if (GetNeuronValue(null, "MoveTo") == 1) MoveTo();
                if (GetNeuronValue(null, "Scan") == 1) Scan();
                if (GetNeuronValue(null, "Coll") == 1) Collision();
            }
            catch { return; }

            if (pending.Count != 0)
            {
                behavior currentBehavior = pending[0];
                pending.RemoveAt(0);
                switch (currentBehavior.theBehavior)
                {
                    case TheBehavior.Rest: break;
                    case TheBehavior.Move:
                        SetNeuronValue("ModuleMove", 0, 2, currentBehavior.param1);
                        break;
                    case TheBehavior.Turn:
                        SetNeuronValue("ModuleTurn", 2, 0, currentBehavior.param1);
                        break;
                }
            }
            if (pending.Count == 0)
            {
                SetNeuronValue(null, "Done", 1);
            }

        }
        public override void Initialize()
        {
            pending.Clear();
            na.GetNeuronAt(0, 0).Label = "Stop";
            na.GetNeuronAt(1, 0).Label = "Done";
            na.GetNeuronAt(2, 0).Label = "TurnTo";
            na.GetNeuronAt(3, 0).Model = Neuron.modelType.FloatValue;
            na.GetNeuronAt(3, 0).Label = "Theta";
            na.GetNeuronAt(4, 0).Label = "MoveTo";
            na.GetNeuronAt(5, 0).Model = Neuron.modelType.FloatValue;
            na.GetNeuronAt(5, 0).Label = "R";
            na.GetNeuronAt(6, 0).Label = "Scan";
            na.GetNeuronAt(9, 0).Label = "Coll";
            na.GetNeuronAt(10, 0).Label = "CollAngle";
            na.GetNeuronAt(10, 0).Model = Neuron.modelType.FloatValue;

            //Connect Neurons to the KB
            Neuron nKBDone = GetNeuron("Module2DKB", "Done");
            if (nKBDone != null)
                na.GetNeuronAt("Done").AddSynapse(nKBDone.Id, 1);
            Neuron nKBStop = GetNeuron("KBOut", "Stop");
            if (nKBStop != null)
                nKBStop.AddSynapse(na.GetNeuronAt("Stop").Id, 1);

        }

        public override void ShowDialog() //delete this function if it isn't needed
        {
            base.ShowDialog();
        }

        //Several Behaviors...

        public void Stop()
        {
            pending.Clear();
            SetNeuronValue(null, "Done", 1);
        }

        //Random (not currently used)
        public void RandomBehavior()
        {
            //lowest priority...only do this if nothing else is pending
            if (pending.Count > 0) return;
            double x = new Random().NextDouble();

            behavior newBehavoir = new behavior()
            {
                theBehavior = TheBehavior.Turn
            };
            newBehavoir.param1 = -(float)Math.PI / 6;
            if (x < .925) newBehavoir.param1 = -(float)Math.PI / 12;
            else if (x < .95) newBehavoir.param1 = -(float)Math.PI / 24;
            else if (x < .975) newBehavoir.param1 = (float)Math.PI / 24;
            else if (x < 1) newBehavoir.param1 = (float)Math.PI / 12;

            pending.Add(newBehavoir);
        }

        public void Scan()
        {
            SetNeuronValue(null, "Done", 0); 

            TurnTo((float)Math.PI / 24);
            TurnTo((float)Math.PI / 24);
            TurnTo((float)Math.PI / 24);
            TurnTo((float)Math.PI / -24);
            TurnTo((float)Math.PI / -24);
            TurnTo((float)Math.PI / -24);
            TurnTo((float)Math.PI / -24);
            TurnTo((float)Math.PI / -24);
            TurnTo((float)Math.PI / -24);
            TurnTo((float)Math.PI / 24);
            TurnTo((float)Math.PI / 24);
            TurnTo((float)Math.PI / 24);
        }

        public bool IsMoving()
        {
            return pending.Count > 0 && pending[0].theBehavior == TheBehavior.Move;
        }

        private void Collision()
        {
            //SetNeuronValue(null, "Done", 0);
            //pending.Clear();
            //float collisionAngle = na.GetNeuronAt("CollAngle").CurrentCharge;
            //TurnTo(-collisionAngle - (float)Math.PI / 2);
            //MoveTo(.2f);
            //TurnTo(+collisionAngle + (float)Math.PI / 2);
        }

        //TurnTo
        public void TurnTo()
        {
            if (pending.Count > 0) return;
            float theta = GetNeuronValue(null, "Theta");
            if (theta == 0) return;
            TurnTo(theta);
        }
        public void TurnTo(float theta)
        {
            SetNeuronValue(null, "Done", 0);

            //don't bother turing more than 180-degrees, turn the other way
            while (theta > Math.PI) theta -= (float)Math.PI * 2;
            while (theta < -Math.PI) theta += (float)Math.PI * 2;
            float deltaTheta = (float)Math.PI / 6;

            while (Math.Abs(theta) > 0.001)
            {
                float theta1 = 0;
                if (theta > 0)
                {
                    if (theta > deltaTheta) theta1 = deltaTheta;
                    else theta1 = theta;
                    theta = theta - theta1;
                }
                else
                {
                    if (theta < -deltaTheta) theta1 = -deltaTheta;
                    else theta1 = theta;
                    theta = theta - theta1;
                }
                behavior newBehavior = new behavior()
                {
                    theBehavior = TheBehavior.Turn,
                    param1 = theta1
                };
                pending.Add(newBehavior);
            }
        }

        //MoveTo
        private void MoveTo()
        {
            float dist = na.GetNeuronAt("R").CurrentCharge;
            SetNeuronValue(null, "MoveTo", 0);
            if (dist <= 0) return;
            MoveTo(dist);
        }
        public void MoveTo(float dist)
        {
            SetNeuronValue(null, "Done", 0);
                        
            while (Math.Abs(dist) > 0.001)
            {
                behavior newBehavior = new behavior(){theBehavior= TheBehavior.Rest};
                pending.Add(newBehavior);
                pending.Add(newBehavior);
                float dist1 = 0;
                if (dist > .2f) dist1 = .2f;
                else dist1 = dist;
                dist = dist - dist1;
                newBehavior  = new behavior()
                {
                    theBehavior = TheBehavior.Move,
                    param1 = dist1
                };
                pending.Add(newBehavior);
            }
        }
    }
}
