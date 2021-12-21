//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class Module3DSim : ModuleBase
    {
        //any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;

        public Point3D cameraPosition = new Point3D(0, 0, 0);
        public Vector3D cameraDirection = new Vector3D(0, 0, -1);
        [XmlIgnore]
        public bool renderStarted = true;
        [XmlIgnore]
        public bool renderDone = false;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            SetNeuronValues();
            //if you want the dlg to update, use the following code 
            //because the thread you are in is not the UI thread
            //UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            for (int i = 0; i < mv.NeuronCount; i++)
                mv.GetNeuronAt(i).Model = Neuron.modelType.Color;
            SetNeuronValues();
        }
        void SetNeuronValues()
        {
            if (dlg == null) return;
            //if (renderStarted && !renderDone)  //TODO bug which prevents drawing...now has blank frames
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ((Module3DSimDlg)dlg).GetBitMap();
            });

            //if (!renderStarted || !renderDone) return;
            renderDone = false;
            renderStarted = false;
            System.Drawing.Bitmap bitmap1 = null;
            if (((Module3DSimDlg)dlg).theBitMap1 != null)
            {
                bitmap1 = ((Module3DSimDlg)dlg).theBitMap1;
                ((Module3DSimDlg)dlg).theBitMap1 = null;
            }
            else
            if (((Module3DSimDlg)dlg).theBitMap2 != null)
            {
                bitmap1 = ((Module3DSimDlg)dlg).theBitMap2;
                ((Module3DSimDlg)dlg).theBitMap2 = null;
            }

            if (bitmap1 == null) return;

            if (mv.Height == 0 || mv.Width == 0) return;
            float ratio = bitmap1.Width / mv.Width;
            float ratio2 = bitmap1.Height / mv.Height;
            if (ratio2 < ratio) ratio = ratio2;

            for (int i = 0; i < mv.Width; i++)
            {
                for (int j = 0; j < mv.Height; j++)
                {
                    Neuron n = mv.GetNeuronAt(i, j);
                    int x = (int)(i * ratio);
                    int y = (int)(j * ratio);
                    if (x >= bitmap1.Width) break;
                    if (y >= bitmap1.Height) break;
                    System.Drawing.Color c = bitmap1.GetPixel(x, y);
                    System.Windows.Media.Color c1 = new System.Windows.Media.Color
                    { A = c.A, R = c.R, G = c.G, B = c.B };
                    int theColor = Utils.ColorToInt(c1);

                    if (theColor != 0 && theColor != 8421504)
                        n.SetValueInt(theColor);
                    else
                        n.SetValueInt(0);
                }
            }
        }

        double theta = 0;
        public void Move(double x) //you can move forward/back in the direciton you are headed
        {
            cameraPosition.Z += -x * Math.Cos(theta);
            cameraPosition.X += x * Math.Sin(theta);
            renderStarted = true;
            UpdateDialog();
        }
        public void Rotate(double deltaTheta)
        {
            theta -= deltaTheta;
            cameraDirection.X = Math.Sin(theta);
            cameraDirection.Z = -Math.Cos(theta);
            renderStarted = true;
            UpdateDialog();
        }
    }
}
