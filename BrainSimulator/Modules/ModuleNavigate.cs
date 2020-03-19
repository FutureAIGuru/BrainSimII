//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleNavigate : ModuleBase
    {
        //state: (this is needed to cope with the parallel operation with other modules
        //Idle (moving forward & scanning)...monitor for:
        // rediscovering landmarks
        // obstacles requiring change in direction
        //busy turning...waiting for a turn to complete
        //busy orienting..waiting for alignement with a landmark angle

        bool turning = false;
        bool scanning = false;
        bool orienting = false;
        bool auto = false; //for debugging in manual mode
        bool goingToGoal = false;

        //counters used to create unique labels for landmarks
        int pointCount = 0;
        int situationCount = 0;
        int landmarkCount = 0;
        int pairCount = 0;
        string colorFired = "";

        Thing lastLandmark = null;
        Thing currentLandmark = null;
        Thing currentSituation = null;
        Thing currentTargetReached = null;
        Thing currentTarget = null;

        //we'll use this to creat a random selection of behaviors
        Random rand = new Random((int)DateTime.Now.Ticks);
        string[] options = new string[] { "RTurnS", "LTurnS", "UTurnS", "GoS" };

        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (colorFired == "")
            {
                foreach (Neuron n in na.Neurons())
                {
                    if (n.Fired() && n.Label.IndexOf("c") == 0 && n.Model == Neuron.modelType.Std)
                    {
                        colorFired = n.Label;
                    }
                }
            }

            auto = (GetNeuronValue(null, "Auto") == 1) ? true : false;
            goingToGoal = (GetNeuronValue(null, "Goal") == 1) ? true : false;

            Module2DKBN kb = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            Module2DModel nmModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (kb == null) return;
            if (nmModel == null) return;
            ModuleBehavior nmBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            if (nmBehavior == null) return;


            //is there an existing landmark? Do I recognize I'm near a spot I was before?
            //this only calculates "R" so it is rotation-independent

            Thing best = null;
            float bestDist = 1000;
            List<Thing> near = nmModel.NearbySegments();
            FindBestLandmarkMatch(kb, ref best, ref bestDist, near);
            nmModel.FireVisibleObjects(); //not really needed
            if (bestDist < .1f) //are we near a landmark we've been at before?
            {
                SetNeuronValue(null, "Found", 1);
                if (best != null && bestDist < .5f)
                {
                    //we have returned to a landmark we've been at before
                    currentLandmark = best;
                    currentSituation = currentLandmark.ReferencedBy[0].T;
                }

                //do we need to reorient ourselves to face the same way as we did before?
                if (!turning && lastLandmark != currentLandmark && currentLandmark != null)
                {
                    lastLandmark = currentLandmark;
                    float totalAngleError = GetTotalAngleError(near);
                    if (totalAngleError > 0.1f)
                    {
                        //we have returned to a landmark, turn to match the previous orientation
                        orienting = true;
                    }
                }
            }
            else
            {
                currentLandmark = null;
                currentSituation = null;
                lastLandmark = null;
            }

            if (orienting)
            {
                if (GetNeuronValue("ModuleBehavior", "Done") == 1)
                {
                    if (lastLandmark == null)
                    {
                        orienting = false;
                        return;
                    }
                    float totalAngleError = GetTotalAngleError(near);
                    if (totalAngleError > 0.25f && auto)
                    {
                        //keep turning until the angular error gets small
                        float angle = (float)PI / 2;
                        //if (totalAngleError > 0)
                        //    angle = -angle;
                        nmBehavior.TurnTo(angle);
                        return;
                    }
                    if (totalAngleError < 0.1f)
                    {
                        orienting = false;
                        return;
                    }
                }
            }

            if (turning)
            {
                if (GetNeuronValue("ModuleBehavior", "Done") == 1)
                {
                    turning = false;
                }
                return;
            }

            if (goingToGoal && !turning && GetNeuronValue("ModuleBehavior", "Done") == 1)
            {
                if (colorFired != "")
                {
                    currentTarget = kb.Labeled(colorFired);
                    colorFired = "";
                }
                if (currentSituation != null) //currentSituation means we're at a decision point...check for the decision and execute it
                {
                    if (currentTarget == null)
                        currentTarget = kb.Labeled(colorFired);
                    Thing action = GoToGoal(currentTarget);
                    if (action != null)
                    {
                        float angle = DoAction(action);
                        if (angle != 0)
                        {
                            nmBehavior.TurnTo(angle);
                            turning = true;
                        }
                        nmBehavior.MoveTo(1);
                        colorFired = "";
                        currentSituation = null;
                        return;
                    }
                }
                else
                {
                    goingToGoal = false;
                    //currentTarget = null;
                }
            }




            //decide which way to turn at an obstacle...this is the beef
            if (!scanning && !turning && !orienting && GetNeuronValue("ModuleBehavior", "Done") == 1)
            {
                //I am up to an obstacle...is there a decision or can I only go one way
                float distAhead = nmModel.GetDistanceAtDirection(0);
                float distLeft = nmModel.GetDistanceAtDirection((float)PI / 2);
                float distRight = nmModel.GetDistanceAtDirection((float)-PI / 2);
                bool canGoAhead = distAhead > 1;
                bool canGoLeft = distLeft > 1;
                bool canGoRight = distRight > 1;
                int options = (canGoAhead ? 1 : 0) + (canGoRight ? 1 : 0) + (canGoLeft ? 1 : 0);

                //First determine if there is a decision to be made or if there is only one option
                if (options == 1 && auto)
                {
                    //we have not choice but to follow the path
                    if (canGoAhead)
                    {
                        nmBehavior.MoveTo(1);
                        return;
                    }
                    if (canGoLeft)
                    {//TODO figure out why thetas seem to be reversed
                        nmBehavior.TurnTo((float)-PI / 2);
                        nmBehavior.MoveTo(1);
                        turning = true;
                        return;
                    }
                    if (canGoRight)
                    {
                        nmBehavior.TurnTo((float)PI / 2);
                        nmBehavior.MoveTo(1);
                        turning = true;
                        return;
                    }
                }
                else if (options == 0 && auto)
                {
                    //we're trapped...note the color ahead
                    currentTargetReached = nmModel.GetColorAtDirection(0);
                    Neuron n1 = na.GetNeuronAt(currentTargetReached.Label);
                    if (n1 == null)
                    {
                        n1 = AddLabel(currentTargetReached.Label + " ");
                        n1.Model = Neuron.modelType.Color;
                        n1.SetValueInt((int)currentTargetReached.V);
                        na.GetNeuronLocation(n1, out int X, out int Y);
                        Neuron n2 = na.GetNeuronAt(X + 1, Y);
                        n2.Label = currentTargetReached.Label;
                    }
                    if (currentTargetReached == currentTarget)
                    {
                        currentSituation = null;
                    }
                    else 
                    {
                        //make a U-Turn and start backtracking
                        nmBehavior.TurnTo((float)-PI);
                        turning = true;
                    }
                }
                else if (auto && !goingToGoal)
                {
                    //we have a choice...

                    //if the current landmark is null, create a new landmark & situation
                    if (currentLandmark == null)
                    {
                        //Create new Landmark...it clones the points so they are not modified by the model module
                        //By having points which don't change, we can determine if we've returned to a same position by calculating the deltas.
                        Thing newLandmark = kb.AddThing("Lm" + landmarkCount++.ToString(), "Landmark");

                        foreach (Thing t in near)
                        {
                            Segment s = Module2DModel.SegmentFromKBThing(t);
                            Thing P1 = kb.AddThing("lmp" + pointCount++.ToString(), "SPoint");
                            P1.V = new PointPlus() { R = s.P1.R, Theta = s.P1.Theta };
                            Thing P2 = kb.AddThing("lmp" + pointCount++.ToString(), "SPoint");
                            P2.V = new PointPlus() { R = s.P2.R, Theta = s.P2.Theta };
                            Thing S = kb.AddThing("l" + t.Label.ToString(), "SSegment");
                            S.AddReference(P1);
                            S.AddReference(P2);
                            S.AddReference(t.References[2].T);//the color
                            newLandmark.AddReference(S);
                        }
                        currentSituation = kb.AddThing("Si" + situationCount++.ToString(), "Situation");
                        currentSituation.AddReference(newLandmark);
                        currentLandmark = newLandmark;
                    }
                    else
                    {
                        //we have arrived back at a decision point...store the outcome we received
                        if (currentTargetReached != null && currentTargetReached != currentSituation)
                        {
                            Thing newOutcomePair = kb.AddThing("x" + pairCount++, currentSituation.Label);
                            newOutcomePair.AddReference(currentSituation.References[currentSituation.References.Count - 1].T);
                            newOutcomePair.AddReference(currentTargetReached);
                            //to connection to decision points, we need pointers both ways
                            if (currentTargetReached.Parents[0] == kb.Labeled("Situation"))
                            {
                                Thing newOutcomePair1 = kb.AddThing("x" + pairCount++, currentTargetReached.Label);
                                newOutcomePair1.AddReference(currentTargetReached.References[currentTargetReached.References.Count - 1].T);
                                newOutcomePair1.AddReference(currentSituation);
                            }
                        }
                        currentTargetReached = currentSituation;
                    }

                    //Decide which untried path to take
                    //priorities...1)continue ahead 2)left 3)right TODO could be randomized
                    string newAction = "";
                    float angle = 0;
                    if (currentSituation.References.Find(l1 => l1.T.Label == "GoS") == null && canGoAhead)
                    {
                        angle = 0;
                        newAction = "GoS";
                    }
                    else if (currentSituation.References.Find(l1 => l1.T.Label == "LTurnS") == null && canGoLeft)
                    {
                        angle = -(float)PI / 2;
                        newAction = "LTurnS";
                    }
                    else if (currentSituation.References.Find(l1 => l1.T.Label == "RTurnS") == null && canGoRight)
                    {
                        angle = (float)PI / 2;
                        newAction = "RTurnS";
                    }
                    else if (currentSituation.References.Find(l1 => l1.T.Label == "UTurnS") == null)
                    {
                        //assume we can always go back the way  we came
                        angle = (float)PI;
                        newAction = "UTurnS";
                    }
                    else
                    {
                        //tried all options
                    }
                    if (newAction != "")
                    {
                        currentSituation.AddReference(kb.Labeled(newAction), .1f);
                        nmBehavior.TurnTo(angle);
                        nmBehavior.MoveTo(1);
                        turning = true;
                    }
                    lastLandmark = currentLandmark;
                    return;
                }
            }

            //default to scanning and moving forward
            else if (GetNeuronValue("ModuleBehavior", "Done") == 1)
            {
                if (scanning)
                {
                    scanning = false;
                    SetNeuronValue("ModuleBehavior", "Done", 0); //this is here because clearing the bit lags by a cycle...bit of a hack
                    nmBehavior.MoveTo(1);
                }
                else if (auto && !scanning)
                {
                    SetNeuronValue("ModuleBehavior", "Done", 0); //this is here because clearing the bit lags by a cycle...bit of a hack
                    nmBehavior.Scan();
                    scanning = true;
                }
            }
        }

        private float GetTotalAngleError(List<Thing> near)
        {
            if (lastLandmark == null) return 10000;
            float totalAngleError = 1000;
            Segment s1 = Module2DModel.SegmentFromKBThing(lastLandmark.References[0].T);
            foreach (Thing t1 in near)
            {
                //does it match one of the segments presently near me?
                Segment s2 = Module2DModel.SegmentFromKBThing(t1);
                if (s1.theColor == s2.theColor)
                {
                    float angle = (float)Abs(s1.MidPoint().Theta - s2.MidPoint().Theta);
                    if (totalAngleError == 1000) totalAngleError = 0;
                    totalAngleError += angle;
                }
            }
            return totalAngleError;
        }

        private static void FindBestLandmarkMatch(Module2DKBN kb, ref Thing best, ref float bestDist, List<Thing> near)
        {
            //searching each spatial situation 
            best = null;
            bestDist = 1000;
            if (kb == null) return;
            if (kb.Labeled("Landmark") == null) return;
            List<Thing> landmarks = kb.Labeled("Landmark").Children;
            foreach (Thing t in landmarks)
            {
                if (t.References.Count > 0)
                {
                    float totalDist = 0;
                    //each child object in that landmark
                    int foundCount = 0;  //must match at least two items to count as a hit
                    foreach (Link L1 in t.References)
                    {
                        Segment s1 = Module2DModel.SegmentFromKBThing(L1.T);
                        foreach (Thing t1 in near)
                        {
                            //does it match one of the segments presently near me?
                            Segment s2 = Module2DModel.SegmentFromKBThing(t1);
                            if (s1.theColor == s2.theColor)
                            {
                                foundCount++;
                                float dist = (float)Abs(((Vector)s1.MidPoint().P).Length - ((Vector)s2.MidPoint().P).Length);
                                totalDist += dist;
                            }
                        }
                    }
                    if (foundCount < near.Count) totalDist = 1000; //didn't find enough matches
                    if (foundCount < t.References.Count && t.References.Count < near.Count) totalDist = 1000; //didn't find enough matches
                    if (totalDist < bestDist)
                    {
                        best = t;
                        bestDist = totalDist;
                    }
                }
            }
        }

        private float DoAction(Thing action)
        {
            float angle = 0;
            if (action.Label == "GoS")
            {
                angle = 0;
            }
            else if (action.Label == "LTurnS")
            {
                angle = -(float)PI / 2;
            }
            else if (action.Label == "RTurnS")
            {
                angle = (float)PI / 2;
            }
            else if (action.Label == "UTurnS")
            {
                //assume we can always go back the way  we came
                angle = (float)PI;
            }
            return angle;
        }

        //This returns the action needed at the currentSituation in order to move toward the goal
        //the KB search is recursive
        public Thing GoToGoal(Thing goal, List<Thing> visited = null)
        {
            if (goal == null) return null;
            Module2DKBN kb = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            foreach (Thing situation in kb.Labeled("Situation").Children)
            {
                if (situation.Label.IndexOf("S") == 0 && (visited == null || !visited.Contains(situation)))
                {
                    foreach (Thing outcomePair in situation.Children)
                    {
                        Link destDecision = outcomePair.References.Find(l => l.T == goal);
                        if (outcomePair.References[1].T == goal)
                       // if (destDecision != null)
                        {
                            //we found the goal...trace back
                            if (situation == currentSituation)
                            {
                                Thing action = outcomePair.References[0].T;
                                return action;
                            }
                            else
                            {
                                if (visited == null) visited = new List<Thing>();
                                visited.Add(situation);
                                Thing retVal = GoToGoal(situation, visited);
                                if (retVal != null)
                                    return retVal;
                                visited.Remove(situation);
                            }
                        }
                    }
                }
            }
            return null;
        }

        public override void Initialize()
        {
            scanning = false;
            turning = false;
            orienting = false;
            auto = false;

            lastLandmark = null;
            currentLandmark = null;
            currentSituation = null;
            ClearNeurons();
            //randomize
            rand = new Random((int)DateTime.Now.Ticks);
            AddLabel("Auto");
            AddLabel("Found");
            AddLabel("Goal");

            //this is the first test of building a sequence of behaviors
            Module2DKBN kb = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            if (kb != null)
            {
                Thing t = kb.AddThing("LTurnS", "Action");
                t = kb.AddThing("RTurnS", "Action");
                t = kb.AddThing("UTurnS", "Action");
                t = kb.AddThing("GoS", "Action");
            }
        }
    }
}
