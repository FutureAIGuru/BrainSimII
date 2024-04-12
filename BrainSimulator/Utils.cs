//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using BrainSimulator.Modules;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Math;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

namespace BrainSimulator
{
    //This is not used
    class Range
    {
        float minX;
        float minY;
        float maxX;
        float maxY;
        public Range(Point loc, Angle angle, float length)
        {
            minX = (float)loc.X;
            minY = (float)loc.Y;
            maxX = minX + (float)Cos(angle) * length;
            maxY = minY + (float)Sin(angle) * length;
            if (minX > maxX)
            {
                float temp = minX;
                minX = maxX;
                maxX = temp;
            }
            if (minY > maxY)
            {
                float temp = minY;
                minY = maxY;
                maxY = temp;
            }
            //                minX -= 1; maxX += 1; minY -= 1; maxY += 1;
        }
        public bool Overlaps(Range r2, float minOverlap = 0)
        {
            if (r2.minX > maxX + minOverlap) return false;
            if (r2.minY > maxY + minOverlap) return false;
            if (r2.maxX < minX - minOverlap) return false;
            if (r2.maxY < minY - minOverlap) return false;
            return true;
        }
    }

    public class HSLColor
    {
        public float hue;
        public float saturation;
        public float luminance;
        public HSLColor() { }
        public HSLColor(float h, float s, float l)
        {
            hue = h;
            saturation = s;
            luminance = l;
        }
        public HSLColor(Dictionary<string, float> values)
        {
            hue = 0;
            saturation = 0;
            luminance = 0;
            try
            {
                hue = values["Hue+"];
                saturation = values["Sat+"];
                luminance = values["Lum+"];
            }
            catch { }
        }
        public HSLColor(Color c)
        {
            System.Drawing.Color c1 = System.Drawing.Color.FromArgb(255, c.R, c.G, c.B);
            hue = c1.GetHue();
            saturation = c1.GetSaturation();
            luminance = c1.GetBrightness();
        }
        public HSLColor(HSLColor c)
        {
            hue = c.hue;
            saturation = c.saturation;
            luminance = c.luminance;
        }

        public override string ToString()
        { return "H:" + hue.ToString("f2") + " S:" + saturation.ToString("f2") + " L:" + luminance.ToString("f2"); }

        public static float operator -(HSLColor c1,HSLColor c2)
        {
            //any lum > .95 is white  \
            //any lum < .15 is black    -set lum to .5
            //any sat  < .1 is gray   /
            if (c1.luminance > 0.95) c1.hue = 0.5f;
            else if (c1.luminance < .1) c1.hue = 0.5f;
            else if (c1.saturation < .1) c1.hue = 0.5f;
            if (c2.luminance > 0.95) c2.hue = 0.5f;
            else if (c2.luminance < .1) c2.hue = 0.5f;
            else if (c2.saturation < .1) c2.hue = 0.5f;
            float diff = Abs(c1.hue- c2.hue) * 5 + Abs(c1.saturation - c2.saturation) + Abs(c1.luminance - c2.luminance);
            diff /= 7;
            return diff;
        }
        public Color ToColor()
        {
            Color c1 = ColorFromHSL();
            return c1;
        }
        // the Color Converter
        Color ColorFromHSL()
        {
            if (saturation == 0)
            {
                byte L = (byte)(luminance * 255);
                return Color.FromArgb(255, L, L, L);
            }

            double min, max;

            max = luminance < 0.5d ? luminance * (1 + saturation) : (luminance + saturation) - (luminance * saturation);
            min = (luminance * 2d) - max;

            Color c = Color.FromArgb(255, (byte)(255 * RGBChannelFromHue(min, max, hue + 1 / 3d)),
                                          (byte)(255 * RGBChannelFromHue(min, max, hue)),
                                          (byte)(255 * RGBChannelFromHue(min, max, hue - 1 / 3d)));
            return c;
        }

