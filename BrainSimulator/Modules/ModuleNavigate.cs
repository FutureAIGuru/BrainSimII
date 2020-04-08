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
        //Auto Mode:  searching the maze
        //goingtogoal: returning to a known endpoint

        //        bool scanning = false;  //used with vision
        bool orienting = false;
        bool auto = false; //for debugging in manual mode
        bool goingToGoal = false;

        //counters used to create unique labels for landmarks
        int pointCount = 0;
        int situationCount = 0;
        int landmarkCount = 0;
        int pairCount = 0;

        Thing lastLandmark = null;
        Thing currentLandmark = null;
        Thing currentSituation = null;
        Thing currentTargetReached = null;
        Thing currentTarget = null;
        Thing lastBest = null;
        int orientationCount = 0;

        //we'll use this to creat a random selection of behaviors
        Random rand = new Random((int)DateTime.Now.Ticks);

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //get the external references
            Module2DKBN kb = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            Module2DModel nmModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (kb == null) return;
            if (nmModel == null) return;
            ModuleBehavior nmBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            if (nmBehavior == null) return;


            //check on the various input neurons...
            //check for a goal selection
            foreach (Neuron n in na.Neurons())
            {
                if (n.Fired() && n.Label.IndexOf("c") == 0 && n.Model == Neuron.modelType.Std)
                {
                    currentTarget = kb.Labeled(n.Label);
                }
            }

            //don't do anything while a behavior is in progress
            if (GetNeuronValue("ModuleBehavior", "Done") == 0) return;

            //check for operating mode
            auto = (GetNeuronValue(null, "Auto") == 1) ? true : false;
            goingToGoal = (GetNeuronValue(null, "Goal") == 1) ? true : false;


            //is there an existing landmark? Do I recognize I'm near a spot I was before?
            //this only calculates "R" so it is rotation-independent

            Thing best = null;
            float bestDist = 1000;

            List<Thing> near = nmModel.NearbySegments(3);

            FindBestLandmarkMatch(kb, ref best, ref bestDist, near);

            nmModel.FireVisibleObjects(); //not really needed

            if (bestDist < .2f) //are we near a landmark we've been at before?
            {
                SetNeuronValue(null, "Found", 1);

                //yse, we have returned to a landmark we've been at before
                currentLandmark = best;
                currentSituation = currentLandmark.ReferencedBy[0].T;

                //do we need to reorient ourselves to face the same way as we did before?
                if (lastLandmark != currentLandmark)
                {
                    lastLandmark = currentLandmark;
                    orienting = true;
                    orientationCount = 0;
                }
            }
            else
            {
                //we're not near an existing landmark
                currentLandmark = null;
                currentSituation = null;
                lastLandmark = null;
            }

            //this is our first time back at a landmark
            if (orienting)
            {
                Angle totalAngleError = GetTotalAngleError(near);
                if (Abs(totalAngleError) > PI / 2 - .1f)
                {
                    //turn in large increments until the angular error gets small
                    Angle angle = (float)PI / 2;
                    nmBehavior.TurnTo(angle);
                    orientationCount++;
                    return;
                }

                //locations are not very accurate and accumulate errors
                //whenever we encounter a landmark, we update the orientation/location of the model to correct errors
                //we're at a landmark and are closely oriented...correct the model for position first time only
                if (best != lastBest)
                {
                    //since we're oriented, corrected to any segment is OK
                    lastBest = best;
                    Segment s2 = Module2DModel.SegmentFromKBThing(near[0]);
                    Segment s1 = null;
                    foreach (Link l in best.References)
                    {
                        Segment s = Module2DModel.SegmentFromKBThing(l.T);
                        if (s.theColor == s2.theColor)
                        { s1 = s; break; }
                    }
                    if (s1 != null)
                    {
                        PointPlus m1 = s1.MidPoint();
                        PointPlus m2 = s2.MidPoint();
                        float deltaX = m1.X - m2.X;
                        float deltaY = m1.Y - m2.Y;
                        PointPlus a1 = new PointPlus() { P = (Point)(s1.P1.V - s1.P2.V) };
                        PointPlus a2 = new PointPlus() { P = (Point)(s2.P1.V - s2.P2.V) };
                        Angle deltaTheta = a1.Theta - a2.Theta;
                        //TODO: this move may also need to be a behavior thing if the sim module also gets an error
                        nmModel.Move(-deltaX, -deltaY);
                        nmBehavior.TurnTo(deltaTheta);
                        return;
                    }
                }
                else
                    orienting = false;
                return;
            }

            //we are in going-to-goal mode
            if (goingToGoal)
            {
                if (currentSituation != null) //currentSituation means we're at a decision point...check for the decision and execute it
                {
                    Thing action = GoToGoal(currentTarget);
                    if (action != null)
                    {
                        float angle = DoAction(action);
                        if (angle != 0)
                        {
                            nmBehavior.TurnTo(angle);
                        }
                        nmBehavior.MoveTo(1);
                        currentSituation = null;
                        return;
                    }
                }
                else
                {
                    goingToGoal = false;
                }
            }

            //we are in exploration mode
            //We're not at a known landmark
            //decide which way to turn at an obstacle
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
                {
                    nmBehavior.TurnTo((float)-PI / 2);
                    nmBehavior.MoveTo(1);
                    return;
                }
                if (canGoRight)
                {
                    nmBehavior.TurnTo((float)PI / 2);
                    nmBehavior.MoveTo(1);
                    return;
                }
            }
            else if (options == 0 && auto)
            {
                //we're trapped...note the color ahead and turn around
                currentTargetReached = nmModel.GetColorAtDirection(0);
                Neuron n1 = null;
                if (currentTargetReached != null) n1 = na.GetNeuronAt(currentTargetReached.Label);
                if (n1 == null && currentTargetReached != null)
                {
                    n1 = AddLabel(currentTargetReached.Label + " ");
                    n1.Model = Neuron.modelType.Color;
                    n1.SetValueInt((int)currentTargetReached.V);
                    na.GetNeuronLocation(n1, out int X, out int Y);
                    Neuron n2 = na.GetNeuronAt(X + 1, Y);
                    if (n2 != null)
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
                        Thing theAction = null;
                        switch (orientationCount) //what way did you have to turn to reorient...the reverse is the action you need to take
                        {
                            case 0: theAction = kb.Labeled("UTurnS"); break;
                            case 1: theAction = kb.Labeled("RTurnS"); break;
                            case 2: theAction = kb.Labeled("GoS"); break;
                            case 3: theAction = kb.Labeled("LTurnS"); break;
                        }

                        if (currentSituation.Children.Find(t1 => t1.References[0].T == theAction) == null)
                        {
                            Thing newOutcomePair = kb.AddThing("x" + pairCount++, currentSituation.Label);
                            newOutcomePair.AddReference(theAction);
                            newOutcomePair.AddReference(currentTargetReached);
                            //to connection two decision points, we need pointers both ways
                            if (currentTargetReached.Parents[0] == kb.Labeled("Situation"))
                            {
                                Thing newOutcomePair1 = kb.AddThing("x" + pairCount++, currentTargetReached.Label);
                                newOutcomePair1.AddReference(currentTargetReached.References[currentTargetReached.References.Count - 1].T);
                                newOutcomePair1.AddReference(currentSituation);
                            }
                        }
                        else //you can set a breakpoint here to detect problems
                            theAction = theAction;
                    }
                    currentTargetReached = currentSituation;
                }

                //Decide which untried path to take
                //priorities...1)continue ahead 2)left 3)right or randomized (depending on comment below)
                string newAction = "NoAction";
                Angle angle = 0;
                List<string> possibleActions = new List<string>();
                if (currentSituation.References.Find(l1 => l1.T.Label == "GoS") == null && canGoAhead)
                    possibleActions.Add("GoS");
                if (currentSituation.References.Find(l1 => l1.T.Label == "LTurnS") == null && canGoLeft)
                    possibleActions.Add("LTurnS");
                if (currentSituation.References.Find(l1 => l1.T.Label == "RTurnS") == null && canGoRight)
                    possibleActions.Add("RTurnS");
                if (possibleActions.Count == 0 && currentSituation.References.Find(l1 => l1.T.Label == "UTurnS") == null)
                    newAction = "UTurnS";
                else if (possibleActions.Count == 1) newAction = possibleActions[0];
                //for debugging, eliminate the ransomizatioin by alternately commenting the 2 stmts below
                //               else if (possibleActions.Count > 0) newAction = possibleActions[0];
                else if (possibleActions.Count > 0) newAction = possibleActions[rand.Next(possibleActions.Count)];
                switch (newAction)
                {
                    case "GoS":
                        angle = 0;
                        break;
                    case "UTurnS":
                        angle = PI;
                        break;
                    case "RTurnS":
                        angle = PI / 2;
                        break;
                    case "LTurnS":
                        angle = -PI / 2;
                        break;
                }

                if (newAction != "NoAction")
                {
                    currentSituation.AddReference(kb.Labeled(newAction), .1f);
                    nmBehavior.TurnTo(angle);
                    nmBehavior.MoveTo(1);
                }
                lastLandmark = currentLandmark;
                return;
            }

            //default to scanning and moving forward  Useful with vision which is current disabled
            else
            {
                //if (scanning)
                //{
                //    scanning = false;
                //    SetNeuronValue("ModuleBehavior", "Done", 0); //this is here because clearing the bit lags by a cycle...bit of a hack
                //    nmBehavior.MoveTo(1);
                //}
                //else if (auto && !scanning)
                //{
                //    SetNeuronValue("ModuleBehavior", "Done", 0); //this is here because clearing the bit lags by a cycle...bit of a hack
                //    nmBehavior.Scan();
                //    scanning = true;
                //}
            }
        }

        private Angle GetTotalAngleError(List<Thing> near)
        {
            if (lastLandmark == null) return 10000;
            Angle totalAngleError = 0;
            for (int i = 0; i < lastLandmark.References.Count; i++)
            {
                Segment s1 = Module2DModel.SegmentFromKBThing(lastLandmark.References[i].T);
                foreach (Thing t1 in near)
                {
                    //does it match one of the segments presently near me?
                    Segment s2 = Module2DModel.SegmentFromKBThing(t1);
                    if (s1.theColor == s2.theColor)
                    {
                        Angle m1 = s1.MidPoint().Theta;
                        Angle m2 = s2.MidPoint().Theta;
                        Angle angle = m1 - m2;
                        totalAngleError += Abs(angle);
                    }
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

            //landmark segments are sorted, closest segment first
            //closer segments are weightet to be more important
            List<Thing> landmarks = kb.Labeled("Landmark").Children;
            foreach (Thing landmark in landmarks)
            {
                if (landmark.References.Count > 0)
                {
                    float totalDist = 0;
                    //each reference in that landmark
                    int foundCount = 0;  //must match several items to count as a hit
                    int linkNumber = 0;
                    foreach (Link L1 in landmark.References)
                    {
                        Segment s1 = Module2DModel.SegmentFromKBThing(L1.T);
                        float d1 = (float)Utils.FindDistanceToSegment(s1);
                        int nearNumber = 0;
                        foreach (Thing t1 in near)
                        {
                            //does it match one of the segments presently near me?
                            Segment s2 = Module2DModel.SegmentFromKBThing(t1);
                            float d2 = (float)Utils.FindDistanceToSegment(s2);
                            if (s1.theColor == s2.theColor)
                            {
                                foundCount++;
                                float dist = (float)Abs(d1 - d2);
                                dist *= Abs(nearNumber - linkNumber) + 1;
                                totalDist += dist;
                            }
                            nearNumber++;
                        }
                        linkNumber++;
                    }
                    if (totalDist < bestDist && foundCount == near.Count)
                    {
                        best = landmark;
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

        //this returns a list of things directly connected to a target object
        private List<Thing> FindGoal(Thing target)
        {
            if (target == null) return null;
            Module2DKBN kb = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            List<Thing> placesToTry = new List<Thing>();

            foreach (Thing situation in kb.Labeled("Situation").Children)
            {
                foreach (Thing outcomePair in situation.Children)
                {
                    if (outcomePair.References.Count > 1 && outcomePair.References[1].T == target)
                    {
                        placesToTry.Add(situation);
                    }
                }
            }
            return placesToTry;
        }

        //This returns the action needed at the currentSituation in order to move toward the goal
        public Thing GoToGoal(Thing goal)
        {
            if (goal == null) return null;
            Module2DKBN kb = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));

            //trivial case, you can go straight to goal
            Thing pair = currentSituation.Children.Find(t => t.References[1].T == goal);
            if (pair != null)
                return pair.References[0].T;

            List<Thing> placesToTry = FindGoal(goal);
            //extend the list of connections to existing nodes until we reach our current position
            for (int i = 0; i < placesToTry.Count; i++)
            {
                List<Thing> newPlacesToTry = FindGoal(placesToTry[i]);
                if (newPlacesToTry.Contains(currentSituation))
                {
                    Thing target = placesToTry[i]; //target is the immediate target
                    pair = currentSituation.Children.Find(t => t.References[1].T == target);
                    return pair.References[0].T;
                }
                foreach (Thing t in newPlacesToTry)
                    if (!placesToTry.Contains(t)) placesToTry.Add(t);
            }
            return null;
        }

        public override void Initialize()
        {
            //scanning = false;
            orienting = false;
            auto = false;

            lastLandmark = null;
            currentLandmark = null;
            currentSituation = null;
            ClearNeurons();
            pointCount = 0;
            situationCount = 0;
            landmarkCount = 0;
            pairCount = 0;


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
