//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using static BrainSimulator.Utils;
using static System.Math;

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

        public static implicit operator Point(PointPlus a) => a.p;
        public static implicit operator PointPlus(Point a) => new PointPlus(a);


        public PointPlus()
        {
            r = 0;
            theta = 0;
            P = new Point(0, 0);
            Conf = 0;
        }
        public PointPlus(float x, float y)
        {
            P = new Point(x, y);
            Conf = 0;
        }
        public PointPlus(float R1, Angle theta1)
        {
            R = R1;
            theta = theta1;
        }
        public PointPlus(PointPlus pp)
        {
            R = pp.R;
            theta = pp.Theta;
            Conf = pp.Conf;
        }
        public PointPlus(Point pp)
        {
            X = (float)pp.X;
            Y = (float)pp.Y;
            Conf = 0;
        }

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
        public PointPlus Clone()
        {
            PointPlus p1 = new PointPlus() { R = this.R, Theta = this.Theta, Conf = this.Conf };
            return p1;
        }
        public void UpdatePolar()
        {
            theta = (float)Atan2(p.Y, p.X);
            r = (float)Sqrt(p.X * p.X + p.Y * p.Y);
            polarDirty = false;
        }
        public bool Near(PointPlus PP, float toler)
        {
            if ((this - PP).R < toler) return true;
            return false;
        }
        public override string ToString()
        {
            string s = "R: " + R.ToString("F3") + ", Theta: " + Degrees.ToString("F3") + "° (" + X.ToString("F2") + "," + Y.ToString("F2") + ") Conf:" + Conf.ToString("F3");
            return s;
        }

        //these make comparisons by value instead of by reference
        public static bool operator ==(PointPlus a, PointPlus b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return (a.P.X == b.P.X && a.P.Y == b.P.Y && a.Conf == b.Conf);
        }
        public static bool operator !=(PointPlus a, PointPlus b)
        {
            if (a is null && b is null) return false;
            if (a is null || b is null) return true;
            return (a.P.X != b.P.X || a.P.Y != b.P.Y || a.Conf != b.Conf);
        }
        public static PointPlus operator +(PointPlus a, PointPlus b)
        {
            Point p = new Point(a.P.X + b.P.X, a.P.Y + b.P.Y);
            PointPlus retVal = new PointPlus
            {
                P = p,
            };
            return retVal;
        }
        public static PointPlus operator -(PointPlus a, PointPlus b)
        {
            PointPlus retVal = new PointPlus
            {
                P = new Point(a.P.X - b.P.X, a.P.Y - b.P.Y)
            };
            return retVal;
        }
        public override bool Equals(object p1)
        {
            if (p1 != null && p1 is PointPlus p2)
            {
                return (p2.P.X == P.X && p2.P.Y == P.Y);

            }
            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class Motion : PointPlus
    {
        public Angle rotation = 0;
        public override string ToString()
        {
            string s = "R: " + R.ToString("F3") + ", Theta: " + Degrees.ToString("F3") + "° (" + X.ToString("F2") + "," + Y.ToString("F2") + ") Rot:" + rotation;
            return s;
        }
    }

    public class Segment
    {
        public PointPlus P1;
        public PointPlus P2;
        public PointPlus Motion;
        public ColorInt theColor;

        public Segment() { }

        public Segment(PointPlus P1i, PointPlus P2i, ColorInt theColori)
        {
            P1 = P1i;
            P2 = P2i;
            theColor = theColori;
        }
        public PointPlus MidPoint
        {
            get
            {
                return new PointPlus { X = (P1.X + P2.X) / 2, Y = (P1.Y + P2.Y) / 2 };
            }
        }

        public PointPlus ClosestPoint()
        {
            Utils.FindDistanceToSegment(new Point(0, 0), P1.P, P2.P, out Point closest);
            return new PointPlus { P = closest };
        }
        public float Length
        {
            get
            {
                float length = (float)((Vector)P2.V - P1.V).Length;
                return length;
            }
        }
        public float VisualWidth()
        {
            float length = P2.Theta - P1.Theta;
            return length;
        }
        public Angle Angle
        {
            get
            {
                PointPlus pTemp = new PointPlus() { P = (Point)(P1.V - P2.V) };
                return pTemp.Theta;
            }
        }

        public Segment Clone()
        {
            Segment s = new Segment
            {
                P1 = this.P1.Clone(),
                P2 = this.P2.Clone(),
                theColor = this.theColor
            };
            if (this.Motion != null)
                Motion = this.Motion.Clone();
            return s;
        }
    }




    //this little helper adds the convenience of displaying angles in radians AND degrees even though they are stored in radians
    //it's really just an extension of float...it also accepts assignment from a double without an explicit cast
    public class Angle
    {
        private float theAngle;
        public Angle(float angle) { this.theAngle = angle; }
        public static implicit operator float(Angle a) => a.theAngle;
        public static implicit operator Angle(float a) => new Angle(a);
        public static implicit operator Angle(double a) => new Angle((float)a);
        public static  Angle operator -(Angle a, Angle b) 
        {
            Angle c = (float)a - (float)b;
            c = ((float)c + PI) % (2 * PI) - PI;
            return c;
        }
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

        public float Degrees
        {
            get { return theAngle * 180 / (float)PI; }
            set { theAngle = (float)(value * PI / 180.0); }
        }
        public float ToDegrees()
        {
            return theAngle * 180 / (float)PI;
        }
        public static float FromDegrees(float degrees)
        {
            return (float)(degrees * PI / 180.0);
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
        public static bool operator ==(ColorInt a, ColorInt b)
        {
            return (a.theColor == b.theColor);
        }
        public static bool operator !=(ColorInt a, ColorInt b)
        {
            return (a.theColor != b.theColor);
        }
        public int CompareTo(ColorInt c)
        {
            return theColor.CompareTo(c.theColor);
        }
        public override bool Equals(object a)
        {
            if (a is ColorInt c)
                return this.theColor.Equals(c.theColor);
            return false;
        }
        public override int GetHashCode()
        {
            return theColor;
        }
    }

}
