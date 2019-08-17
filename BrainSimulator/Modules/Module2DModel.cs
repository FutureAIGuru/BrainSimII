using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace BrainSimulator
{
    public class PointPlus
    {
        public Point P;
        public float conf;
    }

    public class Module2DModel : ModuleBase
    {
        public class physObject1
        {
            public PointPlus P1;
            public PointPlus P2;
            public Color theColor;
        }

        public List<physObject1> objects = new List<physObject1>();


        public bool AddSegment(PointPlus P1, PointPlus P2)
        {
            bool found = false;
            bool modelChanged = false;
            //is object already here?...adjust it
            for (int i = 0; i < objects.Count; i++)
            {
                //if one of the points is not on the line...this is not the stored object we're looking for
                float d1 = Utils.DistancePointToLine(P1.P, objects[i].P1.P, objects[i].P2.P);
                float d2 = Utils.DistancePointToLine(P2.P, objects[i].P1.P, objects[i].P2.P);
                if (d1 > 0.1 || d2 > 0.1) continue;
                Point closest;

                //if both point are on the line, this is already in the model
                d1 = (float)Utils.FindDistanceToSegment(P1.P, objects[i].P1.P, objects[i].P2.P, out closest);
                d2 = (float)Utils.FindDistanceToSegment(P2.P, objects[i].P1.P, objects[i].P2.P, out closest);

                if (P1.conf != 1 && P2.conf != 1)
                    if (d1 < 0.1 && d2 < 0.1) return modelChanged; //segment already in

                //does the proposed touched overlap with the model segment?
                //if the slopes are different, they must be different

                found = true;

                //merge the segments  
                //an existing known  endpoint cannot be changed
                double m1 = Math.Abs(Math.Atan2((P1.P - P2.P).Y, (P1.P - P2.P).X));
                if (Math.Abs(m1) > Math.PI / 4)
                { //primarily vertical line...sort by y}
                    if (P1.P.Y > P2.P.Y)
                    {
                        PointPlus pTemp = P1;
                        P1 = P2;
                        P2 = pTemp;
                    }
                    if (objects[i].P1.P.Y > objects[i].P2.P.Y)
                    {
                        PointPlus pTemp = objects[i].P1;
                        objects[i].P1 = objects[i].P2;
                        objects[i].P2 = pTemp;
                    }
                    //extend lower?
                    if (objects[i].P1.conf == 0 && P1.P.Y < objects[i].P1.P.Y)
                    {
                        objects[i].P1 = P1;
                        na.GetNeuronAt("Change").SetValue(1);
                        modelChanged = true;
                    }
                    //extend upper?
                    if (objects[i].P2.conf == 0 && P2.P.Y > objects[i].P2.P.Y)
                    {
                        objects[i].P2 = P2;
                        na.GetNeuronAt("Change").SetValue(1);
                        modelChanged = true;
                    }
                }
                else
                {//primarily horizontal line...sort by x
                    if (P1.P.X > P2.P.X)
                    {
                        PointPlus pTemp = P1;
                        P1 = P2;
                        P2 = pTemp;
                    }
                    if (objects[i].P1.P.X > objects[i].P2.P.X)
                    {
                        PointPlus pTemp = objects[i].P1;
                        objects[i].P1 = objects[i].P2;
                        objects[i].P2 = pTemp;
                    }
                    if (objects[i].P1.conf == 0 && P1.P.X < objects[i].P1.P.X)
                    {
                        objects[i].P1 = P1;
                        na.GetNeuronAt("Change").SetValue(1);
                        modelChanged = true;
                    }
                    if (objects[i].P2.conf == 0 && P2.P.X > objects[i].P2.P.X)
                    {
                        objects[i].P2 = P2;
                        na.GetNeuronAt("Change").SetValue(1);
                        modelChanged = true;
                    }
                }
            }
            if (!found)
            {
                //not already here?  Add it
                physObject1 newObject = new physObject1()
                {
                    P1 = P1,
                    P2 = P2,
                    theColor = Colors.Wheat
                };

                objects.Add(newObject);
                na.GetNeuronAt("New").SetValue(1);
                modelChanged = true;
            }
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
            return modelChanged;
        }

        public PolarVector FindLowConfidence()
        {
            PolarVector pv = null;
            float nearest = float.MaxValue;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].P1.conf == 0)
                {
                    if (((Vector)objects[i].P1.P).Length < nearest)
                    {
                        pv = Utils.ToPolar(objects[i].P1.P);
                        nearest = (float)((Vector)objects[i].P1.P).Length;
                    }
                }
                if (objects[i].P2.conf == 0)
                {
                    if (((Vector)objects[i].P2.P).Length < nearest)
                    {
                        pv = Utils.ToPolar(objects[i].P2.P);
                        nearest = (float)((Vector)objects[i].P2.P).Length;
                    }
                }
            }
            return pv;
        }
        public bool IsAlreadyInModel(float theta, Color theColor)
        {
            int nearest = -1;
            float dist = float.MaxValue;
            for (int i = 0; i < objects.Count; i++)
            {
                //does this object cross the given visual angle?
                PolarVector pv = new PolarVector() { r = 10, theta = theta };
                pv.theta = Utils.ConvTheta(theta);
                Point p = Utils.ToCartesian(pv);
                Utils.FindIntersection(new Point(0, 0), p, objects[i].P1.P, objects[i].P2.P,
                    out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clos_p1, out Point close_p2, out double collisionAngle);
                if (!segments_intersect) continue;

                //and is it the nearest?
                Vector v = (Vector)intersection;
                if (v.Length < dist)
                {
                    nearest = i;
                    dist = (float)v.Length;
                }
            }
            if (nearest != -1 && objects[nearest].theColor == theColor)
            {
                return true;
            }
            return false;
        }

        public bool SetColor(float theta, Color theColor)
        {
            int nearest = -1;
            float dist = float.MaxValue;
            for (int i = 0; i < objects.Count; i++)
            {
                //has color already been assigned?
                if (objects[i].theColor != Colors.Wheat) continue;

                //does this object cross the given visual angle?
                PolarVector pv = new PolarVector() { r = 10, theta = theta };
                pv.theta = Utils.ConvTheta(theta);
                Point p = Utils.ToCartesian(pv);
                Utils.FindIntersection(new Point(0, 0), p, objects[i].P1.P, objects[i].P2.P,
                    out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clos_p1, out Point close_p2, out double collisionAngle);
                if (!segments_intersect) continue;

                //and is it the nearest?
                Vector v = (Vector)intersection;
                if (v.Length < dist)
                {
                    nearest = i;
                    dist = (float)v.Length;
                }
            }
            if (nearest != -1)
            {
                objects[nearest].theColor = theColor;
                na.GetNeuronAt("Color").SetValue(1);
                return true;
            }
            return false;
        }

        public bool imagination = false;
        PolarVector imaginationOffset;
        float imaginationDirection;
        public void ImagineStart(PolarVector offset, float direction)
        {
            if (imaginationOffset != null) ImagineEnd();
            imagination = true;
            imaginationOffset = offset;
            imaginationDirection = direction;
            Rotate((float)offset.theta);
            Move((float)offset.r);
            Rotate((float)(direction - offset.theta));
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }
        public void ImagineEnd()
        {
            if (!imagination) return;
            Rotate((float)(-imaginationDirection + imaginationOffset.theta));
            Move((float)-imaginationOffset.r);
            Rotate((float)-imaginationOffset.theta);
            imaginationDirection = 0;
            imaginationOffset = null;
            imagination = false;
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }



        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }
        public override void Initialize()
        {
            objects.Clear();
            na.GetNeuronAt(0, 0).Label = "New";
            na.GetNeuronAt(1, 0).Label = "Change";
            na.GetNeuronAt(2, 0).Label = "Color";

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


        public void Rotate(float theta)
        {
            //rotate all the objects in the model
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].P1.P = (Point)Utils.RotateVector((Vector)objects[i].P1.P, theta);
                objects[i].P2.P = (Point)Utils.RotateVector((Vector)objects[i].P2.P, theta);
            }
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }

        public void Move(float motion)
        {
            //move all the objects in the model
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].P1.P = AddMotion(objects[i].P1.P, motion);
                objects[i].P2.P = AddMotion(objects[i].P2.P, motion);
            }
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
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



