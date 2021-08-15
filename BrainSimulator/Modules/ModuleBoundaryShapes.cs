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
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleBoundaryShapes : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundaryShapes()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }

        ModuleUKS uks = null;
        public int cornerCt = 0;
        public int shapeCt = 0;
        public int relationshipCount = 0;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here  
            //return;

            ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return;
            uks = (ModuleUKS)naSource.TheModule;


            foreach (Thing t in uks.Labeled("VisibleArea").Children)
            {
                Thing tempShape = CreateTempShapeFromArea(t);
                if (tempShape == null) continue;
                Thing bestMatch = MatchAreaToStoredShapes(tempShape, out float score);
                if (bestMatch != null && score < 0.5)
                {
                    t.AddReference(bestMatch);
                    DeleteTempShape(tempShape);
                }
                else
                {
                    AddShapeFromArea(tempShape);
                    tempShape.Label = "Shape'" + shapeCt++;
                    t.AddReference(tempShape);
                }
            }


            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        Thing MatchAreaToStoredShapes(Thing t, out float score)
        {
            score = float.MaxValue;
            Thing bestMatch = null;
            float bestScore = float.MaxValue;
            Thing areaParent = uks.GetOrAddThing("Area","Shape");
            if (areaParent == null) return null;
            foreach (Thing t1 in areaParent.Children)
            {
                if (t1.Label.IndexOf("Shape") == 0)
                {
                    float matchScore = GetMatchScore(t1, t);
                    if (matchScore < bestScore)
                    {
                        bestScore = matchScore;
                        bestMatch = t1;
                    }
                }
            }
            score = bestScore;
            return bestMatch;
        }

        float GetMatchScore(Thing t1, Thing t2)
        {
            float retVal = 20;

            //sequence of angles
            List<PointPlus> shapeList1 = GetListFromShape(t1);
            List<PointPlus> shapeList2 = GetListFromShape(t2);

            int count = Math.Min(shapeList1.Count, shapeList2.Count);
            if (shapeList2.Count > shapeList1.Count)
            {
                List<PointPlus> temp = shapeList1;
                shapeList1 = shapeList2;
                shapeList2 = temp;
            }

            //shapelist 2 is shorter (or equal)
            float bestError = float.MaxValue;
            //compare the lists at all possible offsets
            for (int offset = 0; offset < count; offset++)
            {
                //compare list by accumulating error in the polygon
                float errorTot = 0;
                for (int i = 0; i < count; i++)
                {
                    errorTot += (shapeList1[(i+offset)%shapeList1.Count] - shapeList2[i]).R;
                }
                if (errorTot < bestError)
                    bestError = errorTot;
            }
            retVal = bestError;

            return retVal;
        }

        List<PointPlus> GetListFromShape(Thing theShape)
        {
            List<PointPlus> retVal = new List<PointPlus>();
            if (!theShape.Label.Contains("Shape")) return retVal;

            Thing firstCorner = theShape.Children[0];
            Thing curCorner = theShape.Children[0];
            do
            { //TODO what if the references are out of order?
                Thing theAngle = curCorner.References[0].T;
                Thing theLength = curCorner.References[1].T.Parents[0];
                float.TryParse(theAngle.Label.Substring(3), out float theAngle1);
                float.TryParse(theLength.Label.Substring(3), out float theLength1);

                PointPlus pp = new PointPlus { R = theLength1, Theta = new Angle(0).FromDegrees(theAngle1) };
                retVal.Add(pp);
                curCorner = curCorner.References[1].T.References[0].T;
            } while (curCorner != firstCorner);

            return retVal;
        }

        //Areas have absolute coordinates, Shapes are completely relative
        Thing CreateTempShapeFromArea(Thing theArea)
        {
            foreach(Thing t in theArea.Children)
            {
                if (t.Children.Count == 0)
                    return null;
            }
            if (!theArea.Label.Contains("Area")) return null;
            //this abstracts a shape from a specific area
            Thing theShape = uks.AddThing("TempShape", new Thing[] { }); //the resultant shape

            //Areas are randomly ordered. We want the Shape to be in sequence around the shape because that's easier to search

            //first find the top/left corner
            Thing topLeft = null;

            float greatestLength = uks.GetValues(theArea)["Siz+"];
            for (int i = 0; i < theArea.Children.Count; i++)
            {
                Thing corner = theArea.Children[i];
                if (corner.Parents.Contains(uks.Labeled("Corner")))
                {
                    if (topLeft == null) topLeft = corner;
                    Point p1 = (Point)corner.References[0].T.Children[0].V;
                    Point p2 = (Point)corner.References[1].T.Children[0].V;

                    if (p1.Y < ((Point)topLeft.Children[0].V).Y ||
                       (p1.Y == ((Point)topLeft.Children[0].V).Y && p1.X < ((Point)topLeft.Children[0].V).X))
                    {
                        topLeft = theArea.Children[i];
                    }
                }
            }

            //these both are for the area
            Thing nextAreaCorner = topLeft.References[0].T;
            Thing prevAreaCorner = topLeft;

            Thing newShapeCorner = uks.AddThing("nCrnr" + cornerCt++, "Corner");
            Thing firstShapeCorner = newShapeCorner;
            
            while (nextAreaCorner != topLeft)
            {
                theShape.AddChild(newShapeCorner);

                //which area reference points to the next corner (not the previous)
                int refIndex = 0;
                if (nextAreaCorner.References[0].T == prevAreaCorner)
                    refIndex = 1;
                Thing nextShapeCorner = AddAngleandRelationship(greatestLength, nextAreaCorner, newShapeCorner, refIndex);

                newShapeCorner = nextShapeCorner;

                prevAreaCorner = nextAreaCorner;
                nextAreaCorner = nextAreaCorner.References[refIndex].T; //which way do we go?
            }

            theShape.AddChild(newShapeCorner);
            int refIndex1 = 0;
            if (nextAreaCorner.References[0].T == prevAreaCorner)
                refIndex1 = 1;
            AddAngleandRelationship(greatestLength, nextAreaCorner, newShapeCorner, refIndex1,firstShapeCorner);

            return theShape;
        }

        private Thing AddAngleandRelationship(float greatestLength, Thing nextAreaCorner, Thing newShapeCorner, int refIndex,Thing nextShapeCorner = null)
        {

            //find the angle
            Point cnrPt = (Point)nextAreaCorner.Children[0].V;
            Point p1 = (Point)nextAreaCorner.References[0].T.Children[0].V;
            Point p2 = (Point)nextAreaCorner.References[1].T.Children[0].V;
            ModuleBoundarySegments.Arc seg1 = new ModuleBoundarySegments.Arc { p1 = cnrPt, p2 = p1 };
            ModuleBoundarySegments.Arc seg2 = new ModuleBoundarySegments.Arc { p1 = cnrPt, p2 = p2 };
            Angle a = Math.Abs(seg1.Angle - seg2.Angle);
            a = a.FromDegrees((float)(Math.Round(a.ToDegrees() / 15) * 15));
            string s = "Ang" + (int)a.ToDegrees();
            Thing value = uks.GetOrAddThing("Value", "Thing");
            newShapeCorner.AddReference(uks.GetOrAddThing(s,value));

            float lenToNextCorner;
            if (refIndex == 0)
                lenToNextCorner = seg1.Length / greatestLength;  //normalize the lengths
            else
                lenToNextCorner = seg2.Length / greatestLength;  //normalize the lengths
            lenToNextCorner = (float)Math.Round(lenToNextCorner, 1);
            s = "Len" + lenToNextCorner.ToString("f1");
            if (nextShapeCorner == null)
                nextShapeCorner = uks.AddThing("nCrnr" + cornerCt++, "Corner");

            uks.AddRelationship(newShapeCorner, nextShapeCorner, uks.GetOrAddThing(s,value));
            return nextShapeCorner;
        }

        void DeleteTempShape(Thing theTempShape)
        {
            foreach (Thing corner in theTempShape.Children)
            {
                for (int i = 0; i < corner.References.Count; i++)
                {
                    Link L = corner.References[i];
                    if (!L.T.Parents.Contains(uks.Labeled("Value"))) //only delete relationships
                        uks.DeleteThing(L.T);
                }
            }
            uks.DeleteAllChilden(theTempShape);
            uks.DeleteThing(theTempShape);
        }

        void AddShapeFromArea(Thing area)
        {
            area.AddParent(uks.GetOrAddThing("Area","Shape"));
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return;
            uks = (ModuleUKS)naSource.TheModule;

            uks.GetOrAddThing("Area", "Shape");
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (na == null) return; //this is called the first time before the module actually exists
        }
    }
}
