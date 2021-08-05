//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using System.Drawing;
using System.Drawing.Drawing2D;
using static System.Math;


namespace BrainSimulator.Modules
{
    public class ModuleImageZoom : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleImageZoom()
        {
            minHeight = 10;
            maxHeight = 500;
            minWidth = 10;
            maxWidth = 500;
        }

        float scale = 1.0f;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (GetNeuron("Enable").CurrentCharge < 1) return;
            ModuleImageFile mif = (ModuleImageFile)FindModuleByName("ImageFile");
            if (mif == null) return;
            Bitmap bitmap1 = mif.GetBitMap();
            if (bitmap1 == null) return;

            Angle rotation = 0;
            if (GetNeuron("Rot") is Neuron n1)
                rotation = n1.currentCharge;
            rotation = rotation * 2 * (float)PI;
            //if (rotation != 0)
                bitmap1 = RotateBitmap(bitmap1, rotation.ToDegrees());


            System.Windows.Point offset = new System.Windows.Point
            {
                X = GetNeuron("X").CurrentCharge,
                Y = GetNeuron("Y").CurrentCharge,
            };
            scale = GetNeuron("Scale").CurrentCharge;
            if (scale > 1) scale = 1;
            if (scale < 0) scale = 0;
            float minScale = (float)bitmap1.Width / (float)na.Width;
            minScale = minScale + 1;
            scale = 1 + (1 - scale) * (minScale - 1);



            for (int i = 0; i < na.Height - 1; i++)
                for (int j = 0; j < na.Width; j++)
                {
                    Neuron n = na.GetNeuronAt(j, i + 1);
                    int x = (int)(offset.X * bitmap1.Width + j * scale);
                    int y = (int)(offset.Y * bitmap1.Height + i * scale);


                    if (x >= bitmap1.Width) goto NoData;
                    if (y >= bitmap1.Height) goto NoData;
                    if (x < 0) goto NoData;
                    if (y < 0) goto NoData;

                    Color c = bitmap1.GetPixel(x, y);
                    float hue = c.GetHue();
                    float brightness = c.GetBrightness();
                    int val = Utils.ColorToInt(c);

                    n.LastChargeInt = val;
                    continue;
                NoData:
                    n.LastChargeInt = 0xffffff;

                }

        }

        // Return a bitmap rotated around its center.
        private Bitmap RotateBitmap(Bitmap bm, float angle)
        {
            // Make a Matrix to represent rotation
            // by this angle.
            Matrix rotate_at_origin = new Matrix();
            rotate_at_origin.Rotate(angle);

            // Rotate the image's corners to see how big
            // it will be after rotation.
            PointF[] points =
            {
        new PointF(0, 0),
        new PointF(bm.Width, 0),
        new PointF(bm.Width, bm.Height),
        new PointF(0, bm.Height),
    };
            rotate_at_origin.TransformPoints(points);
            float xmin, xmax, ymin, ymax;
            GetPointBounds(points, out xmin, out xmax,
                out ymin, out ymax);


            // Make a bitmap to hold the rotated result.
            int wid = (int)Math.Round(xmax - xmin);
            int hgt = (int)Math.Round(ymax - ymin);
            Bitmap result = new Bitmap(wid, hgt);

            // Create the real rotation transformation.
            Matrix rotate_at_center = new Matrix();
            rotate_at_center.RotateAt(angle,
                new PointF(wid / 2f, hgt / 2f));

            rotate_at_center.Scale(3,3f);

            // Draw the image onto the new bitmap rotated.
            using (Graphics gr = Graphics.FromImage(result))
            {
                // Use smooth image interpolation.
                gr.InterpolationMode = InterpolationMode.High;

                // Clear with the color in the image's upper left corner.
                gr.Clear(bm.GetPixel(0, 0));

                //// For debugging. (It's easier to see the background.)
                //gr.Clear(Color.LightBlue);

                // Set up the transformation to rotate.
                gr.Transform = rotate_at_center;

                // Draw the image centered on the bitmap.
                int x = (wid - bm.Width) / 2;
                int y = (hgt - bm.Height) / 2;
                gr.DrawImage(bm, x, y);
            }

            // Return the result bitmap.
            return result;
        }

        private void GetPointBounds (System.Drawing.PointF[] pts, out float minx, out float maxx, out float miny, out float maxy)
        {
            minx = pts.Min(a => a.X);
            miny = pts.Min(a => a.Y);
            maxx = pts.Max(a => a.X);
            maxy = pts.Max(a => a.Y);
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            for (int i = 1; i < na.Height; i++)
                for (int j = 0; j < na.Width; j++)
                {
                    na.GetNeuronAt(j, i).Model = Neuron.modelType.Color;
                }
            Neuron n1 = na.GetNeuronAt(0, 0);
            n1.Model = Neuron.modelType.IF;
            n1.Label = "Enable";
            n1.AddSynapse(n1.id, 1);
            n1.SetValue(1);

            n1 = na.GetNeuronAt(1, 0);
            n1.Model = Neuron.modelType.FloatValue;
            n1.Label = "X";
            n1.SetValue(0);

            n1 = na.GetNeuronAt(2, 0);
            n1.Model = Neuron.modelType.FloatValue;
            n1.Label = "Y";
            n1.SetValue(0);

            n1 = na.GetNeuronAt(3, 0);
            n1.Model = Neuron.modelType.FloatValue;
            n1.Label = "Scale";
            n1.SetValue(1);

            n1 = na.GetNeuronAt(4, 0);
            n1.Model = Neuron.modelType.FloatValue;
            n1.Label = "Rot";
            n1.SetValue(1);


        }

        public void SetX(float val)
        {
            SetNeuronValue("X", val);
        }
        public void SetZoom(float val)
        {
            SetNeuronValue("Scale", val);
        }
        public void SetY(float val)
        {
            SetNeuronValue("Y", val);
        }
        public void SetRotation(float val)
        {
            SetNeuronValue("Rot", val);
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (na == null) return; //this is called the first time before the module actually exists
        }
    }
}