        static double RGBChannelFromHue(double m1, double m2, double h)
        {
            h = (h + 1d) % 1d;
            if (h < 0) h += 1;
            if (h * 6 < 1) return m1 + (m2 - m1) * 6 * h;
            else if (h * 2 < 1) return m2;
            else if (h * 3 < 2) return m1 + (m2 - m1) * 6 * (2d / 3d - h);
            else return m1;

        }
    }


    public static class Utils
    {
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern void GetSystemTimePreciseAsFileTime(out long filetime);
        public static long GetPreciseTime()
        {
            GetSystemTimePreciseAsFileTime(out long fileTime);
            return fileTime;
        }

        public static void Noop()
        {

        }

        public static float RoundToSignificantDigits(this float d, int digits)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return (float)(scale * Math.Round(d / scale, digits));
        }

        //this searches a control tree to find a control by name so you can retrieve its value
        public static Control FindByName(Visual v, string name)
        {
            foreach (Object o in LogicalTreeHelper.GetChildren(v))
            {
                if (o is Visual v3)
                {
                    if (v3 is Control c1)
                    {
                        if (c1.Name == name)
                            return c1;
                    }
                    try
                    {
                        Control c2 = FindByName(v3, name);
                        if (c2 != null)
                            return c2;
                    }
                    catch { }
                }
            }
            return null;
        }

        public static float Rad(float degrees)
        {
            return (float)(degrees * Math.PI / 180);
        }


        public static System.Drawing.Color IntToDrawingColor(int theColor)
        {
            Color c1 = IntToColor(theColor);
            System.Drawing.Color c = System.Drawing.Color.FromArgb(c1.A, c1.R, c1.G, c1.B);
            return c;
        }

        public static Color IntToColor(int theColor)
        {
            Color c = new Color();
            c.A = 255;
            c.B = (byte)(theColor & 0xff);
            c.G = (byte)(theColor >> 8 & 0xff);
            c.R = (byte)(theColor >> 16 & 0xff);
            return c;
        }
        public static int ColorToInt(Color theColor)
        {
            int retVal = 0;
            //retVal += theColor.A << 24; do we need "a" value?
            retVal += theColor.R << 16;
            retVal += theColor.G << 8;
            retVal += theColor.B;
            return retVal;
        }
        public static int ColorToInt(System.Drawing.Color theColor)
        {
            int retVal = 0;
            //retVal += theColor.A << 24; ??
            retVal += theColor.R << 16;
            retVal += theColor.G << 8;
            retVal += theColor.B;
            return retVal;
        }

        //helper to make rainbow colors
        // Map a value to a rainbow color.
        public static Color RainbowColorFromValue(float value) //value has a range -1,1
        {
            // Convert into a value between 0 and 1023.
            int int_value = (int)(1023 * value);

            if (int_value < -1022) //fully negative
            {
                return Colors.Black;
            }
            else if (int_value >= 1023) //fully positive
            {
                return Colors.White;
            }
            else if (int_value == 0) //0 (blue)
            {
                return Colors.Blue;
            }
            else if (int_value < 0) // -1,0 graysacle
            {
                int_value = (1024 - (Math.Abs(int_value) / 2) + 512) / 4;
                return Color.FromRgb((byte)int_value, (byte)int_value, (byte)int_value);
            }

            int_value = 1023 - int_value;
            // Map different color bands.
            if (int_value < 256)
            {
                // Red to yellow. (255, 0, 0) to (255, 255, 0).
                return Color.FromRgb(255, (byte)int_value, 0);
            }
            else if (int_value < 512)
            {
                // Yellow to green. (255, 255, 0) to (0, 255, 0).
                int_value -= 256;
                return Color.FromRgb((byte)(255 - int_value), 255, 0);
            }
            else if (int_value < 768)
            {
                // Green to aqua. (0, 255, 0) to (0, 255, 255).
                int_value -= 512;
                return Color.FromRgb(0, 255, (byte)int_value);
            }
            else
            {
                // Aqua to blue. (0, 255, 255) to (0, 0, 255).
                int_value -= 768;
                return Color.FromRgb(0, (byte)(255 - int_value), 255);
            }
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
        }
        public static double FindDistanceToSegment(Segment s)
        {
            if (s == null) return 0;
            return FindDistanceToSegment(new Point(0, 0), s.P1.P, s.P2.P, out Point closest);
        }

