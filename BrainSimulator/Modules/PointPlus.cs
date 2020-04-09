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
using System.Windows.Media;
using static System.Math;
using static BrainSimulator.Utils;

namespace BrainSimulator.Modules
{
    //this is an extension of a position point which allows access via both polar and cartesian coordinates
    //it also accepts a "conf"idence value which can be used to indicate the accuracy of the position
    public class PointPlus
    {
        private Point p;
        private float r;
        private Angle theta;
        private bool polarDirty = false;
        private bool xyDirty = false;

        [XmlIgnore]
        public Point P
        {
            get { if (xyDirty) UpdateXY(); return p; }
            set { polarDirty = true; p = value; }
        }
        public float X { get { if (xyDirty) UpdateXY(); return (float)p.X; } set { if (xyDirty) UpdateXY(); p.X = value; polarDirty = true; } }
        public float Y { get { if (xyDirty) UpdateXY(); return (float)p.Y; } set { if (xyDirty) UpdateXY(); p.Y = value; polarDirty = true; } }
        [XmlIgnore]
        public Vector V { get => (Vector)P; }
        [XmlIgnore]
        public float Degrees { get => (float)(Theta * 180 / PI); }
        public float Conf { get; set; }
        [XmlIgnore]
        public float R { get { if (polarDirty) UpdatePolar(); return r; } set { if (polarDirty) UpdatePolar(); r = value; xyDirty = true; } }
        [XmlIgnore]
        public Angle Theta
        {
            get { if (polarDirty) UpdatePolar(); return theta; }
            set
            {//keep theta within the range +/- PI
                if (polarDirty) UpdatePolar();
                theta = value;
                if (theta > PI) theta -= 2 * (float)PI;
                if (theta < -PI) theta += 2 * (float)PI;
                xyDirty = true;
            }
        }
        private void UpdateXY()
        {
            p.X = r * Cos(theta);
            p.Y = r * Sin(theta);
            xyDirty = false;
        }

        public void UpdatePolar()
        {
            theta = (float)Atan2(p.Y, p.X);
            r = (float)Sqrt(p.X * p.X + p.Y * p.Y);
            polarDirty = false;
        }
        public bool Near(PointPlus PP, float toler)
        {
            //if ((Abs(PP.R - R) < 1 && Abs(PP.Theta - Theta) < .1) ||
            //    ((Abs(PP.X - X) < toler && Abs(PP.Y - Y) < toler)))
            if (Abs(PP.X - X) <= toler && Abs(PP.Y - Y) <= toler)
                return true;
            return false;
        }
        public override string ToString()
        {
            string s = "R: " + R.ToString("F3") + ", Theta: " + Degrees.ToString("F3") + "° (" + X.ToString("F2") + "," + Y.ToString("F2") + ")";
            return s;
        }
    }

    //this little helper adds the convenience of displaying angles in radians AND degrees even though they are stored in radians
    //it's really just an extension of float...it also accepts assignment from a double without an explicit cast
    public class Angle
    {
        private readonly float theAngle;
        public Angle(float angle) { this.theAngle = angle; }
        public static implicit operator float(Angle a) => a.theAngle;
        public static implicit operator Angle(float a) => new Angle(a);
        public static implicit operator Angle(double a) => new Angle((float)a);
        public override string ToString()
        {
            float degrees = theAngle * 180 / (float)PI;
            string s = theAngle.ToString("F3") + " " + degrees.ToString("F3") + "°";
            return s;
        }
        public int CompareTo(Angle a)
        {
            return theAngle.CompareTo(a.theAngle);
        }
    }
    public class ColorInt
    {
        private readonly int theColor;
        public ColorInt(int aColor) { this.theColor = aColor; }
        public static implicit operator int(ColorInt c) => c.theColor;
        public static implicit operator ColorInt(int aColor) => new ColorInt(aColor);
        public static implicit operator ColorInt(Color aColor) => new ColorInt(ColorToInt(aColor));
        public override string ToString()
        {
            int A = theColor >> 24 & 0xff;
            int R = theColor >> 16 & 0xff;
            int G = theColor >> 8 & 0xff;
            int B = theColor & 0xff;
            string s = "ARGB: " + A + "," + R + "," + G + "," + B;
            return s;
        }
        public int CompareTo(ColorInt c)
        {
            return theColor.CompareTo(c.theColor);
        }
    }

}
