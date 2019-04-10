using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;


namespace BrainSimulator
{
    public class ModuleRealityModel : ModuleBase
    {
        //NEEDED:
        //eye position deltax,x (rotation)
        //body position deltax,z (translation)
        //absolute eye/body position to create simulated visual image

        //add new thing...delete object (list of things)
        //things have x,y,z, color, thingtype, etc. Enough properties to reconstruct?
        //things move with deltax,y
        //turn list of objects into presumed image
        //detect existing thing vs new thing vs changing thing

        class Thing
        {
            public int thingType;
            public int color;
            public float cx, cy; //(center) range 0 is dead ahead, +1 is behind around to right or straight up, -1 around to left (or straignt down)
            public float z; //range 0 is inside you, +1 is infinitely far away, .5 is 1 meter?
            public float sizeX, sizeY;//this is the actual object size  assumed at distance 5.        
            public bool allInFieldOfView;
            public float moved;
            public float sizeChanged;
            public virtual bool Equals(Thing t)
            {
                if (thingType == t.thingType && color == t.color) return true;
                return false;
            }
        }
        List<Thing> thingsInReality = new List<Thing>();

        override public void Fire()
        {
            Init();
            if (naIn == null) return;

            for (int i = 1; i < naIn.Rows; i++)
            {
                //are there objects in the visual field?
                if (naIn.GetNeuronAt(0, i).LastCharge == 0) break; //end of shapes in visual field


                Thing CT = new Thing();
                CT.thingType = 0; //we must convert to angular distances
                CT.z = 5;//we have no idea how far away it is...just its apparent size.

                CT.color = naIn.GetNeuronAt(0, i).LastChargeInt;

                //sizes are given [0,1] in the field of view which is 60-degrees or +/-.16
                CT.sizeX = (naIn.GetNeuronAt(1, i).LastCharge - .5f) / 3;
                CT.sizeY = (naIn.GetNeuronAt(2, i).LastCharge - .5f) / 3;
                CT.cx = (naIn.GetNeuronAt(3, i).LastCharge - .5f) / 3;
                CT.cy = (naIn.GetNeuronAt(4, i).LastCharge - .5f) / 3;
                CT.allInFieldOfView = naIn.GetNeuronAt(5, i).LastCharge == 1;
                CT.thingType = (int)naIn.GetNeuronAt(6, i).LastCharge;

                int foundIndex = -1;
                for (int j = 0; j < thingsInReality.Count; j++)
                {
                    Thing t = thingsInReality[j];
                    //only care about things which might be fully in visual field
                    //camera field of view is 60-degrees so should be x=[-.1666,.1666] 

                    if (CT.color == t.color)
                    {
                        if (CT.allInFieldOfView)
                        {
                            //the object is there but has it moved ?
                            while (CT.sizeX - t.sizeX > 0.02 && CT.sizeY - t.sizeY > 0.02) //object is closer (or grown)
                            {
                                float oldz = t.z;
                                t.z -= .1f; //assume the object hasn't moved and adjust the model
                                t.sizeX *= t.z / oldz;
                                t.sizeY *= t.z / oldz;
                                t.sizeChanged = 1;
                            }
                            if (CT.sizeX - t.sizeX < -0.02 && CT.sizeY - t.sizeY < -0.02) //object is further (or shrunk)
                            {
                                float oldz = t.z;
                                t.z += .1f; //assume the object hasn't moved and adjust the model
                                t.sizeX *= t.z / oldz;
                                t.sizeY *= t.z / oldz;
                                t.sizeChanged = 1;
                            }
                            if (!Utils.Close(CT.cx, t.cx) || !Utils.Close(CT.cy, t.cy)) //object has moved
                            {
                                //moved is handled elsewhere
                            }
                        }
                        foundIndex = i;

                        //update the values to match what we see
                        t.cx = CT.cx;
                        t.cy = CT.cy;
                        break; //found a match, quit looking
                    }
                }
                //Object was not found, add it 
                if (foundIndex == -1)
                {
                    thingsInReality.Add(CT);
                }
            }
            //find missing objects  is there anything which should be in the visual field which isnt?

            //update the neuron values for the reality model
            na.ClearNeuronChargeInArea();
            for (int i = 0; i < thingsInReality.Count; i++)
            {
                Thing t = thingsInReality[i];
                int xVal = (int)((t.cx / 2 + 0.5) * na.Width);
                while (na.GetNeuronAt(0, xVal).LastChargeInt != 0) xVal++;
                na.GetNeuronAt(xVal, 0).SetValueInt(t.color);
                na.GetNeuronAt(xVal, 1).SetValue(t.cy);
                na.GetNeuronAt(xVal, 2).SetValue(t.z); //need to range properly
                na.GetNeuronAt(xVal, 3).SetValue(t.sizeX);
                na.GetNeuronAt(xVal, 4).SetValue(t.sizeY);
            }
        }


        public static void SphericalToCartesian(float radius, float polar, float elevation, out Vector3D outCart)
        {
            float a = radius * (float)Math.Cos(elevation);
            outCart = new Vector3D();
            outCart.X = a * Math.Cos(polar);
            outCart.Y = radius * Math.Sin(elevation);
            outCart.Z = a * Math.Sin(polar);
        }


        public static void CartesianToSpherical(Vector3D cartCoords, out float outRadius, out float outPolar, out float outElevation)
        {
            if (cartCoords.X == 0)
                cartCoords.X = float.MinValue;
            outRadius = (float)Math.Sqrt((cartCoords.X * cartCoords.X)
                            + (cartCoords.Y * cartCoords.Y)
                            + (cartCoords.Z * cartCoords.Z));
            outPolar = (float)Math.Atan(cartCoords.Z / cartCoords.X);
            if (cartCoords.X < 0)
                outPolar += (float)Math.PI;
            outElevation = (float)Math.Asin(cartCoords.Y / outRadius);
        }

        public void Move(int amount)
        {
            //current position is (0,0,0)  Motion makes it (0,0,z)
            //so we need to add z to all the components
            //then we'll be able to ask whether the size change of the object when next view matche up or needs to be adjusted
            foreach (Thing t in thingsInReality)
            {
                //convert to cartesian
                float oldZ = t.z;
                t.cx *= (float)Math.PI;//Convert to radians
                t.cy *= (float)Math.PI / 2;

                SphericalToCartesian(t.z, t.cx, t.cy, out Vector3D position);
                //                SphericalToCartesian(t.z, 0, 0, out Vector3D position);

                //add
                position.X -= amount / 1000f;

                //convert back
                CartesianToSpherical(position, out t.z, out t.cx, out t.cy);

                t.cx /= (float)Math.PI;//convert back from radians
                t.cy /= (float)Math.PI / 2;

                if (t.z < 0)
                {
                    t.z = -t.z;
                    t.cx = t.cx + 1;
                }

                LimitCoordinates(ref t.cx);

                if (Math.Abs(t.z) < 0.1) //too close for comfort
                {
                    t.sizeX *= oldZ / t.z;
                    t.sizeY *= oldZ / t.z;
                    t.moved = oldZ / t.z;
                }
            }
        }
        public void Rotate(int amount)
        {
            //amount is in .001 radians.  
            float factor = (float)Math.PI * 1000f;
            foreach (Thing t in thingsInReality)
            {
                t.cx += amount / factor;
                LimitCoordinates(ref t.cx);
            }
        }
        private static void LimitCoordinates(ref float x)
        {
            if (x < -1) x += 2;
            if (x > 1) x -= 2;
        }



    }
}