        public static double FindDistanceToSegment(Segment s, out Point closest)
        {
            return FindDistanceToSegment(new Point(0, 0), s.P1.P, s.P2.P, out closest);
        }

        public static Vector GetClosestPointOnLine(Vector A, Vector B, Vector P)
        {
            Vector AP = P - A;       //Vector from A to P   
            Vector AB = B - A;       //Vector from A to B  

            float magnitudeAB = (float)(AB.Length * AB.Length);     //Magnitude of AB vector (it's length squared)     
            float ABAPproduct = (float)Vector.Multiply(AP, AB);    //The DOT product of a_to_p and a_to_b     
            float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

            //if (distance < 0)     //Check if P projection is over vectorAB     
            //{
            //    return A;

            //}
            //else if (distance > 1)
            //{
            //    return B;
            //}
            //else
            {
                return A + AB * distance;
            }
        }

        public static float DistanceBetweenTwoSegments(Point p1, Point p2, Point p3, Point p4)
        {
            float retVal = float.MaxValue;
            double d1 = FindDistanceToSegment(p1, p3, p4, out Point closest);
            if (d1 < retVal)
                retVal = (float)d1;
            d1 = FindDistanceToSegment(p2, p3, p4, out closest);
            if (d1 < retVal)
                retVal = (float)d1;
            d1 = FindDistanceToSegment(p3, p1, p2, out closest);
            if (d1 < retVal)
                retVal = (float)d1;
            d1 = FindDistanceToSegment(p4, p1, p2, out closest);
            if (d1 < retVal)
                retVal = (float)d1;
            return retVal;
        }

        // Calculate the distance between
        // point pt and the segment p1 --> p2.
        public static double FindDistanceToSegment(
            Point pt, Point p1, Point p2, out Point closest)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = p1;
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            double t = ((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) /
                (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new Point(p1.X, p1.Y);
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
            }
            else if (t > 1)
            {
                closest = new Point(p2.X, p2.Y);
                dx = pt.X - p2.X;
                dy = pt.Y - p2.Y;
            }
            else
            {
                closest = new Point(p1.X + t * dx, p1.Y + t * dy);
                dx = pt.X - closest.X;
                dy = pt.Y - closest.Y;
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            FindIntersection(p1, p2, p3, p4,
            out bool lines_intersect, out bool segments_intersect,
            out Point intersection,
            out Point close_p1, out Point close_p2,
            out double collisionAngle);
            return segments_intersect;
        }

        // Find the point of intersection between
        // the lines p1 --> p2 and p3 --> p4.
        public static void FindIntersection(
            Point p1, Point p2, Point p3, Point p4,
            out Point intersection
            )
        {
            FindIntersection(p1, p2, p3, p4,
            out bool lines_intersect, out bool segments_intersect,
            out intersection,
            out Point close_p1, out Point close_p2,
            out double collisionAngle);
        }
        public static void FindIntersection(
        Point p1, Point p2, Point p3, Point p4,
        out bool lines_intersect, out bool segments_intersect,
        out Point intersection,
        out Point close_p1, out Point close_p2,
        out double collisionAngle)
        {
            // Get the segments' parameters.
            double dx12 = p2.X - p1.X;
            double dy12 = p2.Y - p1.Y;
            double dx34 = p4.X - p3.X;
            double dy34 = p4.Y - p3.Y;

            double theta1 = Math.Atan2(dy12, dx12); //obstacle
            double theta2 = Math.Atan2(dy34, dx34); //motion attempt
            collisionAngle = theta2 - theta1; //angle between the two

            // Solve for t1 and t2
            double denominator = (dy12 * dx34 - dx12 * dy34);

            double t1 =
                ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                    / denominator;

            if (double.IsInfinity(t1))
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new Point(float.NaN, float.NaN);
                close_p1 = new Point(float.NaN, float.NaN);
                close_p2 = new Point(float.NaN, float.NaN);
                return;
            }
            lines_intersect = true;

            double t2 =
                ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
                    / -denominator;

            // Find the point of intersection.
            intersection = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect =
                ((t1 >= 0) && (t1 <= 1) &&
                 (t2 >= 0) && (t2 <= 1));
            //segments_intersect =
            //    ((t1 >= -.09) && (t1 <= 1.09) &&
            //     (t2 >= -.09) && (t2 <= 1.09));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);
            close_p2 = new Point(p3.X + dx34 * t2, p3.Y + dy34 * t2);
        }

