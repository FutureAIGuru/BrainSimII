﻿//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKSDlg : ModuleBaseDlg
    {
        public ModuleUKSDlg()
        {
            InitializeComponent();
        }
        public override bool Draw(bool checkDrawTimer)
        {
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            if (!base.Draw(checkDrawTimer)) return false;
            Button_Click(null, null);
            return true;
        }

        const int maxDepth = 6;
        List<string> expandedItems = new List<string>();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string root = textBox1.Text;
            ModuleUKS parent = (ModuleUKS)base.ParentModule;
            expandedItems.Clear();
            FindExpandedItems(theTreeView.Items, root);
            theTreeView.Items.Clear();
            List<Thing> KB = parent.GetTheKB();
            Thing t = parent.Labeled(root);
            if (t != null)
            {
                TreeViewItem tvi = new TreeViewItem { Header = t.Label };
                tvi.IsExpanded = true; //always expand the top-level item
                theTreeView.Items.Add(tvi);
                tvi.MouseRightButtonDown += Tvi_MouseRightButtonDown;
                AddChildren(t, tvi, 0);
            }
        }

        private void Tvi_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem tvi)
            {
                string thingLabel = LeftOfColon(tvi.Header.ToString()).Trim();
                ModuleUKS parent = (ModuleUKS)base.ParentModule;
                //if (parent.FindModuleByType(typeof(ModuleAudible)) is ModuleAudible mda)
                //{
                //    mda.SpeakThing(thingLabel);
                //}
                //textBox1.Text = LeftOfColon(tvi.Header.ToString());
                //Button_Click(null, null);
                e.Handled = true;
            }
        }

        string LeftOfColon(string s)
        {
            int i = s.IndexOf(':');
            if (i != -1)
            {
                s = s.Substring(0, i);
            }
            return s;
        }

        private void FindExpandedItems(ItemCollection items, string parentLabel)
        {
            foreach (TreeViewItem tvi1 in items)
            {
                if (tvi1.IsExpanded)
                {
                    if (tvi1.Header.ToString().IndexOf("Reference") == -1)
                        expandedItems.Add(LeftOfColon(tvi1.Header.ToString()));
                    else if (tvi1.Header.ToString().IndexOf("References") != -1)
                    {
                        expandedItems.Add(parentLabel.Trim() + ":References");
                    }
                    else if (tvi1.Header.ToString().IndexOf("ReferencedBy") != -1)
                    {
                        expandedItems.Add(parentLabel.Trim() + ":ReferencedBy");
                    }
                }
                FindExpandedItems(tvi1.Items, LeftOfColon(tvi1.Header.ToString()));
            }
        }

        private void AddChildren(Thing t, TreeViewItem tvi, int depth)
        {
            List<Thing> theChildren;
            if ((bool)checkBoxSort.IsChecked) theChildren = t.Children.OrderByDescending(x => x.useCount).ToList();
            else theChildren = t.Children;
            for (int i = 0; i < theChildren.Count; i++)
            {
                Thing child = theChildren[i];
                string header = child.Label + ":" + child.useCount;
                if (child.References.Count > 0)
                {
                    header += " (";
                    ModuleUKS parent = (ModuleUKS)base.ParentModule;
                    Thing best = parent.FindBestReference(child);
                    foreach (Link L in child.References)
                    {
                        if (L.T == best) header += "*";
                        if (L.weight < 0) header += "-";
                        header += L.T.Label + ", ";
                    }
                    header = header.Substring(0, header.Length - 2);
                    header += ")";
                }
                if (child.V != null)
                {
                    if (child.V is int iVal)
                    {
                        header += " : " + iVal.ToString("X");
                    }
                    else
                        header += " : " + child.V.ToString();
                }
                TreeViewItem tviChild = new TreeViewItem { Header = header };
                if (expandedItems.Contains(LeftOfColon(header)))
                    tviChild.IsExpanded = true;
                tvi.Items.Add(tviChild);
                tviChild.MouseRightButtonDown += Tvi_MouseRightButtonDown;
                if (depth < maxDepth)
                {
                    AddChildren(child, tviChild, depth + 1);
                    AddReferences(child, tviChild);
                    AddReferencedBy(child, tviChild);
                }
            }
        }
        private void AddReferences(Thing t, TreeViewItem tvi)

        {
            if (t.References.Count == 0) return;
            TreeViewItem tviRefLabel = new TreeViewItem { Header = "References: " + t.References.Count.ToString() };
            if (expandedItems.Contains(t.Label + ":References"))
                tviRefLabel.IsExpanded = true;
            tvi.Items.Add(tviRefLabel);
            for (int i = 0; i < t.References.Count; i++)
            {
                Link reference = t.References[i];
                TreeViewItem tviRef = new TreeViewItem
                {
                    Header =
                    reference.T.Label + " : " +
                    //reference.weight.ToString() + " : " +
                    reference.hits + " : -" +
                    reference.misses + " : " +
                    ((float)reference.hits / (float)reference.misses).ToString("f3")
                };
                tviRef.MouseRightButtonDown += Tvi_MouseRightButtonDown;
                tviRefLabel.Items.Add(tviRef);
            }
        }
        private void AddReferencedBy(Thing t, TreeViewItem tvi)
        {
            if (t.ReferencedBy.Count == 0) return;
            TreeViewItem tviRefLabel = new TreeViewItem { Header = "ReferencedBy: " + t.ReferencedBy.Count.ToString() };
            if (expandedItems.Contains(t.Label + ":ReferencedBy"))
                tviRefLabel.IsExpanded = true;
            tvi.Items.Add(tviRefLabel);
            for (int i = 0; i < t.ReferencedBy.Count; i++)
            {
                Link referencedBy = t.ReferencedBy[i];
                TreeViewItem tviRef = new TreeViewItem
                {
                    Header =
                    referencedBy.T.Label + " : " +
                    //reference.weight.ToString() + " : " +
                    referencedBy.hits + " : -" +
                    referencedBy.misses + " : " +
                    ((float)referencedBy.hits / (float)referencedBy.misses).ToString("f3")
                };
                tviRef.MouseRightButtonDown += Tvi_MouseRightButtonDown;
                tviRefLabel.Items.Add(tviRef);
            }
        }

        private void TheTreeView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(true);
        }

        DispatcherTimer dt;
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            dt = new DispatcherTimer();
            dt.Interval = new TimeSpan(0, 0, 0, 0, 200);
            dt.Tick += Dt_Tick;
            dt.Start();
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            Draw(true);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            dt.Stop();
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Button_Click(null, null);
            }
        }

        private void CheckBoxSort_Checked(object sender, RoutedEventArgs e)
        {
            Draw(false);
        }

        private void CheckBoxSort_Unchecked(object sender, RoutedEventArgs e)
        {
            Draw(false);
        }
    }
}
