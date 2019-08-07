using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace BrainSimulator
{
    public static class Utils
    {
        public static double fieldOfView = Math.PI / 2;
        public static Color FromArgb(int theColor)
        {
            Color c = new Color();
            c.A = 255;
            c.B = (byte)(theColor & 0xff);
            c.G = (byte)(theColor >> 8 & 0xff);
            c.R = (byte)(theColor >> 16 & 0xff);
            return c;
        }
        public static int ToArgb(Color theColor)
        {
            int retVal = 0;
            //retVal += theColor.A << 24; ??
            retVal += theColor.R << 16;
            retVal += theColor.G << 8;
            retVal += theColor.B;
            return retVal;
        }

        public static bool Close(float f1, float f2, float toler = 0.2f)
        {
            float dif = f2 - f1;
            dif = Math.Abs(dif);
            if (dif > toler) return false;
            return true;
        }

        public static bool Close(int a, int b)
        {
            if (Math.Abs(a - b) < 4) return true;
            return false;
        }
        public static bool ColorClose(Color c1, Color c2)
        {
            if (Close(c1.R, c2.R) && Close(c1.G, c2.G) && Close(c1.B, c2.B)) return true;
            return false;
        }

        public static string GetColorName(Color col)
        {
            PropertyInfo[] p1 = typeof(Colors).GetProperties();
            foreach (PropertyInfo p in p1)
            {
                Color c = (Color)p.GetValue(null);
                if (ColorClose(c, col))
                    return p.Name;
            }
            return "??";

            //  PropertyInfo colorProperty = typeof(Colors).GetProperties()
            //        .FirstOrDefault(p => Color.AreClose((Color)p.GetValue(null), col));
            //    return colorProperty != null ? colorProperty.Name : "unnamed color";
        }

        public static Vector RotateVector(Vector v, double theta)
        {
            var ca = Math.Cos(theta);
            var sa = Math.Sin(theta);
            return new Vector(ca * v.X - sa * v.Y, sa * v.X + ca * v.Y);
        }

        public static PolarVector ToPolar(Point p)
        {
            PolarVector pv = new PolarVector()
            {
                theta = ConvTheta(Math.Atan2(p.Y, p.X)),
                r = Math.Sqrt(p.X * p.X + p.Y * p.Y)
            };
            return pv;
        }
        public static Point ToCartesian(PolarVector pv)
        {
            Point p = new Point(pv.r * Math.Cos(pv.theta), pv.r * Math.Sin(pv.theta));
            return p;
        }

        //This converts an angle from a normal (with 0 pointing in +X) to
        //robotic (with 0 pointing ahead (+Y))
        //interestingly, this function is its own inverse
        public static double ConvTheta(double theta)
        {
            return Math.PI / 2 - theta;
        }

    }
    public class PolarVector
    {
        public double r;
        public double theta;
    }
}
