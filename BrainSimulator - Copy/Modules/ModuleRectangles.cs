using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleRectangles : ModuleBase
    {
        class DetectedRectangle
        {
            public int color;
            public int minX, minY, maxX, maxY;
            public int count;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (naIn == null) return;

            na.ClearNeuronChargeInArea();

            List<DetectedRectangle> rects = new List<DetectedRectangle>();

            naIn.BeginEnum();
            for (Neuron n = naIn.GetNextNeuron(); n != null; n = naIn.GetNextNeuron())
            {
                if (n.LastCharge != 0)
                {
                    naIn.GetNeuronLocation(n, out int X, out int Y);

                    int i = rects.FindIndex(r => r.color == n.LastChargeInt);
                    if (i == -1)
                    {
                        i = rects.Count;
                        rects.Add(new DetectedRectangle());
                        rects[i].color = n.LastChargeInt;
                        rects[i].maxX = rects[i].minX = X;
                        rects[i].maxY = rects[i].minY = Y;
                        rects[i].count = 1;
                    }
                    else
                    {
                        if (X < rects[i].minX) rects[i].minX = X;
                        if (Y < rects[i].minY) rects[i].minY = Y;
                        if (X > rects[i].maxX) rects[i].maxX = X;
                        if (Y > rects[i].maxY) rects[i].maxY = Y;
                        rects[i].count++;
                    }
                }
            }

            //if we have already detected rectangles, output them.
            //get the first rectangle and output it
            for (int i = 0; i < rects.Count; i++)
            {
                DetectedRectangle r = rects[i];

                //calculate the details for the rectangle
                //ranges...center:[-1,1] (zero center) bounds of the input rectangle so it works better in the reality model
                //         size: [0,1] as percent of visual field (width)
                naIn.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
                float width = naIn.Width;
                float sX = (r.maxX - r.minX) / width;
                float sY = (r.maxY - r.minY) / width;
                float cX = r.minX + (r.maxX - r.minX) / 2;
                float cY = r.minY + (r.maxY - r.minY) / 2;
                cX = cX / naIn.Width;
                cY = cY / naIn.Height;
                cX = cX * 2 - 1;
                cY = cY * 2 - 1;
                na.GetNeuronAt(0, 1 + i).SetValueInt(r.color);
                na.GetNeuronAt(3, 1 + i).Model = Neuron.modelType.Color;
                na.GetNeuronAt(1, 1 + i).SetValue(sX);
                na.GetNeuronAt(2, 1 + i).SetValue(sY);
                na.GetNeuronAt(3, 1 + i).SetValue(cX);
                na.GetNeuronAt(4, 1 + i).SetValue(cY);
                na.GetNeuronAt(3, 1 + i).Model = Neuron.modelType.FloatValue;
                na.GetNeuronAt(4, 1 + i).Model = Neuron.modelType.FloatValue;
                if (Utils.Close(sX, sY, .05f))
                    na.GetNeuronAt(5, 1 + i).SetValue(1);
                else
                    na.GetNeuronAt(6, 1 + i).SetValue(1);

                if (r.count > 3)
                    na.GetNeuronAt(7, 1 + i).SetValue(1);
                //outputs: 0-color, 1-SX, 2-SY, 3-CX, 4-CY,5-SQ,6-Rct,7-all on screen
            }
        }
    }
}
