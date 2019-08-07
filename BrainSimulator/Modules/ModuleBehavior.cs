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

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (na.GetNeuronAt(0, 0).LastCharge == 1) Stop();
            if (na.GetNeuronAt(1, 0).LastCharge == 1) RandomBehavior();
            if (na.GetNeuronAt(2, 0).LastCharge == 1) BackOff();
            if (na.GetNeuronAt(3, 0).LastCharge == 1) TurnTo();
            if (na.GetNeuronAt(5, 0).LastCharge == 1) MoveTo();
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
                    NeuronArea naEntity = theNeuronArray.FindAreaByLabel("ModuleEntity");
                    try
                    {
                        naEntity.GetNeuronAt("Idle").SetValue(1);
                    }
                    catch { }
                }
            }
        }
        public override void Initialize()
        {
            na.GetNeuronAt(0, 0).Label = "Stop";
            na.GetNeuronAt(1, 0).Label = "Random";
            na.GetNeuronAt(2, 0).Label = "BackOff";
            na.GetNeuronAt(3, 0).Label = "TurnTo";
            na.GetNeuronAt(5, 0).Label = "MoveTo";
        }
        public override void ShowDialog() //delete this function if it isn't needed
        {
            base.ShowDialog();
        }

        //Several Behaviors...
        private void Stop()
        {
            pending.Clear();
        }



        //Random
        private void RandomBehavior()
        {
            double x = new Random().NextDouble();
            if (x < .8) //Move?
            {
                behavior newBehavoir = new behavior()
                {
                    theBehavior = TheBehavior.Move,
                    param1 = .2f
                };
                pending.Add(newBehavoir);
            }
            else //turn if desired
            {
                behavior newBehavoir = new behavior()
                {
                    theBehavior = TheBehavior.Turn
                };
                newBehavoir.param1 = -(float)Math.PI / 6;
                if (x < .8) newBehavoir.param1 = -(float)Math.PI / 6;
                else if (x < .85) newBehavoir.param1 = -(float)Math.PI / 24;
                else if (x < .9) newBehavoir.param1 = (float)Math.PI / 24;
                else if (x < .95) newBehavoir.param1 = (float)Math.PI / 6;

                pending.Add(newBehavoir);
            }
        }

        //Back Off
        private void BackOff()
        {
            behavior newBehavoir = new behavior()
            {
                theBehavior = TheBehavior.Turn,
                param1 = (float)Math.PI/12
            };
            pending.Add(newBehavoir);
            newBehavoir = new behavior()
            {
                theBehavior = TheBehavior.Move,
                param1 = -1
            };
            pending.Add(newBehavoir);
            pending.Add(newBehavoir);

            //            NeuronArea naMo/ve = theNeuronArray.FindAreaByLabel("ModuleMove");
            //            naMove.GetNeuronAt(0, 4).CurrentCharge = 1;
        }
        //TurnTo
        private void TurnTo()
        {
             float theta = na.GetNeuronAt(4, 0).CurrentCharge ;
            na.GetNeuronAt(4, 0).SetValue(0);
            if (theta == 0) return;
            theta = theta;
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
                //newBehavior = new behavior();
                //newBehavior.theBehavior = TheBehavior.Rest;
                //pending.Add(newBehavior);
                //pending.Add(newBehavior);
            }
        }
        //MoveTo
        private void MoveTo()
        {
            float dist = na.GetNeuronAt(6, 0).CurrentCharge; 
            if (dist <= 0) return;
            while (Math.Abs(dist) > 0.001)
            {
                float dist1 = 0;
                if (dist > .2f) dist1 = .2f;
                else dist1 = dist;
                dist = dist - dist1;
                behavior newBehavoir = new behavior()
                {
                    theBehavior = TheBehavior.Move,
                    param1 = dist1 * 5
                };
                pending.Add(newBehavoir);
            }
        }
    }
}
