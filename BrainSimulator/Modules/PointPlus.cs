//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows;

namespace BrainSimulator
{
        public class PointPlus
        {
            private Point p;
            private float r;
            private float theta;
            private bool polarDirty = false;
            private bool xyDirty = false;

            [XmlIgnore]
            public Point P
            {
                get { if (xyDirty) UpdateXY(); return p; }
                set { polarDirty = true; p = value; }
            }
            public float X { get { if (xyDirty) UpdateXY(); return (float)p.X; } set { p.X = value; polarDirty = true; } }
            public float Y { get { if (xyDirty) UpdateXY(); return (float)p.Y; } set { p.Y = value; polarDirty = true; } }
            [XmlIgnore]
            public Vector V { get => (Vector)P; }
            [XmlIgnore]
            public float Degrees { get => (float)(Theta * 180 / Math.PI); }
            public float Conf { get; set; }
            [XmlIgnore]
            public float R { get { if (polarDirty) UpdatePolar(); return r; } set { r = value; xyDirty = true; } }
            [XmlIgnore]
            public float Theta
            {
                get { if (polarDirty) UpdatePolar(); return theta; }
                set
                {//keep theta within the range +/- PI
                    theta = value;
                    if (theta > Math.PI) theta -= 2 * (float)Math.PI;
                    if (theta < -Math.PI) theta += 2 * (float)Math.PI;
                    xyDirty = true;
                }
            }

            private void UpdateXY()
            {
                p.X = r * Math.Cos(theta);
                p.Y = r * Math.Sin(theta);
                xyDirty = false;
            }

            public void UpdatePolar()
            {
                theta = (float)Math.Atan2(p.Y, p.X);
                r = (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
                polarDirty = false;
            }
            public bool Near(PointPlus PP, float toler)
            {
                if ((Math.Abs(PP.R - R) < 1 && Math.Abs(PP.Theta - Theta) < .1) ||
                    ((Math.Abs(PP.X - X) < toler && Math.Abs(PP.Y - Y) < toler)))
                    return true;
                return false;
            }
        }


}