        public static float DistancePointToLine(Point P, Point P1, Point P2)
        {
            double distance = Math.Abs((P2.X - P1.X) * (P1.Y - P.Y) - (P1.X - P.X) * (P2.Y - P1.Y)) /
                    Math.Sqrt(Math.Pow(P2.X - P1.X, 2) + Math.Pow(P2.Y - P1.Y, 2));
            return (float)distance;
        }

        //find a point which is dist off the end of a line segment
        public static PointPlus ExtendSegment(Point P1, Point P2, float dist, bool firstPt)
        {
            if (firstPt)
            {
                Vector v = P2 - P1;
                double changeLength = (v.Length + dist) / v.Length;
                v = Vector.Multiply(changeLength, v);
                PointPlus newPoint = new PointPlus { P = P2 - v };
                return newPoint;
            }
            else
            {
                Vector v = P1 - P2;
                double changeLength = (v.Length + dist) / v.Length;
                v = Vector.Multiply(changeLength, v);
                PointPlus newPoint = new PointPlus { P = P1 - v };
                return newPoint;
            }
        }

        /// <summary>
        /// Determines if the given point is inside the polygon
        /// </summary>
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        public static bool IsPointInPolygon(Point[] polygon, Point testPoint)
        {
            bool result = false;
            if (polygon == null) return false;
            if (polygon.Count() == 2)
            {
                float dist = Utils.DistancePointToLine(testPoint, polygon[0], polygon[1]);
                if (dist < 0.1f) return true;
                return false;
            }
            int j = polygon.Count() - 1;
            if (polygon.Contains(testPoint)) return true;
            for (int i = 0; i < polygon.Count(); i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }



        /// <summary>
        /// Method to compute the centroid of a polygon. This does NOT work for a complex polygon.
        /// </summary>
        /// <param name="poly">points that define the polygon</param>
        /// <returns>centroid point, or PointF.Empty if something wrong</returns>
        public static Point GetCentroid(List<Point> poly)
        {
            double accumulatedArea = 0.0f;
            double centerX = 0.0f;
            double centerY = 0.0f;

            if (poly.Count == 2)
            {
                return new Point((poly[0].X + poly[1].X) / 2f, (poly[0].Y + poly[1].Y) / 2f);
            }


            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                double temp = poly[i].X * poly[j].Y - poly[j].X * poly[i].Y;
                accumulatedArea += temp;
                centerX += (poly[i].X + poly[j].X) * temp;
                centerY += (poly[i].Y + poly[j].Y) * temp;
            }

            if (Math.Abs(accumulatedArea) < 1E-7f)
                return new Point(0, 0);  // Avoid division by zero

            accumulatedArea *= 3f;
            return new Point(centerX / accumulatedArea, centerY / accumulatedArea);
        }



