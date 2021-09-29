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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;

namespace BrainSimulator.Modules
{

    public partial class ModuleAttentionDlg : ModuleBaseDlg
    {
        public static readonly DependencyProperty AttentionObjectProperty =
            DependencyProperty.Register("attentionObject", typeof(Thing), typeof(MenuItem));

        public ModuleAttentionDlg()
        {
            InitializeComponent();
        }

        ModuleUKS uks = null;

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            ModuleAttention parent = (ModuleAttention)base.ParentModule;
            theCanvas.Children.Clear();
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);

            ModuleView naSource = MainWindow.theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return false;
            uks = (ModuleUKS)naSource.TheModule;

            Thing mentalModel = uks.GetOrAddThing("MentalModel", "Visual");
            if (mentalModel == null || mentalModel.Children.Count == 0) return false;

            Thing attn = uks.Labeled("ATTN");
            if (attn == null || attn.References.Count == 0) return false;

            Thing attnTarget = attn.GetReferenceWithAncestor(uks.Labeled("Visual"));
            var values = uks.GetValues(attnTarget);
            HSLColor c1 = new HSLColor(values["Hue+"], values["Sat+"], values["Lum+"]);
            Color fillColor = c1.ToColor();

            try
            {
                double largest = 0;
                foreach (Thing area in mentalModel.Children)
                {
                    var areaValues = uks.GetValues(area);
                    Thing theShape = area.Children[0];
                    var shapeValues = uks.GetValues(theShape);
                    PointPlus pOffset = new PointPlus(areaValues["CtrX+"] - shapeValues["CtrX+"], areaValues["CtrY+"] - shapeValues["CtrY+"]);
                    foreach (Thing corner in theShape.Children)
                    {
                        Point p = (Point)corner.Children[0].V;
                        p = p + pOffset;
                        largest = Math.Max(largest, p.X);
                        largest = Math.Max(largest, p.Y);
                    }
                }

                largest += 10; //a little margin

                float  scale = (float)Math.Min(windowSize.X, windowSize.Y) / (float)largest;
                if (scale == 0) return false;

                theCanvas.Children.Clear();
                foreach (Thing area in mentalModel.Children)
                {
                    PointCollection pts = new PointCollection();
                    var areaValues = uks.GetValues(area);
                    Thing theShape = area.Children[0];
                    var shapeValues = uks.GetValues(theShape);

                    //These corrections are needed because known objects are stored at the location and size when they were first seen
                    //now the viewed object will have a different size and location
                    PointPlus pAreaCtr = new PointPlus(areaValues["CtrX+"], areaValues["CtrY+"]);
                    PointPlus pShapeCtr = new PointPlus(shapeValues["CtrX+"], shapeValues["CtrY+"]);
                    float areaSize = areaValues["Siz+"];
                    float shapeSize = shapeValues["Siz+"];
                    Angle areaAngle = areaValues["Ang+"];
                    Angle shapeAngle = shapeValues["Ang+"];
                    Angle rotation = areaAngle - shapeAngle;

                    foreach (Thing corner in theShape.Children)
                    {
                        PointPlus p = new PointPlus((Point)corner.Children[0].V);
                        p = p-pShapeCtr;
                        p.Theta += rotation;
                        float ratio = areaSize / shapeSize;
                        p.X *= ratio;
                        p.Y *= ratio;
                        p = p + pAreaCtr;

                        p.X *= scale;
                        p.Y *= scale;
                        pts.Add(p);
                    }
                    Polygon poly = new Polygon { Points = pts, Stroke = new SolidColorBrush(Colors.AliceBlue) };
                    poly.ToolTip = area.Label;
                    poly.Fill = this.Background;
                    if (attnTarget == area)
                    {
                        poly.Fill = new SolidColorBrush(fillColor);
                        poly.Stroke = new SolidColorBrush(fillColor);
                        poly.Fill.Opacity = 1;
                    }
                    poly.MouseDown += Poly_MouseDown;
                    poly.SetValue(AttentionObjectProperty, area);
                    theCanvas.Children.Add(poly);
                }
            }
#pragma warning disable 168
            catch (Exception e) //sometimes useful for debugging
#pragma warning restore 168
            { }
            return true;
        }

        private void Poly_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Polygon poly)
            {
                Thing t = (Thing)poly.GetValue(AttentionObjectProperty);
                ModuleAttention parent = (ModuleAttention)base.ParentModule;
                parent.SetAttention(t);
            }
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void theCanvas_MouseEnter(object sender, MouseEventArgs e)
        {
            theCanvas.Background = new SolidColorBrush(Colors.LightSteelBlue);
            ModuleAttention parent = (ModuleAttention)base.ParentModule;
            parent.SetEnable(false);
        }

        private void theCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            theCanvas.Background = new SolidColorBrush(Colors.Gray);
            ModuleAttention parent = (ModuleAttention)base.ParentModule;
            parent.SetEnable(true);
        }
    }
}
