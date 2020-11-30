//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class BoundaryX
    {
        public Point p1;
        public Point p2;
        public string debugString;
        public string motionString;
        public Angle angle;
        public bool processed;
    }

    public class ModuleLineFinder : ModuleBase
    {
        [XmlIgnore]
        public List<BoundaryX> boundaries;
        public enum Dir { X, L, R, U, D, XU, XD };
        [XmlIgnore]
        public List<BoundaryX> lines;

        List<Point> corners;

        Dir GetCellDir(int x, int y)
        {
            x *= 6;
            ModuleView naSource = theNeuronArray.FindAreaByLabel("ModuleBoundary");
            if (x >= naSource.Width || y >= naSource.Height || y < 0 || x < 0) return Dir.X;
            if (CellAlreadyUsed(x / 6, y)) return Dir.X;
            if (naSource.GetNeuronAt(x + 4, y).Fired()) return Dir.XU;
            if (naSource.GetNeuronAt(x + 5, y).Fired()) return Dir.XD;
            if (naSource.GetNeuronAt(x, y).Fired()) return Dir.L;
            if (naSource.GetNeuronAt(x + 1, y).Fired()) return Dir.R;
            if (naSource.GetNeuronAt(x + 2, y).Fired()) return Dir.U;
            if (naSource.GetNeuronAt(x + 3, y).Fired()) return Dir.D;
            return Dir.X;
        }

        List<int> handledCells;
        bool CellAlreadyUsed(int x, int y)
        {
            int val = (x << 16) + y;
            return handledCells.Contains(val);
        }
        void MarkCellUsed(int x, int y)
        {
            handledCells.Add((x << 16) + y);
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here
            boundaries = new List<BoundaryX>();
            ModuleView naSource = theNeuronArray.FindAreaByLabel("ModuleBoundary");

            //this keeps a list of neurons already visited
            handledCells = new List<int>();

            if (naSource != null)
            {
                FindBoundaries(naSource);
                SharpenRUEdges();
                FindCorners();
                AdjustLines();
            }
        }


        private void SharpenRUEdges()
        {
            //this handles cases of horizontal and vertical lines which are recorde 1-away from true positionj because the 
            //location point is always in the upper left of a 2x2 cell but the boundary may be at the top, bottom, left, or right.
            for (int i = 0; i < boundaries.Count; i++)
            {
                BoundaryX boundary = boundaries[i];
                //string target = DR.ToString() + R.ToString();

                string s = boundary.motionString;

                s = SharpenCorner(DR, R, UR, s);
                s = SharpenCorner(UR, U, UL, s);
                s = SharpenCorner(UL, L, DL, s);
                boundaries[i].motionString = s;
            }
        }

        private static string SharpenCorner(char c1, char c2, char c3, string s)
        {
            string target = c1.ToString() + c2.ToString();
            int j = s.IndexOf(target, StringComparison.Ordinal);
            while (j != -1 && j < s.Length-1)
            {
                int k = j + 1;
                char c = s[k];
                while (k < s.Length && s[k] == c2) k++;
                if (k < s.Length && s[k] == c3)
                {
                    s = s.Substring(0, j) + c2 + s.Substring(j + 1, k - j - 1) + c2 + s.Substring(k);
                }
                j = s.IndexOf(target, j+1, StringComparison.Ordinal);
            }

            return s;
        }

        private void FindCorners()
        {
            //TODO add check for boundary ending where it starts
            lines = new List<BoundaryX>();
            for (int i = 0; i < boundaries.Count; i++)
            {
                BoundaryX boundary = boundaries[i];
                int curX = (int)Math.Round(boundary.p1.X);
                int curY = (int)Math.Round(boundary.p1.Y);
                int newX = curX;
                int newY = curY;
                corners = new List<Point>();
                corners.Add(new Point(curX, curY));
                BoundaryX theLine;
                string s = boundary.motionString;
                for (int j = 0; j < s.Length - 2; j++)
                {
                    for (int offset = 1; offset < 4 && j + offset < s.Length; offset++)
                    {
                        if ((IsHoriz(s[j]) && IsVert(s[j + offset])) ||
                            (IsVert(s[j]) && IsHoriz(s[j + offset])) ||
                            (IsAngle(s[j]) && IsAngle(s[j + offset]) && s[j] != s[j + offset])
                            )
                        {
                            string sString = s.Substring(0, j + offset);
                            GetPrincipalSlope(sString);
                            //char last = sString[sString.Length - 1];
                            //char nextToLast = sString[sString.Length - 2];
                            //if (IsAngle(last) && (IsHoriz(nextToLast) || IsVert(nextToLast)))
                            // {
                            //    sString = sString.Substring(0, sString.Length - 1) + nextToLast;
                            // }
                            GetPositionOfLinePoint(curX, curY, s, j + 1, out newX, out newY);
                            //s = s.Substring(j + offset);
                            //j = 0;
                            corners.Add(new Point(newX, newY));
                            j += 1;// offset;
                            /*                            theLine = new Boundary
                                                        {
                                                            p1 = new Point(curX, curY),
                                                            p2 = new Point(newX, newY),
                                                            debugString = sString,
                                                            processed = false,
                                                        };
                                                        lines.Add(theLine);
                                                        j = 0;
                                                        curX = newX;
                                                        curY = newY;
                            */
                            break;
                        }
                    }
                }
                //GetPositionOfLinePoint(curX, curY, s, s.Length - 1, out newX, out newY);
                //theLine = new Boundary
                //{
                //    p1 = new Point(curX, curY),
                //    p2 = new Point(newX, newY),
                //    debugString = s,
                //    processed = false,
                //};
                //lines.Add(theLine);
            }
        }

        void AdjustLines()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string s = lines[i].debugString;
                if (s.Length > 4)
                {
                    Angle lastM = 23;
                    for (int j = 0; j < 3; j++)
                    {
                        string s1 = s.Substring(j, s.Length - j * 2);
                        Angle m = GetLineStringSlope(s1);
                        if (Math.Abs(m - lastM) < Utils.Rad(2))
                        {
                            //if (s[0] == D || s[0] == DR) lines[i].p1.Y += 1;
                            //if (s[0] == R || s[0] == DR) lines[i].p1.X += 1;
                            j--;
                            s1 = s.Substring(j, s.Length - j * 2);
                            string newString = s1.Substring(0, j) + s1 + s1.Substring(s1.Length - j);
                            GetPositionOfLinePoint((int)lines[i].p1.X, (int)lines[i].p1.Y, newString, newString.Length, out int newX, out int newY);
                            lines[i].debugString = newString;
                            lines[i].p2 = new Point(newX, newY);
                            break;
                        }
                        lastM = m;
                    }
                    Angle m1 = GetLineStringSlope(lines[i].debugString);
                    if (m1 > Utils.Rad(179)) m1 = 0;
                    if (m1 < 0) m1 += Utils.Rad(180);
                    lines[i].angle = m1;

                }
                else
                {
                    lines.RemoveAt(i);
                    i--;
                }
            }
        }

        protected class charCount
        {
            public char c;
            public int count;
        };
        Angle GetPrincipalSlope(string s)
        {
            List<charCount> counts = new List<charCount>();
            for (int i = 0; i < s.Length; i++)
            {
                int index = counts.FindIndex(x => x.c == s[i]);
                if (index == -1)
                {
                    counts.Add(new charCount { c = s[i], count = 0, });
                    index = counts.Count - 1;
                }
                counts[index].count++;
            }
            counts.OrderBy(x => x.count);
            //extraneous chars must be at the beginning or end of the string
            string s1 = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == counts[0].c || s[i] == counts[1].c)
                {
                    if (s1.Length == 0 || s1[s1.Length - 1] != s[i] || s[i] == counts[0].c)
                        s1 += s[i];
                }
            }
            return GetLineStringSlope(s1);
        }

        Angle GetLineStringSlope(string s)
        {
            GetPositionOfLinePoint(0, 0, s, s.Length, out int x1, out int y1);
            Angle theAngle = Math.Atan2(y1, x1);
            return theAngle;
        }
        bool IsHoriz(char c)
        {
            return c == L || c == R;
        }
        bool IsVert(char c)
        {
            return c == U || c == D;
        }
        bool IsAngle(char c)
        {
            return c == DL || c == DR || c == UL || c == UR;
        }
        private void GetPositionOfLinePoint(int x0, int y0, string motion, int pos, out int x1, out int y1)
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
        private void FindBoundaries(ModuleView naSource)
        {
            //TODO implement case of angle > 270
            //TODO handle case where an angle is shared by two boundary edges
            for (int x = 0; x < naSource.Width / 6; x++)
                for (int y = 0; y < naSource.Height; y++)
                {
                    bool done = CellAlreadyUsed(x, y);
                    int y1 = y;
                    int x1 = x;


                    /* possible patterns
                     *         D D D
                     * D D D XU                   R       L          R        L
                     *                            R       L          R        L
                     * D D D                      XU    XU             XD     XD
                     *       XDD D D            R       L              R        L
                     *                          R       L              R        L
                     *       XUU U U
                     * U U U                                              D D        U U
                     *                              XU   XD             R     L    L     R
                     * U U U XD                   XU       XD           R     L    L     R
                     *         U U U            XU           XD           U U        D D
                     *       
                     * The number of consequtive LRDorU must be nearly constant (vary by 1) although the first group can be smaller (by any amount)
                     * This defines legal sequences...D can only be followed by D or LR (or LL one step down)
                     */

                    string debugString = "";
                    motionString = "";
                    while (!done && x1 < naSource.Width && y1 < naSource.Height && y1 >= 0)
                    {
                        //follow along the line of pixels
                        switch (GetCellDir(x1, y1))
                        {
                            case Dir.R:
                                debugString += "R";
                                MarkCellUsed(x1, y1);
                                if (CheckCell(ref x1, ref y1, 0, 1, Dir.R)) { }
                                else if (CheckCell(ref x1, ref y1, 0, -1, Dir.R)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, 0, 1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, 1, -1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, 0, -1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 1, Dir.U, Dir.R)) { }
                                else if (CheckCell(ref x1, ref y1, 1, -1, Dir.D, Dir.R)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 1, Dir.D, Dir.R)) { }
                                else if (CheckCell(ref x1, ref y1, -1, -1, Dir.U, Dir.R)) { }
                                else goto case Dir.X;
                                break;

                            case Dir.L:
                                debugString += "L";
                                MarkCellUsed(x1, y1);
                                if (CheckCell(ref x1, ref y1, 0, 1, Dir.L)) { }
                                else if (CheckCell(ref x1, ref y1, 0, -1, Dir.L)) { }
                                else if (CheckCell(ref x1, ref y1, 0, 1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, 0, -1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, -1, -1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 1, Dir.U, Dir.L)) { }
                                else if (CheckCell(ref x1, ref y1, -1, -1, Dir.D, Dir.L)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 1, Dir.D, Dir.L)) { }
                                else if (CheckCell(ref x1, ref y1, 1, -1, Dir.U, Dir.L)) { }
                                else goto case Dir.X;
                                break;

                            case Dir.D:
                                debugString += "D";
                                MarkCellUsed(x1, y1);
                                if (CheckCell(ref x1, ref y1, 1, 0, Dir.D)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 0, Dir.D)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 0, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 0, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 1, Dir.R, Dir.D)) { }
                                else if (CheckCell(ref x1, ref y1, -1, -1, Dir.L, Dir.D)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 1, Dir.L, Dir.D)) { }
                                else if (CheckCell(ref x1, ref y1, 1, -1, Dir.R, Dir.D)) { }
                                else goto case Dir.X;
                                break;

                            case Dir.U:
                                debugString += "U";
                                MarkCellUsed(x1, y1);
                                if (CheckCell(ref x1, ref y1, 1, 0, Dir.U)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 0, Dir.U)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 0, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 0, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, 1, -1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, -1, -1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 1, Dir.L, Dir.U)) { }
                                else if (CheckCell(ref x1, ref y1, -1, -1, Dir.R, Dir.U)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 1, Dir.R, Dir.U)) { }
                                else if (CheckCell(ref x1, ref y1, 1, -1, Dir.L, Dir.U)) { }
                                else goto case Dir.X;
                                break;

                            case Dir.XD:
                                debugString += (char)0x2198;
                                MarkCellUsed(x1, y1);
                                if (CheckCell(ref x1, ref y1, 1, 0, Dir.D)) { }
                                else if (CheckCell(ref x1, ref y1, 0, -1, Dir.L)) { }
                                else if (CheckCell(ref x1, ref y1, -1, -1, Dir.D)) { }
                                else if (CheckCell(ref x1, ref y1, -1, -1, Dir.R)) { }
                                else if (CheckCell(ref x1, ref y1, -1, -1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 0, Dir.U)) { }
                                else if (CheckCell(ref x1, ref y1, 0, 1, Dir.R)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 1, Dir.U)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 1, Dir.L)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, 0, -1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, 0, 1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 0, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 0, Dir.XU)) { }
                                else goto case Dir.X;
                                break;

                            case Dir.XU:
                                debugString += (char)0x2197;
                                MarkCellUsed(x1, y1);
                                if (CheckCell(ref x1, ref y1, 0, -1, Dir.R)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 0, Dir.D)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 1, Dir.U)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 1, Dir.R)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, 0, 1, Dir.L)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 0, Dir.U)) { }
                                else if (CheckCell(ref x1, ref y1, 1, -1, Dir.D)) { }
                                else if (CheckCell(ref x1, ref y1, 1, -1, Dir.L)) { }
                                else if (CheckCell(ref x1, ref y1, 1, -1, Dir.XU)) { }
                                else if (CheckCell(ref x1, ref y1, 0, -1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, 0, 1, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, 1, 0, Dir.XD)) { }
                                else if (CheckCell(ref x1, ref y1, -1, 0, Dir.XD)) { }
                                else goto case Dir.X;
                                break;

                            case Dir.X:
                                if (x != x1 || y != y1)
                                {
                                    MarkCellUsed(x1, y1);
                                    boundaries.Add(new BoundaryX { p1 = new Point(x, y), p2 = new Point(x1, y1), debugString = debugString, motionString = motionString, });
                                }
                                done = true;
                                break;
                        }
                    }
                }
        }

        const char UL = (char)0x2196;
        const char UR = (char)0x2197;
        const char DR = (char)0x2198;
        const char DL = (char)0x2199;
        const char L = (char)0x2b05;
        const char R = (char)0x2b95;
        const char U = (char)0x2b06;
        const char D = (char)0x2b07;
        char GetMotionChar(int dx, int dy)
        {
            if (dx == 1 && dy == 0) return R;
            if (dx == -1 && dy == 0) return L;
            if (dx == 0 && dy == 1) return D;
            if (dx == 0 && dy == -1) return U;
            if (dx == 1 && dy == 1) return DR;
            if (dx == -1 && dy == 1) return DL;
            if (dx == 1 && dy == -1) return UR;
            if (dx == -1 && dy == -1) return UL;
            return ' ';
        }

        string motionString = "";
        bool CheckCell(ref int x, ref int y, int dx, int dy, Dir target, Dir prevDir = Dir.X)
        {
            if (GetCellDir(x + dx, y + dy) == target)
            {
                x = x + dx;
                y = y + dy;
                motionString += GetMotionChar(dx, dy);
                //if (prevDir == Dir.X)
                //{
                //}
                //else
                //{
                //    if (prevDir == Dir.R || prevDir == Dir.L)
                //    {
                //        motionString += GetMotionChar(0, dy);
                //        motionString += GetMotionChar(dx, 0);
                //    }
                //    else
                //    {
                //        motionString += GetMotionChar(dx, 0);
                //        motionString += GetMotionChar(0, dy);
                //    }
                return true;
            }
            return false;
        }

        public override void Initialize()
        {
        }
    }
}
