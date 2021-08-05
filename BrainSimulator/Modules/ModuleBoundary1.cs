//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class Boundary
    {
        public Point p1;
        public Point p2;
        public string theString;
        //        public bool processed;
        //        public Angle angle;
        //        public List<Point> corners = new List<Point>();
    }
    public class ModuleBoundary1 : ModuleBase
    {
        public class BoundaryNeuron
        {
            public bool fired = false;
            public int neighborFiredCount = 0;
            public bool processed = false;
            public bool isCorner = false;
            public byte clearNeighbors = 0; //bitfield of neighbor neurons
        }

        [XmlIgnore]
        public List<Boundary> boundaries = new List<Boundary>();
        [XmlIgnore]
        public List<Boundary> boundarySegments = new List<Boundary>();
        int lastInflection = 0; //used in predicting next point
        int mustExitDir = -10; //used in special case of corner points with 7 neighbors


        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView naSource = theNeuronArray.FindModuleByLabel("ImageZoom");
            if (naSource == null) return;
            boundaries.Clear();
            foreach (Neuron n in na.Neurons1)
                n.SetValue(0);

            if (naSource != null)
            {
                int theHeight = naSource.Height;
                int theWidth = naSource.Width;
                BoundaryNeuron[,] sourceValue = new BoundaryNeuron[theWidth, theHeight];
                //copy to local 2D array for speed and simplicity
                for (int y = 1; y < theHeight; y++)
                {
                    for (int x = 0; x < theWidth; x++)
                    {
                        Neuron n1 = naSource.GetNeuronAt(x, y);
                        sourceValue[x, y] = new BoundaryNeuron() { fired = n1.LastChargeInt == 0 };
                    }
                }
                //count the number of firing neighbors at each cell
                for (int y = 1; y < theHeight; y++)
                {
                    for (int x = 0; x < theWidth; x++)
                    {
                        if (sourceValue[x, y].fired)
                        {
                            sourceValue[x, y].neighborFiredCount = GetNeighborFiredCount(sourceValue, x, y, out byte clearNeighbors);
                            sourceValue[x, y].clearNeighbors = clearNeighbors;
                        }
                    }
                }

                //go to any unprocessed boundary point and follow along the boundary
                string boundaryString = "";
                lastInflection = 0;
                for (int y1 = 1; y1 < theHeight; y1++)
                {
                    for (int x1 = 0; x1 < theWidth; x1++)
                    {
                        boundaryString = "";
                        BoundaryNeuron bn = sourceValue[x1, y1];
                        if (bn.neighborFiredCount > 1 &&
                            bn.neighborFiredCount < 7 &&
                            !bn.processed ||
                            (bn.neighborFiredCount == 7 && !bn.processed && bn.clearNeighbors == 0x040)
                            )
                        {
                            int x0 = x1; int y0 = y1;
                            sourceValue[x0, y0].processed = true;
                            List<Point> corners = new List<Point>();
                            corners.Add(new Point(x0, y0));
                            while (GetNextBoundaryPoint(sourceValue, ref x0, ref y0, ref boundaryString, corners))
                            {
                            }
                            if (boundaryString != "")
                            {
                                Boundary theBoundary = new Boundary
                                {
                                    p1 = new Point(x1, y1),
                                    p2 = new Point(x0, y0),
                                    theString = boundaryString,
                                    //corners = corners,
                                };
                                boundaries.Add(theBoundary);
                            }
                            lastInflection = 0;
                        }
                        sourceValue[x1, y1].processed = true;
                    }
                }

                //close boundary loops
                //these will always be unclosed because the first point is marked used and cannot be reused
                for (int i = 0; i < boundaries.Count; i++)
                {
                    int dx = (int)(boundaries[i].p1.X - boundaries[i].p2.X);
                    int dy = (int)(boundaries[i].p1.Y - boundaries[i].p2.Y);
                    if (Math.Abs(dx) < 2 && Math.Abs(dy) < 2)
                    {
                        char newChar = GetMotionChar(dx, dy);
                        boundaries[i].theString += newChar;
                        boundaries[i].p2.X += dx;
                        boundaries[i].p2.Y += dy;
                    }
                }

                //SquareTheCorners();
                boundarySegments.Clear();
                for (int j = 0; j < boundaries.Count; j++)
                {
                    String[] lines = LongestStraightLine(boundaries[j].theString).ToArray();
                    Array.Sort(lines, (x, y) => y.Length.CompareTo(x.Length));
                    string s = boundaries[j].theString;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        int x0 = (int)boundaries[j].p1.X;
                        int y0 = (int)boundaries[j].p1.Y;
                        int x1 = x0;
                        int y1 = y0;
                        int index = s.IndexOf(lines[i], StringComparison.Ordinal);
                        while (index != -1)
                        {
                            if (index != 0)
                                GetPositionOfLinePoint(x0, y0, boundaries[j].theString, index + 1, out x1, out y1);
                            GetPositionOfLinePoint(x0, y0, boundaries[j].theString, index + lines[i].Length + 1, out int x2, out int y2);
                            boundarySegments.Add(new Boundary { p1 = new Point(x1, y1), p2 = new Point(x2, y2), theString = lines[i] });
                            int end = index + lines[i].Length + 2;
                            if (end >= s.Length) end = s.Length;
                            s = s.Substring(0, index) + '*' + new string('x', lines[i].Length) + '*' + s.Substring(end);
                            index = s.IndexOf(lines[i], StringComparison.Ordinal);
                        }
                    }
                }
                for (int i = 0; i < boundaries.Count; i++)
                {
                    Boundary b = boundaries[i];
                    SetTheNeurons((int)b.p1.X, (int)b.p1.Y, b.theString);
                }
            }
        }

        List<String> LongestStraightLine(string s)
        {
            int bestStart = -1;
            int bestLength = -1;
            string bestString = "";
            for (int i = 0; i < s.Length; i++)
            {
                for (int j = 2; j + i <= s.Length; j++)
                {
                    string s1 = s.Substring(i, j);
                    if (IsStraightLine(s1))
                    {
                        if (j > bestLength)
                        {
                            bestLength = j;
                            bestStart = i;
                            bestString = s1;
                        }
                    }
                }
            }
            List<String> retVal = new List<String>();
            if (bestStart < 0) return retVal;
            //take off first and last characters
            string sToAdd = s.Substring(bestStart + 1, bestLength - 2);
            if (sToAdd != "")
                retVal.Add(sToAdd);
            string leftRemainder = s.Substring(0, bestStart);
            string rightRemainder = s.Substring(bestStart + bestLength);
            if (leftRemainder.Length > 2)
                retVal.AddRange(LongestStraightLine(leftRemainder));
            if (rightRemainder.Length > 2)
                retVal.AddRange(LongestStraightLine(rightRemainder));

            return retVal;
        }
        private void SetTheNeurons(int x0, int y0, string s)
        {
            SetTheNeuron(x0, y0);
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case U: y0--; break;
                    case D: y0++; break;
                    case R: x0++; break;
                    case L: x0--; break;
                    case UR: x0++; y0--; break;
                    case UL: x0--; y0--; break;
                    case DR: x0++; y0++; break;
                    case DL: x0--; y0++; break;
                }
                SetTheNeuron(x0, y0);
            }
        }
        private void SetTheNeuron(int x0, int y0)
        {
            ModuleView naSource = theNeuronArray.FindModuleByLabel("ImageZoom");
            //Set neuron
            int theHeight = naSource.Height;
            int theWidth = naSource.Width;
            int x = (int)(x0 * na.Width / (float)theWidth);
            int y = (int)(y0 * na.Height / (float)theHeight);
            Neuron n = na.GetNeuronAt(x, y); 
            if (n != null) 
                n.SetValue(1);
        }

        public static char[] dirChars = new char[] { R, UR, U, UL, L, DL, D, DR };
        //eight possible direction characterss
        const char R = '\u2b95';
        const char UR = '\u2197';
        const char U = '\u2b06';
        const char UL = '\u2196';
        const char L = '\u2b05';
        const char DL = '\u2199';
        const char D = '\u2b07';
        const char DR = '\u2198';

        void GetDeltasFromDirection(int dir, out int dx, out int dy)
        {
            while (dir < 0) dir += 8;
            while (dir > 7) dir -= 8;
            switch (dir)
            {
                case 0: dx = 1; dy = 0; break;
                case 1: dx = 1; dy = -1; break;
                case 2: dx = 0; dy = -1; break;
                case 3: dx = -1; dy = -1; break;
                case 4: dx = -1; dy = 0; break;
                case 5: dx = -1; dy = 1; break;
                case 6: dx = 0; dy = 1; break;
                case 7: dx = 1; dy = 1; break;
                default: dx = 0; dy = 0; break;
            }
        }
        int GetDirectionFromDeltas(int dx, int dy)
        {
            if (dx == 1 && dy == 0) return 0;
            if (dx == 1 && dy == -1) return 1;
            if (dx == 0 && dy == -1) return 2;
            if (dx == -1 && dy == -1) return 3;
            if (dx == -1 && dy == 0) return 4;
            if (dx == -1 && dy == 1) return 5;
            if (dx == 0 && dy == 1) return 6;
            if (dx == 1 && dy == 1) return 7;
            return -1;
        }

        public static int GetDirFromChar(char c)
        {
            for (int i = 0; i < 8; i++) if (c == dirChars[i]) return i;
            return -1;
        }

        char GetCharFromDir(int dir)
        {
            if (dir < 0) dir += 8;
            if (dir > 7) dir -= 8;
            return dirChars[dir];
        }

        char GetMotionChar(int dx, int dy)
        {
            return (dirChars[GetDirectionFromDeltas(dx, dy)]);
        }
        Angle GetLineStringSlope(string s)
        {
            GetPositionOfLinePoint(0, 0, s, s.Length, out int x1, out int y1);
            Angle theAngle = Math.Atan2(y1, x1);
            return theAngle;
        }

        void IsCurve(string s)
        {
            //TODO: implement..allow small gradual unidirectional changes in slope
        }

        //this looks at a string of moves along a boundary and determines if it represents a straight line
        public static bool IsStraightLine(string s)
        {
            if (s.Length >= 2)
            {
                //a line consists of 1 or 2 directions which must differe by only 1
                if (s.Distinct().Count() > 2) return false;
                char[] dd = s.Distinct().ToArray();
                if (dd.Length < 2) return true; //all the same character is always straight.

                //2 directions which must differe by only 1
                int dir1 = GetDirFromChar(dd[0]);
                int dir2 = GetDirFromChar(dd[1]);
                if (dir1 != dir2 && Math.Abs(dir1 - dir2) != 1 && Math.Abs(dir1 - dir2) != 7) return false;

                //count groups of alternating characters to see which is larger
                int[] groups = myGroupBy(s);
                //this is complicated by the zeroth group which may be a partial
                //major groups > 1 in length...other must be == or + error
                int dominantGroupOdd = 0;
                int dominantGroupSize = 1;

                for (int i = 0; i < groups.Length; i++)
                {
                    if (groups[i] != 1)
                    {
                        dominantGroupOdd = i % 2;
                        if (groups[i] > dominantGroupSize)
                            dominantGroupSize = groups[i];
                        if (i > 2) break;
                    }
                }
                //all odd numbered groups must be length of 1
                for (int i = (dominantGroupOdd + 1) % 2; i < groups.Length; i += 2)
                {
                    if (groups[i] != 1) return false;
                }
                int allowedError = 0;
                if (dominantGroupOdd == 0 && groups[0] > dominantGroupSize) return false;
                for (int i = dominantGroupOdd; i < groups.Length - 1; i += 2)
                {
                    if (groups[i] != dominantGroupSize)
                    {
                        if (allowedError == 0) //not set yet
                        {
                            if (Math.Abs(groups[i] - dominantGroupSize) > 1) return false;
                            allowedError = groups[i] - dominantGroupSize;
                        }
                        if (groups[i] != dominantGroupSize + allowedError) return false;
                    }
                }
            }
            return true;
        }
        int[] AllowablePointsForLine(string s)
        {
            List<int> dirs = new List<int>();
            if (s.Length == 0) for (int i = 0; i < 8; i++) dirs.Add(i);
            else
            {
                if (s.Length == 1 || s.Distinct().Count() == 1) //or all chars are same
                {
                    int dir = GetDirFromChar(s[0]);
                    dirs.Add(dir);
                    dirs.Add(dir - 1);
                    dirs.Add(dir + 1);
                }
                else if (s.Distinct().Count() == 2)
                {
                    char[] dd = s.Distinct().ToArray();
                    //hack
                    for (int i = 0; i < dd.Length; i++)
                    {
                        if (IsStraightLine(s + dd[i]))
                            dirs.Add(GetDirFromChar(dd[i]));
                    }
                }
            }
            return dirs.ToArray();
        }
        int[] AllowableDirsOrdered(string motionString)
        {
            int[] dirs = new int[8];
            if (motionString == "")
            {
                for (int i = 0; i < 8; i++) dirs[i] = i;
            }
            else
            {
                int dir = GetDirFromChar(motionString.Last());
                for (int i = 0; i < 8; i++)
                {
                    if (i % 2 != 0)
                        dirs[i] = dir + (i + 1) / 2;
                    else
                        dirs[i] = dir - i / 2;
                }
            }
            return dirs;
        }

        //this is like GroupBy and returns an array of the counts of consecutive characters in a string
        public static int[] myGroupBy(string s)
        {
            List<int> retVal = new List<int>();
            int i = 0;
            while (i < s.Length)
            {
                int count = 1;
                while (i < s.Length - 1 && s[i] == s[i + 1]) { count++; i++; };
                i++;
                retVal.Add(count);
            }
            return retVal.ToArray();
        }

        bool GetNextBoundaryPoint(BoundaryNeuron[,] sourceValue, ref int x, ref int y, ref string motionString, List<Point> corners)
        {
            if (mustExitDir != -10) //special case of an interior corner
            {
                GetDeltasFromDirection(mustExitDir, out int dx1, out int dy1);
                mustExitDir = -10;
                if (IsPointNextBoundaryPoint(sourceValue, ref x, ref y, dx1, dy1, ref motionString)) return true;
                return false;
            }
            int[] dirs = AllowablePointsForLine(motionString.Substring(lastInflection));
            for (int i = 0; i < dirs.Length; i++)
            {
                GetDeltasFromDirection(dirs[i], out int dx1, out int dy1);
                if (WithinBounds(sourceValue, x + dx1, y + dy1) &&
                    HasCommonEdge(x, y, dx1, dy1, sourceValue[x, y].clearNeighbors, sourceValue[x + dx1, y + dy1].clearNeighbors))
                {
                    if (IsPointNextBoundaryPoint(sourceValue, ref x, ref y, dx1, dy1, ref motionString)) return true;
                }
            }

            //no allowable point on a line...must be a corner
            corners.Add(new Point(x, y));
            lastInflection = motionString.Length - 1;

            dirs = AllowableDirsOrdered(motionString);
            for (int i = 0; i < dirs.Length; i++) //there is a corner...check all directions
            {
                GetDeltasFromDirection(dirs[i], out int dx1, out int dy1);
                if (WithinBounds(sourceValue, x + dx1, y + dy1) &&
                    HasCommonEdge(x, y, dx1, dy1, sourceValue[x, y].clearNeighbors, sourceValue[x + dx1, y + dy1].clearNeighbors))
                    if (IsPointNextBoundaryPoint(sourceValue, ref x, ref y, dx1, dy1, ref motionString))
                    {
                        lastInflection++;
                        return true;
                    }
            }
            for (int i = 0; i < dirs.Length; i++)
            {
                GetDeltasFromDirection(dirs[i], out int dx1, out int dy1);
                if (WithinBounds(sourceValue, x + dx1, y + dy1) &&
                    HasCommonEdge(x, y, dx1, dy1, sourceValue[x, y].clearNeighbors, sourceValue[x + dx1, y + dy1].clearNeighbors))
                    if (IsPointNextBoundaryPoint(sourceValue, ref x, ref y, dx1, dy1, ref motionString, true))
                    {
                        corners.Add(new Point(x, y));
                        lastInflection = motionString.Length;
                        return true;
                    }
            }


            for (int i = 0; i < dirs.Length; i++) //there is a corner...check all directions
            {
                GetDeltasFromDirection(dirs[i], out int dx1, out int dy1);
                if (WithinBounds(sourceValue, x + dx1, y + dy1) &&
                    HasCommonEdge(x, y, dx1, dy1, sourceValue[x, y].clearNeighbors, sourceValue[x + dx1, y + dy1].clearNeighbors))
                    if (IsPointNextBoundaryPoint(sourceValue, ref x, ref y, dx1, dy1, ref motionString, true))
                    {
                        lastInflection++;
                        return true;
                    }
            }
            //TODO, handle case of more than one possible exit from a cell (this may be working)
            return false;
        }

        //for two pixels to be part of the same boundary, 
        //they must be adjacent to each other AND share a common non-color neighbor
        bool HasCommonEdge(int x, int y, int dx, int dy, byte clearBits1, byte clearBits2)
        {
            for (int i = 0; i < 8; i++)
            {
                if ((clearBits1 & (1 << i)) != 0)
                {
                    GetDeltasFromDirection(i, out int dx1, out int dy1);
                    Point p1 = new Point(x + dx1, y + dy1);
                    for (int j = 0; j < 8; j++)
                    {
                        if ((clearBits2 & (1 << j)) != 0)
                        {
                            GetDeltasFromDirection(j, out int dx2, out int dy2);
                            Point p2 = new Point(x + dx + dx2, y + dy + dy2);
                            if (p2.Equals(p1)) return true;
                        }
                    }
                }
            }
            return false;
        }


        bool IsPointNextBoundaryPoint(BoundaryNeuron[,] sourceValue, ref int x, ref int y, int dx, int dy, ref string motionString, bool specialCase = false)
        {
            int x0 = x + dx; int y0 = y + dy;
            if (WithinBounds(sourceValue, x0, y0) &&
                !sourceValue[x0, y0].processed &&
                sourceValue[x0, y0].neighborFiredCount > 1)
            {
                if (sourceValue[x0, y0].neighborFiredCount <= 6)
                {
                    motionString += GetMotionChar(dx, dy);
                    sourceValue[x0, y0].processed = true;
                    x = x0; y = y0;
                    mustExitDir = -10;
                    return true;
                }
                else if (specialCase)
                {
                    //special case with 7 neighbors where entry direction must be checked
                    //the non-firing cell must be adjascent to the entry 
                    int dir = GetDirectionFromDeltas(dx, dy);
                    dir += 4; //get the inverse
                    if (dir > 7) dir -= 8;
                    dir--;
                    GetDeltasFromDirection(dir, out int dx1, out int dy1);
                    if (!sourceValue[x0 + dx1, y0 + dy1].fired)
                    {
                        int mustExitDir0 = dir - 1;
                        GetDeltasFromDirection(mustExitDir0, out int dx2, out int dy2);
                        if (!sourceValue[x0 + dx2, y0 + dy2].processed &&
                            sourceValue[x0 + dx2, y0 + dy2].neighborFiredCount < 7)
                        {
                            motionString += GetMotionChar(dx, dy);
                            sourceValue[x0, y0].processed = true;
                            x = x0; y = y0;
                            mustExitDir = mustExitDir0;
                            return true;
                        }
                    }

                    dir += 2;
                    GetDeltasFromDirection(dir, out dx1, out dy1);
                    if (!sourceValue[x0 + dx1, y0 + dy1].fired)
                    {
                        int mustExitDir0 = dir + 1;
                        GetDeltasFromDirection(mustExitDir0, out int dx2, out int dy2);
                        if (!sourceValue[x0 + dx2, y0 + dy2].processed &&
                            sourceValue[x0 + dx2, y0 + dy2].neighborFiredCount < 7)
                        {
                            motionString += GetMotionChar(dx, dy);
                            sourceValue[x0, y0].processed = true;
                            x = x0; y = y0;
                            mustExitDir = mustExitDir0;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        int GetNeighborFiredCount(BoundaryNeuron[,] sourceValue, int x, int y, out byte clearNeighbors)
        {
            int retVal = 0;
            clearNeighbors = 0;
            for (int i = 0; i < 8; i++)
            {
                GetDeltasFromDirection(i, out int dx, out int dy);
                if (WithinBounds(sourceValue, x + dx, y + dy) && sourceValue[x + dx, y + dy].fired)
                {
                    retVal++;
                }
                else
                {
                    clearNeighbors |= (byte)(1 << i);
                }
            }
            return retVal;
        }

        bool WithinBounds(BoundaryNeuron[,] array, int x, int y)
        {
            if (x >= 0 && y >= 1 && x < array.GetLength(0) && y < array.GetLength(1)) return true;
            return false;
        }

        public static void GetPositionOfLinePoint(int x0, int y0, string motion, int pos, out int x1, out int y1)
        {
            x1 = x0;
            y1 = y0;
            for (int i = 0; i < pos && i < motion.Length; i++)
            {
                if (motion[i] == U) y1--;
                if (motion[i] == D) y1++;
                if (motion[i] == R) x1++;
                if (motion[i] == L) x1--;
                if (motion[i] == UR) { x1++; y1--; };
                if (motion[i] == UL) { x1--; y1--; };
                if (motion[i] == DR) { x1++; y1++; };
                if (motion[i] == DL) { x1--; y1++; };
            }
        }


        private void SquareTheCorners()
        {
            for (int i = 0; i < boundaries.Count; i++)
            {
                Boundary boundary = boundaries[i];
                string s = boundary.theString;
                s = SharpenCorner(R, UR, U, s);
                s = SharpenCorner(R, DR, D, s);
                s = SharpenCorner(L, UL, U, s);
                s = SharpenCorner(L, DL, D, s);
                s = SharpenCorner(U, UR, R, s);
                s = SharpenCorner(U, UL, L, s);
                s = SharpenCorner(D, DR, R, s);
                s = SharpenCorner(D, DL, L, s);
                boundaries[i].theString = s;
            }
        }

        private static string SharpenCorner(char c1, char c2, char c3, string s)
        {
            string target = c1.ToString() + c2 + c3;
            string replacement = c1.ToString() + c1 + c3 + c3;
            s = s.Replace(target, replacement);
            return s;
        }


        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }
    }
}
