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
using System.Windows.Controls;

namespace BrainSimulator.Modules
{
    public class ModuleImageZoom : ModuleBase
    {

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
        string oldFilePath;
        bool refreshNeeded = true;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (GetNeuron("Enable").CurrentCharge < 1) return;
            ModuleImageFile mif = (ModuleImageFile)FindModuleByName("ImageFile");
            if (mif == null) return;
            string newFilePath = mif.GetFilePath();
            //TODO need to actually check if neuron values changes (pan/zoom/rot) instead of refreshNeeded
            if (newFilePath == oldFilePath && !refreshNeeded) 
                return;
            oldFilePath = newFilePath;
            Bitmap bitmap1 = null;
            try
            {
                bitmap1 = new Bitmap(newFilePath);
            }
            catch {}
            if (bitmap1 == null) return;

            Angle rotation = 0;
            if (GetNeuron("Rot") is Neuron n1)
                rotation = n1.currentCharge; //range (0,1)
            rotation = rotation * 2 * (float)PI; //range (0,2Pi)

            //bitmap1 = RotateBitmap(bitmap1, rotation.ToDegrees());

            System.Windows.Point offset = new System.Windows.Point
            {
                X = GetNeuron("X").CurrentCharge,
                Y = GetNeuron("Y").CurrentCharge,
            };
            scale = GetNeuron("Scale").CurrentCharge;
            if (scale > 1) scale = 1;
            if (scale < 0) scale = 0;
            float minScale = (float)bitmap1.Width / (float)na.Width;
            scale =  scale + minScale;

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
            refreshNeeded = false;
            UpdateDialog();
        }

        // Return a bitmap rotated around its center.
        private Bitmap RotateBitmap(Bitmap bm, float angle)
        {
            // Make a Matrix to represent rotation
            // by this angle.
            bm.SetResolution(96, 96);

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
            GetPointBounds(points, out float xmin, out float xmax,out float ymin, out float ymax);

            // Make a bitmap to hold the rotated result.
            int wid = (int)Math.Round(xmax - xmin);
            int hgt = (int)Math.Round(ymax - ymin);
            Bitmap result = new Bitmap(wid, hgt);

            // Create the real rotation transformation.
            Matrix rotate_at_center = new Matrix();
            rotate_at_center.RotateAt(angle,
                new PointF(wid / 2f, hgt / 2f));

            //rotate_at_center.Scale(4,4);

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
            if (na == null) return;
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
            n1.SetValue(0);

            n1 = na.GetNeuronAt(4, 0);
            n1.Model = Neuron.modelType.FloatValue;
            n1.Label = "Rot";
            n1.SetValue(0);


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


        public override MenuItem GetCustomMenuItems()
        {
            StackPanel s2 = new StackPanel { Orientation = Orientation.Vertical };

            StackPanel s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "X:", Width = 60, HorizontalContentAlignment = HorizontalAlignment.Right});
            Slider sl1 = new Slider { Name = "x", Maximum = 1, Width = 100, Height = 20, Value = GetNeuronValue("X") };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);


            s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Y:", Width = 60, HorizontalContentAlignment = HorizontalAlignment.Right });
            sl1 = new Slider { Name = "y", Maximum = 1, Width = 100, Height = 20, Value = GetNeuronValue("Y") };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);

            s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Zoom:", Width = 60, HorizontalContentAlignment = HorizontalAlignment.Right });
            sl1 = new Slider { Name = "zoom", Maximum = 1, Width = 100, Height = 20, Value = GetNeuronValue("Scale") };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);

            s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Rotation:", Width = 60, HorizontalContentAlignment = HorizontalAlignment.Right });
            sl1 = new Slider { Name = "rotation", Maximum = 1, SmallChange = 0.00277777, LargeChange=PI/24,  Width = 100, Height = 20, Value = GetNeuronValue("Rot") };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);

            return new MenuItem { Header = s2, StaysOpenOnClick = true };
        }

        private void Sl1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider sl)
            {
                if (sl.Name == "x") SetNeuronValue("X", (float)sl.Value);
                if (sl.Name == "y") SetNeuronValue("Y", (float)sl.Value);
                if (sl.Name == "zoom") SetNeuronValue("scale", (float)sl.Value);
                if (sl.Name == "rotation") SetNeuronValue("Rot", (float)sl.Value);
                refreshNeeded = true;
            }
        }

    }
}
