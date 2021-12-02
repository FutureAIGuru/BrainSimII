//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public partial class ModuleUKSDlg : ModuleBaseDlg
    {
        const int maxDepth = 6;
        int totalItemCount = 0;
        bool mouseInTree = false; //prevent auto-update while the mouse is in the tree

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


        List<string> expandedItems = new List<string>();
        int charsPerLine = 60;
        bool updateFailed = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //figure out how wide the treeview is so we can wrap the text
            var typeFace = new System.Windows.Media.Typeface(theTreeView.FontFamily.ToString());
            var ft = new FormattedText("xxxxxxxxxx", System.Globalization.
                CultureInfo.CurrentCulture,
                theTreeView.FlowDirection,
                typeFace,
                theTreeView.FontSize,
                theTreeView.Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            charsPerLine = (int)( 10 * theTreeView.ActualWidth / ft.Width);
            charsPerLine -= 10; //leave a little margin...the indent is calculated for individual entries
            

            try
            {
                string root = textBox1.Text;
                ModuleUKS parent = (ModuleUKS)base.ParentModule;
                if (!updateFailed)
                {
                    expandedItems.Clear();
                    FindExpandedItems(theTreeView.Items, "");
                }
                updateFailed = false;
                theTreeView.Items.Clear();
                int childCount = 0;
                int refCount = 0;
                List<Thing> uks = parent.GetTheUKS();
                foreach(Thing t1 in uks)
                {
                    childCount += t1.Children.Count;
                    refCount += t1.References.Count;
                }
                statusLabel.Content = uks.Count + " Nodes  " + (childCount + refCount)+" Edges";
                Thing t = parent.Labeled(root);
                if (t != null)
                {
                    totalItemCount = 0;
                    TreeViewItem tvi = new TreeViewItem { Header = t.Label };
                    tvi.IsExpanded = true; //always expand the top-level item
                    theTreeView.Items.Add(tvi);
                    totalItemCount++;
                    tvi.MouseRightButtonDown += Tvi_MouseRightButtonDown;
                    AddChildren(t, tvi, 0, t.Label);
                }
                else if (root == "") //search for unattached Things
                {
                    try //ignore problems of collection modified
                    {
                        foreach (Thing t1 in uks)
                        {
                            if (t1.Parents.Count == 0)
                            {
                                TreeViewItem tvi = new TreeViewItem { Header = t1.Label };
                                theTreeView.Items.Add(tvi);
                            }
                        }
                    }
                    catch { updateFailed = true; }
                }
            }
            catch
            {
                updateFailed = true;
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
                        expandedItems.Add(parentLabel + "|" + LeftOfColon(tvi1.Header.ToString()));
                    else if (tvi1.Header.ToString().IndexOf("References") != -1)
                    {
                        expandedItems.Add(parentLabel + "|" + ":References");
                    }
                    else if (tvi1.Header.ToString().IndexOf("ReferencedBy") != -1)
                    {
                        expandedItems.Add(parentLabel + "|" + ":ReferencedBy");
                    }
                }
                FindExpandedItems(tvi1.Items, parentLabel + "|" + LeftOfColon(tvi1.Header.ToString()));
            }
        }


        private void AddChildren(Thing t, TreeViewItem tvi, int depth, string parentLabel)
        {
            if (totalItemCount > 3000) return;

            IList<Thing> theChildren;
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
                    List<Link> refs = child.References.OrderBy(x => -x.Value1).ToList();
                    for (int j = 0; j < refs.Count; j++)// child.References.Count; j++)// each (Link L in child.References)
                    {
                        if (header.Length - header.LastIndexOf('\n') > charsPerLine-7*depth) header += "\n";
                        Link l = refs[j];// child.References[j];
                        if (l is Relationship r)
                        {
                            header += r.relationshipType.Label + "->" + r.T.Label + ", ";
                        }
                        else
                        {
                            if (l.T == best) header += "*";
                            if (l.weight < 0) header += "-";
                            header += l.T.Label + ", ";
                        }
                    }
                    //header = header.Substring(0, header.Length - 2);
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
                if (child.lastFired == MainWindow.theNeuronArray.Generation)
                    tviChild.Background = new SolidColorBrush(Colors.LightGreen);

                if (expandedItems.Contains("|" + parentLabel + "|" + LeftOfColon(header)))
                    tviChild.IsExpanded = true;
                tvi.Items.Add(tviChild);
                totalItemCount++;
                tviChild.MouseRightButtonDown += Tvi_MouseRightButtonDown;
                if (depth < maxDepth)
                {
                    AddChildren(child, tviChild, depth + 1, parentLabel + "|" + child.Label);
                    AddReferences(child, tviChild, parentLabel);
                    AddReferencedBy(child, tviChild, parentLabel);
                }
            }
        }

        private void AddReferences(Thing t, TreeViewItem tvi, string parentLabel)
        {
            if (t.References.Count == 0) return;
            TreeViewItem tviRefLabel = new TreeViewItem { Header = "References: " + t.References.Count.ToString() };

            string fullString = "|" + parentLabel + "|" + t.Label + "|:References";
            if (expandedItems.Contains(fullString))
                tviRefLabel.IsExpanded = true;
            tvi.Items.Add(tviRefLabel);
            totalItemCount++;
            IList<Link> sortedReferences = t.References.OrderBy(x => -x.Value1).ToList();
            for (int i = 0; i < sortedReferences.Count; i++)
            {
                Link reference = sortedReferences[i];

                if (reference.T.HasAncestorLabeled("Value"))
                {
                    TreeViewItem tviRef = new TreeViewItem
                    {
                        Header = reference.T.Label + " " + reference.T.Parents[0].Label + " " + reference.weight,
                    };
                    tviRef.MouseRightButtonDown += Tvi_MouseRightButtonDown;
                    tviRefLabel.Items.Add(tviRef);
                    totalItemCount++;
                }
                else if (reference is Relationship r)
                {
                    TreeViewItem tviRef = new TreeViewItem
                    {
                        Header = r.relationshipType.Label + "->" + reference.T.Label,
                    };
                    tviRef.MouseRightButtonDown += Tvi_MouseRightButtonDown;
                    tviRefLabel.Items.Add(tviRef);
                    totalItemCount++;
                }
                else
                {
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
                    totalItemCount++;
                }
            }
        }
        private void AddReferencedBy(Thing t, TreeViewItem tvi, string parentLabel)
        {
            if (t.ReferencedBy.Count == 0) return;
            TreeViewItem tviRefLabel = new TreeViewItem { Header = "ReferencedBy: " + t.ReferencedBy.Count.ToString() };

            string fullString = "|" + parentLabel + "|" + t.Label + "|:ReferencedBy";
            if (expandedItems.Contains(fullString))
                tviRefLabel.IsExpanded = true;
            tvi.Items.Add(tviRefLabel);
            totalItemCount++;
            IList<Link> sortedReferencedBy = t.ReferencedBy.OrderBy(x => -x.Value1).ToList();
            for (int i = 0; i < sortedReferencedBy.Count; i++)
            {
                Link referencedBy = sortedReferencedBy[i];
                TreeViewItem tviRef;
                if (referencedBy is Relationship r)
                {
                    if (t == r.relationshipType)
                    {
                        tviRef = new TreeViewItem
                        {
                            Header =
                               r.source.Label + "->" + r.T.Label + " " +
                               referencedBy.hits + " : -" +
                               referencedBy.misses + " : " +
                               ((float)referencedBy.hits / (float)referencedBy.misses).ToString("f3")
                        };
                    }
                    else
                    {
                        tviRef = new TreeViewItem
                        {
                            Header =
                               r.T.Label + " : " + r.relationshipType.Label + " " +
                               referencedBy.hits + " : -" +
                               referencedBy.misses + " : " +
                               ((float)referencedBy.hits / (float)referencedBy.misses).ToString("f3")
                        };
                    }
                }
                else
                {
                    tviRef = new TreeViewItem
                    {
                        Header =
                           referencedBy.T.Label + " : " +
                           //reference.weight.ToString() + " : " +
                           referencedBy.hits + " : -" +
                           referencedBy.misses + " : " +
                           ((float)referencedBy.hits / (float)referencedBy.misses).ToString("f3")
                    };
                }
                tviRef.MouseRightButtonDown += Tvi_MouseRightButtonDown;
                tviRefLabel.Items.Add(tviRef);
                totalItemCount++;
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
            if (!mouseInTree)
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

        private void theTreeView_MouseEnter(object sender, MouseEventArgs e)
        {
            mouseInTree = true;
            theTreeView.Background = new SolidColorBrush(Colors.LightSteelBlue);
        }

        private void theTreeView_MouseLeave(object sender, MouseEventArgs e)
        {
            mouseInTree = false;
            theTreeView.Background = new SolidColorBrush(Colors.LightGray);
        }
    }
}
