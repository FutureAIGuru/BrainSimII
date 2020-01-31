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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public partial class Module2DKBNDlg : ModuleBaseDlg
    {
        public Module2DKBNDlg()
        {
            InitializeComponent();
        }
        public override bool Draw()
        {
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            if (!base.Draw()) return false;
            Button_Click(null, null);
            return true;
        }

    
        List<string> expandedItems = new List<string>();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string root = textBox1.Text;
            Module2DKBN parent = (Module2DKBN)base.ParentModule;
            expandedItems.Clear();
            FindExpandedItems(theTreeView.Items,root);
            theTreeView.Items.Clear();
            List<Thing> KB = parent.GetTheKB();
            Thing t = parent.Labeled(root);
            if (t != null)
            {
                TreeViewItem tvi = new TreeViewItem { Header = t.Label };
                if (expandedItems.Contains(t.Label)) tvi.IsExpanded = true;
                theTreeView.Items.Add(tvi);
                try
                {
                    AddChildren(t, tvi);
                }
                catch { }
            }
        }

        string LeftOfColon(string s)
        {
            int i = s.IndexOf(':');
            if (i != -1)
            {
                s = s.Substring(0,i);
            }
            return s;
        }
        private void FindExpandedItems(ItemCollection items,string parentLabel)
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
        private void AddChildren(Thing t, TreeViewItem tvi)
        {
            foreach (Thing child in t.Children)
            {
                string header = child.Label+":"+child.useCount;
                if (child.References.Count > 0)
                {
                    header +=  " (";
                    foreach(Link L in child.References)
                    {
                        if (L.weight < 0)header += "-";
                        header += L.T.Label + ", ";
                    }
                    header = header.Substring(0,header.Length - 2);
                    header += ")";
                }
                if (child.V != null)
                {
                    if (child.V is int i)
                    {
                        header += " : " + i.ToString("X");
                    }
                    else
                        header += " : " + child.V.ToString();
                }
                TreeViewItem tviChild = new TreeViewItem { Header = header };
                if (expandedItems.Contains(LeftOfColon(header)))
                    tviChild.IsExpanded = true;
                tvi.Items.Add(tviChild);
                AddChildren(child, tviChild);
                AddReferences(child, tviChild);
                AddReferencedBy(child, tviChild);
            }
        }
        private void AddReferences(Thing t, TreeViewItem tvi)

        {
            if (t.References.Count == 0) return;
            TreeViewItem tviRefLabel = new TreeViewItem { Header = "References: " + t.References.Count.ToString() };
            if (expandedItems.Contains(t.Label + ":References"))
                tviRefLabel.IsExpanded = true;
            tvi.Items.Add(tviRefLabel);
            foreach (Link reference in t.References)
            {
                TreeViewItem tviRef = new TreeViewItem {
                    Header =
                    reference.T.Label + " : " +
                    //reference.weight.ToString() + " : " +
                    reference.hits + " : -" +
                    reference.misses+ " : " +
                    ((float)reference.hits / (float)reference.misses).ToString("f3") 
                };
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
            foreach (Link referencedBy in t.ReferencedBy)
            {
                TreeViewItem tviRef = new TreeViewItem {
                    Header =
                    referencedBy.T.Label + " : " +
                    //reference.weight.ToString() + " : " +
                    referencedBy.hits + " : -" +
                    referencedBy.misses + " : " +
                    ((float)referencedBy.hits / (float)referencedBy.misses).ToString("f3")
                };
                tviRefLabel.Items.Add(tviRef);
            }
        }

        private void TheTreeView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw();
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
            Draw();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            dt.Stop();
        }

      
    }
}