        //This textbox has a special action to cope with peculiar focus issues when a textbox is placed on a context menu
        public static TextBox ContextMenuTextBox(string content, string name, float width)
        {
            TextBox tb = new TextBox()
            {
                Text = content,
                Name = name,
                Width = width,
                VerticalAlignment = VerticalAlignment.Center,
            };
            tb.PreviewLostKeyboardFocus += Tb_PreviewLostKeyboardFocus;
            return tb;
        }

        private static void Tb_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!(e.NewFocus is TextBox) && !(e.NewFocus is ComboBox))
                e.Handled = true;
        }

        public static void AddToValues(float value, List<float> values)
        {
            if (!values.Contains(value))
            {
                values.Add(value);
                values.Sort();
                values.Reverse();
            }
        }

        //there is a label followed by a combobox with provided values
        public static MenuItem CreateComboBoxMenuItem(string cbName, float value, List<float> values, string format, string label,
            int textWidth, RoutedEventHandler theEventHandler)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = label, Padding = new Thickness(0) });
            ComboBox theCombo = CreateComboBox(cbName, value, values, format, textWidth, theEventHandler);
            sp.Children.Add(theCombo);
            return new MenuItem { StaysOpenOnClick = true, Header = sp };
        }

        public static ComboBox CreateComboBox(string cbName, float value, List<float> values, string format, int textWidth, RoutedEventHandler theEventHandler)
        {
            ComboBox theCombo = new ComboBox { IsEditable = true, Width = textWidth, Name = cbName };
            theCombo.Text = format.IndexOf("X") == -1 ? value.ToString(format) : ((int)value).ToString(format);
            for (int i = 0; i < values.Count; i++)
                theCombo.Items.Add(format.IndexOf("X") == -1 ? values[i].ToString(format) : ((int)values[i]).ToString(format));
            theCombo.AddHandler(TextBox.TextChangedEvent, theEventHandler);
            theCombo.AddHandler(ComboBox.SelectionChangedEvent, theEventHandler);
            return theCombo;
        }

        public static void ValidateInput(ComboBox cb, float min, float max, string validation = "")
        {
            //this hack finds the textbox within a combobox
            var textbox = (TextBox)cb.Template.FindName("PART_EditableTextBox", cb);
            if (textbox != null)
            {
                Border parent = (Border)textbox.Parent;
                if (validation == "")
                {
                    if (!float.TryParse(textbox.Text, out float x))
                        parent.Background = new SolidColorBrush(Colors.Pink);
                    else if (x > max || x < min)
                        parent.Background = new SolidColorBrush(Colors.Yellow);
                    else
                        parent.Background = new SolidColorBrush(Colors.LightGreen);
                }
                else if (validation == "Int")
                {
                    if (!int.TryParse(textbox.Text, out int x))
                        parent.Background = new SolidColorBrush(Colors.Pink);
                    else if (x > max || x < min)
                        parent.Background = new SolidColorBrush(Colors.Yellow);
                    else
                        parent.Background = new SolidColorBrush(Colors.LightGreen);
                }
                else if (validation == "Hex")
                {
                    try
                    {
                        uint newCharge = Convert.ToUInt32(textbox.Text, 16);
                        parent.Background = new SolidColorBrush(Colors.LightGreen);
                    }
                    catch
                    {
                        parent.Background = new SolidColorBrush(Colors.Pink);
                    }
                }
            }
        }

        public static Type[] GetArrayOfModuleTypes()
        {
            var listOfBs = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                            from assemblyType in domainAssembly.GetTypes()
                            where typeof(ModuleBase).IsAssignableFrom(assemblyType)
                            orderby assemblyType.Name
                            select assemblyType
                ).ToArray();
            List<Type> retVal = new List<Type>();
            foreach (var t in listOfBs)
            {
                if (t.Name != "ModuleBase")
                    retVal.Add(t);
            }
            return retVal.ToArray();
        }

    }
}
