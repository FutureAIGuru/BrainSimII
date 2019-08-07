using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace BrainSimulator
{
    public class Module2DModel : ModuleBase
    {
        public class physObject1
        {
            public int observedPixel;
            public Point P1;
            public float conf1, conf2;
            public Point P2;
            public Color theColor;
        }

        public List<physObject1> objects = new List<physObject1>();

        public bool AddSegment(Point P1, Point P2, float conf1, float conf2)
        {
            bool found = false;
            //is object already here?...adjust it
            for (int i = 0; i < objects.Count; i++)
            {
                //does the proposed touched overlap with the model segment?
                //if the slopes are different, they must be different
                double m1 = Math.Abs(Math.Atan2((P1 - P2).Y, (P1 - P2).X));
                double m2 = Math.Abs(Math.Atan2((objects[i].P1 - objects[i].P2).Y, (objects[i].P1 - objects[i].P2).X));
                double error = m1 - m2;
                if (Math.Abs(error) > 0.2 && Math.Abs(error - Math.PI) > 0.02)
                    continue;

                //determine if points are on a segment by calculating the distance to the 2 endpoints
                //if the sum of these distances equals the length of the segment, the point is on the segment
                bool p1IsOn = false, p2IsOn = false;
                double l1 = (P1 - objects[i].P1).Length;
                double l2 = (P1 - objects[i].P2).Length;
                double l3 = (objects[i].P1 - objects[i].P2).Length;
                error = l3 - (l1 + l2);
                if (Math.Abs(error) < 0.05) p1IsOn = true;//p1 is on the segment
                l1 = (P2 - objects[i].P1).Length;
                l2 = (P2 - objects[i].P2).Length;
                error = l3 - (l1 + l2);
                if (Math.Abs(error) < 0.05) p2IsOn = true;//p2 is on the segment
                if (p1IsOn || p2IsOn)  
                {
                    //if either point is on the segment, (since the slopes are the same), this is already in the model
                    found = true;
                    physObject1 save = objects[i];
                    objects[i].conf1 = objects[i].conf2 = 0;
                    Point min, max;

                    //merge the segments
                    if (Math.Abs(m1) > Math.PI / 4)
                    { //primarily vertical line...sort by y}
                        min = P1;
                        if (P2.Y < min.Y) min = P2;
                        if (objects[i].P1.Y < min.Y) min = objects[i].P1;
                        if (objects[i].P2.Y < min.Y) min = objects[i].P2;
                        max = P1;
                        if (P2.Y > max.Y) max = P2;
                        if (objects[i].P1.Y > max.Y) max = objects[i].P1;
                        if (objects[i].P2.Y > max.Y) max = objects[i].P2;
                        objects[i].P1 = min;
                        objects[i].P2 = max;
                    }
                    else
                    {//primarily horizontal line...sort by x
                        min = P1;
                        if (P2.X < min.X) min = P2;
                        if (objects[i].P1.X < min.X) min = objects[i].P1;
                        if (objects[i].P2.X < min.X) min = objects[i].P2;
                        max = P1;
                        if (P2.X > max.X) max = P2;
                        if (objects[i].P1.X > max.X) max = objects[i].P1;
                        if (objects[i].P2.X > max.X) max = objects[i].P2;
                        objects[i].P1 = min;
                        objects[i].P2 = max;
                    }
                    if (conf1 == 1 && objects[i].P1 == P1) objects[i].conf1 = 1;
                    if (conf1 == 1 && objects[i].P2 == P1) objects[i].conf2 = 1;
                    if (conf2 == 1 && objects[i].P1 == P2) objects[i].conf1 = 1;
                    if (conf2 == 1 && objects[i].P2 == P2) objects[i].conf2 = 1;
                    if (save.conf1 == 1 && objects[i].P1 == save.P1) objects[i].conf1 = 1;
                    if (save.conf1 == 1 && objects[i].P2 == save.P1) objects[i].conf2 = 1;
                    if (save.conf2 == 1 && objects[i].P1 == save.P2) objects[i].conf1 = 1;
                    if (save.conf2 == 1 && objects[i].P2 == save.P2) objects[i].conf2 = 1;
                }
            }
            if (!found)
            {
                //not already here?  Add it
                physObject1 newObject = new physObject1()
                {
                    P1 = P1,
                    P2 = P2,
                    conf1 = conf1,
                    conf2 = conf2,
                    theColor = Colors.Wheat
                };

                objects.Add(newObject);
            }
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
            return found;
        }
        public PolarVector FindLowConfidence()
        {
            PolarVector pv = null;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].conf1 == 0)
                {
                    pv = Utils.ToPolar(objects[i].P1);
                    break;
                }
                if (objects[i].conf2 == 0)
                {
                    pv = Utils.ToPolar(objects[i].P2);
                    break;
                }
            }
            return pv;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }
        public override void Initialize()
        {
            objects.Clear();
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }
        double ToDegrees(double radians)
        { return radians * 180 / Math.PI; }
        Point GetPoint(int i, int len)
        {
            float assumedDepth = 1f;
            i -= len / 2;
            float theta = (float)Utils.fieldOfView * i / (float)len;
            Point p1 = Utils.ToCartesian(new PolarVector() { theta = theta, r = assumedDepth });
            return p1;
        }

        public void Rotate(double theta) //(in radians * 1000)
        {
            //rotate all the objects
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].P1 = (Point)Utils.RotateVector((Vector)objects[i].P1, theta / 1000);
                objects[i].P2 = (Point)Utils.RotateVector((Vector)objects[i].P2, theta / 1000);
                //                objects[i].P1 = AddTheta(objects[i].P1, -theta / 1000f);
                //                objects[i].P2 = AddTheta(objects[i].P2, -theta / 1000f);
            }
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
            //turned = theta / 1000f;
        }
        public void Move(int motion)
        {
            //check for collisions
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].P1 = AddMotion(objects[i].P1, motion / 1000f);
                objects[i].P2 = AddMotion(objects[i].P2, motion / 1000f);
            }
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
            //moved = motion / 1000f;
        }
        Point AddTheta(Point p1, double dTheta)
        {
            PolarVector pv = new PolarVector();
            pv = Utils.ToPolar(p1);
            pv.theta += dTheta;
            return Utils.ToCartesian(pv);
        }
        Point AddMotion(Point p1, float dY)
        {
            p1.Y -= dY;
            return p1;
        }
    }
}



