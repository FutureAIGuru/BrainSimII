//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Windows;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleNavigate : ModuleBase
    {
        //Auto Mode:  searching the maze
        //goingtogoal: returning to a known endpoint

        bool orienting = false;
        bool auto = false; //for debugging in manual mode

        //counters used to create unique labels 

        Thing lastLandmark = null;
        Thing currentLandmark = null;
        Thing currentEvent = null;
        Thing currentTargetReached = null;
        Thing currentTarget = null;
        Thing mostRecentAction = null;
        Thing mostRecentDecisionPoint = null;


        //we use this to create a random selection of behaviors
        Random rand = new Random((int)DateTime.Now.Ticks);

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //get the external references
            ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
            if (UKS == null) return;
            Module2DModel nmModel = (Module2DModel)FindModleu(typeof(Module2DModel));
            if (nmModel == null) return;
            ModuleBehavior nmBehavior = (ModuleBehavior)FindModleu(typeof(ModuleBehavior));
            if (nmBehavior == null) return;
            ModuleEvent mEvent = (ModuleEvent)FindModleu(typeof(ModuleEvent));
            if (mEvent == null) return;


            //check on the various input neurons...
            CheckNeurons(UKS);

            //don't do anything while a behavior is in progress
            if (GetNeuronValue("ModuleBehavior", "Done") == 0) return;

            //is there an existing landmark? Do I recognize I'm near a spot I was before?
            //this only calculates "R" so it is rotation-independent
            Thing best = null;
            float bestDist = 1000;

            List<Thing> near = nmModel.NearbySegments(3);

            FindBestLandmarkMatch(UKS, ref best, ref bestDist, near);

            nmModel.FireVisibleObjects(); //not really needed

            if (bestDist < .2f) //we are near a landmark we've been at before?
            {
                SetNeuronValue(null, "Found", 1);

                //yse, we have returned to a landmark we've been at before
                currentLandmark = best;
                currentEvent = currentLandmark.ReferencedBy[0].T;

                //we need to reorient ourselves to face the same way as we did before (set a flag)
                if (lastLandmark != currentLandmark)
                {
                    lastLandmark = currentLandmark;
                    orienting = true;
                }
            }
            else
            {
                //we're not near an existing landmark
                currentLandmark = null;
                currentEvent = null;
                lastLandmark = null;
            }

            //this is on arrival back at a landmark
            if (orienting)
            {
                Angle totalAngleError = GetTotalAngleError(near);
                if (Abs(totalAngleError) > PI / 2 - .1f)
                {
                    //turn in large increments until the angular error gets small
                    Angle angle = (float)PI / 2;
                    nmBehavior.TurnTo(angle);
                    return;
                }
                //this corrects for roundoff errors
                CorrectModelPosition(nmModel, nmBehavior, best, near);
                orienting = false;
                return;
            }

            //we are in going-to-goal mode
            if (currentTarget != null)//goingToGoal)
            {
                if (currentEvent != null) //currentEvent means we're at a decision point...check for the decision and execute it
                {
                    Thing action = GoToGoal(currentTarget);
                    if (action != null)
                    {
                        float angle = GetAngleFromAction(action);
                        if (angle != 0)
                        {
                            nmBehavior.TurnTo(angle);
                        }
                        nmBehavior.MoveTo(1);
                        currentEvent = null;
                        return;
                    }
                }
            }

            //we are in exploration mode
            //We're not at a known landmark
            //decide which way to turn at an obstacle
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
                //we have no choice but to follow the path
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
                Thing thingAhead = nmModel.GetNearestThing();
                currentTargetReached = thingAhead.References[2].T;
                if (mostRecentDecisionPoint != null)
                {
                    mEvent.AddOutcomePair(mostRecentDecisionPoint, mostRecentAction, currentTargetReached);
                    mostRecentAction = null;
                    mostRecentDecisionPoint = null;
                }

                Neuron n1 = null;
                if (currentTargetReached != null) n1 = mv.GetNeuronAt(currentTargetReached.Label);

                //color a new goal neuron
                if (n1 == null && currentTargetReached != null)
                {
                    n1 = AddLabel(currentTargetReached.Label + " ");
                    n1.Model = Neuron.modelType.Color;
                    n1.SetValueInt((int)currentTargetReached.V);
                    mv.GetNeuronLocation(n1, out int X, out int Y);
                    Neuron n2 = mv.GetNeuronAt(X + 1, Y);
                    if (n2 != null)
                        n2.Label = currentTargetReached.Label;
                }

                if (currentTargetReached == currentTarget)
                {
                    //if we're looking for a goal, we've reached it, so stop
                    //currentTarget = null;
                    currentEvent = null;
                }
                else
                {
                    //make a U-Turn and start backtracking
                    nmBehavior.TurnTo((float)-PI);
                }
            }
            else if (auto)
            {
                //we have a choice...
                //if the current landmark is null, create a new landmark & Event
                if (currentLandmark == null)
                {
                    //Create new Landmark...it clones the points so they are not modified by the model module
                    Thing newLandmark = mEvent.CreateLandmark(near);
                    currentEvent = mEvent.CreateEvent(newLandmark);
                    currentLandmark = newLandmark;
                    if (mostRecentDecisionPoint != null)
                        mEvent.AddOutcomePair(mostRecentDecisionPoint, mostRecentAction, currentEvent);
                }
                else
                {
                    if (mostRecentDecisionPoint != null && mostRecentDecisionPoint.Children.FindFirst(t1 => t1.References[0].T == mostRecentAction) == null)
                        mEvent.AddOutcomePair(mostRecentDecisionPoint, mostRecentAction, currentEvent);
                }

                //TODO improve the method of finding something not tried before
                //Decide which untried path to take
                //priorities...1)continue ahead 2)left 3)right or randomized (depending on comment below)
                Thing newAction = UKS.Labeled("NoAction");
                List<Thing> possibleActions = new List<Thing>();
                if (currentEvent.Children.FindFirst(t => t.References[0].T.Label == "GoS") == null && canGoAhead)
                    possibleActions.Add(UKS.Labeled("GoS"));
                if (currentEvent.Children.FindFirst(t => t.References[0].T.Label == "LTurnS") == null && canGoLeft)
                    possibleActions.Add(UKS.Labeled("LTurnS"));
                if (currentEvent.Children.FindFirst(t => t.References[0].T.Label == "RTurnS") == null && canGoRight)
                    possibleActions.Add(UKS.Labeled("RTurnS"));
                if (possibleActions.Count == 0 && currentEvent.Children.FindFirst(t => t.References[0].T.Label == "UTurnS") == null)
                    newAction = UKS.Labeled("UTurnS");
                else if (possibleActions.Count == 1)
                    newAction = possibleActions[0];
                else if (possibleActions.Count > 0)
                    //for debugging, eliminate the randomization by alternately commenting the 2 stmts below
                    //    newAction = possibleActions[0];
                    newAction = possibleActions[rand.Next(possibleActions.Count)];

                if (newAction.Label != "NoAction")
                {
                    mostRecentAction = newAction;
                    mostRecentDecisionPoint = currentEvent;

                    Angle angle = GetAngleFromAction(newAction);
                    nmBehavior.TurnTo(angle);
                    nmBehavior.MoveTo(1);
                }
                else
                {
                    //TODO: all actions at the current Event have been tried, is there another Event which hasn't been exhausted?
                }
                lastLandmark = currentLandmark;
                return;
            }
        }

        private void CheckNeurons(ModuleUKSN UKS)
        {
            //check for operating mode
            auto = (GetNeuronValue(null, "Auto") == 1) ? true : false;

            //check for a goal selection
            foreach (Neuron n in mv.Neurons)
            {
                if (n.Fired() && n.Label.IndexOf("c") == 0 && n.Model == Neuron.modelType.IF)
                {
                    Thing newTarget = UKS.Labeled(n.Label);
                    if (newTarget == currentTarget)
                    { }// currentTarget = null;
                    else
                        currentTarget = newTarget;
                    currentTargetReached = null;
                    break;
                }
            }
        }

        private void CorrectModelPosition(Module2DModel nmModel, ModuleBehavior nmBehavior, Thing best, List<Thing> near)
        {
            //locations are not very accurate and accumulate errors
            //whenever we encounter a landmark, we update the orientation/location of the model to correct errors
            Segment s2 = Module2DModel.SegmentFromUKSThing(near[0]);
            Segment s1 = null;
            foreach (Link l in best.References)
            {
                Segment s = Module2DModel.SegmentFromUKSThing(l.T);
                if (s.theColor == s2.theColor)
                { s1 = s; break; }
            }
            if (s1 != null)
            {
                PointPlus m1 = s1.MidPoint;
                PointPlus m2 = s2.MidPoint;
                float deltaX = m1.X - m2.X;
                float deltaY = m1.Y - m2.Y;
                PointPlus a1 = new PointPlus() { P = (Point)(s1.P1.V - s1.P2.V) };
                PointPlus a2 = new PointPlus() { P = (Point)(s2.P1.V - s2.P2.V) };
                Angle deltaTheta = a1.Theta - a2.Theta;
                nmModel.Move(-deltaX, -deltaY);
                nmBehavior.TurnTo(deltaTheta);
            }
        }

        private Angle GetTotalAngleError(List<Thing> near)
        {
            if (lastLandmark == null) return 10000;
            Angle totalAngleError = 0;
            for (int i = 0; i < lastLandmark.References.Count; i++)
            {
                Segment s1 = Module2DModel.SegmentFromUKSThing(lastLandmark.References[i].T);
                foreach (Thing t1 in near)
                {
                    //does it match one of the segments presently near me?
                    Segment s2 = Module2DModel.SegmentFromUKSThing(t1);
                    if (s1.theColor == s2.theColor)
                    {
                        Angle m1 = s1.MidPoint.Theta;
                        Angle m2 = s2.MidPoint.Theta;
                        Angle angle = m1 - m2;
                        totalAngleError += Abs(angle);
                    }
                }
            }
            return totalAngleError;
        }

        private static void FindBestLandmarkMatch(ModuleUKSN UKS, ref Thing best, ref float bestDist, List<Thing> near)
        {
            //searching each spatial Event 
            best = null;
            bestDist = 1000;
            if (UKS == null) return;
            if (UKS.Labeled("Landmark") == null) return;

            //landmark segments are sorted, closest segment first
            //closer segments are more important
            IList<Thing> landmarks = UKS.Labeled("Landmark").Children;
            foreach (Thing landmark in landmarks)
            {
                float totalDist = 0;
                //each reference in that landmark
                int foundCount = 0;  //must match several items to count as a hit
                int linkNumber = 0;
                foreach (Link L1 in landmark.References)
                {
                    Segment s1 = Module2DModel.SegmentFromUKSThing(L1.T);
                    float d1 = (float)Utils.FindDistanceToSegment(s1);
                    int nearNumber = 0;
                    foreach (Thing t1 in near)
                    {
                        //does it match one of the segments presently near me?
                        Segment s2 = Module2DModel.SegmentFromUKSThing(t1);
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

        private Angle GetAngleFromAction(Thing action)
        {
            switch (action.Label)
            {
                case "GoS": return 0;
                case "LTurnS": return -PI / 2;
                case "RTurnS": return PI / 2;
                case "UTurnS": return PI;
            }
            return 0;
        }

        //this returns a list of things directly connected to the given target 
        private List<Thing> FindGoal(Thing target)
        {
            if (target == null) return null;
            ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));

            return UKS.Labeled("Event").ChildrenWriteable.FindAll(
                t => t.Children.FindFirst(u => u.References[1].T == target) != null);
        }

        //This returns the action needed at the currentEvent in order to move toward the goal
        public Thing GoToGoal(Thing goal)
        {
            if (goal == null) return null;
            ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));

            //trivial case, you can go straight to goal
            Thing pair = currentEvent.Children.FindFirst(t => t.References[1].T == goal);
            if (pair != null)
                return pair.References[0].T;

            IList<Thing> placesToTry = FindGoal(goal);
            //extend the list of connections to existing nodes until we reach our current position
            for (int i = 0; i < placesToTry.Count; i++)
            {
                IList<Thing> newPlacesToTry = FindGoal(placesToTry[i]);
                if (newPlacesToTry.Contains(currentEvent))
                {
                    Thing target = placesToTry[i]; //target is the intermediate target
                    pair = currentEvent.Children.FindFirst(t => t.References[1].T == target);
                    return pair.References[0].T; //return the action of the outcome pair
                }
                foreach (Thing t in newPlacesToTry)
                    if (!placesToTry.Contains(t)) 
                        placesToTry.Add(t);
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
            currentEvent = null;
            ClearNeurons();

            //randomize
            rand = new Random((int)DateTime.Now.Ticks);

            AddLabel("Auto");
            AddLabel("Found");

            //this is the first test of building a sequence of behaviors
            ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
            if (UKS != null)
            {
                Thing t = UKS.AddThing("LTurnS", "Action");
                t = UKS.AddThing("RTurnS", "Action");
                t = UKS.AddThing("UTurnS", "Action");
                t = UKS.AddThing("GoS", "Action");
            }
        }
    }
}
