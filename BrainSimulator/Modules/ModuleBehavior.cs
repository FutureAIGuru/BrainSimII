using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleBehavior : ModuleBase
    {
        //NOT USED YET
        public enum TheBehavior { Rest, Move, Turn };
        public class behavior
        {
            public TheBehavior theBehavior;
            public float param1;
        }

        List<behavior> pending = new List<behavior>();

        bool backing = false;
        public bool IsIdle()
        {
            return (pending.Count == 0);
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            try
            {
                if (na.GetNeuronAt("Stop").LastCharge == 1) Stop();
                if (na.GetNeuronAt("TurnTo").LastCharge == 1) TurnTo();
                if (na.GetNeuronAt("MoveTo").LastCharge == 1) MoveTo();
                if (na.GetNeuronAt("Scan").LastCharge == 1) Scan();
                if (na.GetNeuronAt("Conf").LastCharge == 1) Conf();
                if (na.GetNeuronAt("EndPt").LastCharge == 1) ConfEnd();
                if (na.GetNeuronAt("Coll").LastCharge == 1) Collision();
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
                        NeuronArea naMove = theNeuronArray.FindAreaByLabel("ModuleMove");
                        naMove.GetNeuronAt(0, 2).SetValue(currentBehavior.param1);
                        break;
                    case TheBehavior.Turn:
                        NeuronArea naTurn = theNeuronArray.FindAreaByLabel("ModuleTurn");
                        naTurn.GetNeuronAt(2, 0).SetValue(currentBehavior.param1);
                        break;
                }
                if (pending.Count == 0)
                {
                    handlingConf = false;
                    na.GetNeuronAt("Done").CurrentCharge = 1;
                    NeuronArea naEntity = theNeuronArray.FindAreaByLabel("ModuleEntity");
                    try
                    {
                        naEntity.GetNeuronAt("Idle").SetValue(1);
                        //naEntity.GetNeuronAt("Collision").SetValue(0);
                        na.GetNeuronAt(2, 0).SetValue(0);
                    }
                    catch { }
                    backing = false;
                }
            }

        }
        public override void Initialize()
        {
            na.GetNeuronAt(0, 0).Label = "Stop";
            na.GetNeuronAt(1, 0).Label = "Done";
            na.GetNeuronAt(2, 0).Label = "TurnTo";
            na.GetNeuronAt(3, 0).Model = Neuron.modelType.FloatValue;
            na.GetNeuronAt(3, 0).Label = "Theta";
            na.GetNeuronAt(4, 0).Label = "MoveTo";
            na.GetNeuronAt(5, 0).Model = Neuron.modelType.FloatValue;
            na.GetNeuronAt(5, 0).Label = "R";
            na.GetNeuronAt(6, 0).Label = "Scan";
            na.GetNeuronAt(7, 0).Label = "Conf";
            na.GetNeuronAt(8, 0).Label = "EndPt";
            na.GetNeuronAt(9, 0).Label = "Coll";
            na.GetNeuronAt(10, 0).Label = "CollAngle";
            na.GetNeuronAt(10, 0).Model = Neuron.modelType.FloatValue;
        }

        public override void ShowDialog() //delete this function if it isn't needed
        {
            base.ShowDialog();
        }

        //Several Behaviors...
        public void Stop()
        {
            pending.Clear();
            na.GetNeuronAt("Done").SetValue(1);
        }



        //Random
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

        private void Scan()
        {
            for (int i = 0; i < 24; i++)
            {
                TurnTo((float)Math.PI / 12);
            }
        }

        bool handlingConf = false;
        private void Conf()
        {
            NeuronArea naModel = theNeuronArray.FindAreaByLabel("Module2DModel");
            if (naModel == null) return;
            Module2DModel nmModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));

            PointPlus pv = nmModel.FindLowConfidence();
            if (pv != null)
            {
                TurnTo((float)-pv.Theta);
                MoveTo((float)pv.R - .3f);
                TurnTo((float)Math.PI / 2);
                TurnTo((float)-Math.PI);
                handlingConf = false;
            }
        }

        public void GoTo()
        {

        }

        private void ConfEnd()
        {
            if (handlingConf)
                Stop();
        }

        private void Collision()
        {
            pending.Clear();
            float collisionAngle = na.GetNeuronAt("CollAngle").CurrentCharge;
            TurnTo(-collisionAngle - (float)Math.PI / 2);
            MoveTo(.2f);
            TurnTo(+collisionAngle + (float)Math.PI / 2);
        }

        //TurnTo
        public void TurnTo()
        {
            if (pending.Count > 0) return;
            float theta = na.GetNeuronAt("Theta").CurrentCharge;
            if (theta == 0) return;
            TurnTo(theta);
        }
        public void TurnTo(float theta)
        {
            while (theta > Math.PI) theta -= (float)Math.PI * 2;
            while (theta < -Math.PI) theta += (float)Math.PI * 2;

            while (Math.Abs(theta) > 0.001)
            {
                float theta1 = 0;
                if (theta > 0)
                {
                    if (theta > Math.PI / 6) theta1 = (float)Math.PI / 6;
                    else theta1 = theta;
                    theta = theta - theta1;
                }
                else
                {
                    if (theta < -Math.PI / 6) theta1 = -(float)Math.PI / 6;
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
            na.GetNeuronAt("MoveTo").SetValue(0);
            if (dist <= 0) return;
            MoveTo(dist);
        }
        public void MoveTo(float dist)
        {
            while (Math.Abs(dist) > 0.001)
            {
                float dist1 = 0;
                if (dist > .2f) dist1 = .2f;
                else dist1 = dist;
                dist = dist - dist1;
                behavior newBehavoir = new behavior()
                {
                    theBehavior = TheBehavior.Move,
                    param1 = dist1
                };
                pending.Add(newBehavoir);
            }
        }
    }
}
