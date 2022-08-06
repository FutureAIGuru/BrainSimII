//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
//to highlight grid cell
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BrainSimulator.Modules
{
    public partial class ModuleAssociationDlg : ModuleBaseDlg
    {
        public ModuleAssociationDlg()
        {
            InitializeComponent();
        }

        ModuleUKS uks = null;
        public bool busy = false;
        public override bool Draw(bool checkDrawTimer)
        {
            if (busy) return false;
            //the datagridview is so slow, always wait for the timer
            if (!base.Draw(checkDrawTimer)) return false;

            if (mouseInWindow) return true;

            ModuleView naSource = MainWindow.theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return false;
            uks = (ModuleUKS)naSource.TheModule;

            Thing mentalModel = uks.GetOrAddThing("MentalModel", "Visual");
            if (mentalModel == null || mentalModel.Children.Count == 0) return false;

            Thing attn = uks.Labeled("ATTN");
            if (attn == null || attn.References.Count == 0) return false;

            busy = true;
            Thing attnTarget = attn.GetReferenceWithAncestor(uks.Labeled("Visual"));


            DataTable dt = new DataTable();
            dt.Columns.Add("*", typeof(string));

            IList<Thing> words = uks.Labeled("Word").Children;
            foreach (Thing child in words)
            {
                dt.Columns.Add(child.Label, typeof(string));
            }

            List<Thing> properties = uks.Labeled("Color").Children.ToList();
            properties.AddRange(uks.Labeled("Area").Children.ToList());
            //properties = properties.OrderBy(x => x.Label).ToList();
            IList<Thing> relationships = uks.Labeled("Relationship").Children;
            //relationships = relationships.OrderBy(x => x.Label.Substring(1)).ToList();


            //collect all the values in a single spot
            float[,] values = ((ModuleAssociation)base.ParentModule).GetAssociations();


            int row = 0;
            foreach (Thing property in properties)
            {
                DataRow dr = dt.NewRow();
                dr[0] = property.Label;
                List<Link> refs = property.GetReferencedByWithAncestor(uks.Labeled("Word"));
                for (int i = 0; i < words.Count; i++)
                {
                    Link l = words[i].HasReference(property);
                    if (l != null)
                    {
                        if (cbRawValues.IsChecked == true)
                            dr[i + 1] = l.hits + "/" + l.misses;
                        else
                            dr[i + 1] = l.Value1.ToString("f2");
                    }
                }
                dt.Rows.Add(dr);
                row++;
            }

            foreach (Thing relationship in relationships)
            {
                DataRow dr = dt.NewRow();
                dr[0] = relationship.Label;
                List<Link> refs = relationship.GetReferencedByWithAncestor(uks.Labeled("Word"));
                for (int i = 0; i < words.Count; i++)
                {
                    Link l = words[i].HasReference(relationship);
                    if (l != null)
                    {
                        if (cbRawValues.IsChecked == true)
                            dr[i + 1] = l.hits + "/" + l.misses;
                        else
                            dr[i + 1] = l.Value1.ToString("f2");
                    }
                }
                dt.Rows.Add(dr);
                row++;
            }


            theGrid.ItemsSource = dt.DefaultView;
            theGrid.SelectionUnit = DataGridSelectionUnit.Cell;

            float[] maxInRow = new float[values.GetLength(0)];
            float[] maxInCol = new float[values.GetLength(1)];
            for (int i = 0; i < values.GetLength(0); i++)
                for (int j = 0; j < values.GetLength(1); j++)
                {
                    if (values[i, j] > maxInRow[i])
                        maxInRow[i] = values[i, j];
                }
            for (int j = 0; j < values.GetLength(1); j++)
                for (int i = 0; i < values.GetLength(0); i++)
                {
                    if (values[i, j] > maxInCol[j])
                        maxInCol[j] = values[i, j];
                }

            //set the background colors of the significant elements
            for (int j = 0; j < values.GetLength(1); j++)
                for (int i = 0; i < values.GetLength(0); i++)
                {
                    if (values[i, j] == maxInRow[i] && values[i, j] == maxInCol[j])
                        SetCellBackground(i, j + 1, Colors.LightGreen);
                    else if (values[i, j] == maxInRow[i])
                        SetCellBackground(i, j + 1, Colors.LightGoldenrodYellow);
                    else if (values[i, j] == maxInCol[j])
                        SetCellBackground(i, j + 1, Colors.Yellow);
                    else
                        SetCellBackground(i, j + 1, Colors.White);
                }

            theGrid.FrozenColumnCount = 1;
            busy = false;
            return true;
        }

        private void SetCellBackground(int rowIndex, int colIndex, Color theBackgroundColor)
        {
            if (colIndex == 7)
            { }
            theGrid.EnableRowVirtualization = false;
            theGrid.EnableColumnVirtualization = false;
            object item = theGrid.Items[rowIndex];
            DataGridRow row = theGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            if (row == null)
            {
                theGrid.ScrollIntoView(item);
                row = theGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            }
            if (row != null)
            {
                DataGridCell cell = GetCell(theGrid, row, colIndex);
                if (cell != null)
                {
                    DataGridCellInfo dataGridCellInfo = new DataGridCellInfo(cell);
                    //theGrid.SelectedCells.Add(dataGridCellInfo);
                    cell.Background = new SolidColorBrush(theBackgroundColor);
                }
            }
        }
        private Color GetCellBackground(int rowIndex, int colIndex)
        {
            object item = theGrid.Items[rowIndex];
            DataGridRow row = theGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            if (row == null)
            {
                theGrid.ScrollIntoView(item);
                row = theGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            }
            if (row != null)
            {
                DataGridCell cell = GetCell(theGrid, row, colIndex);
                if (cell != null)
                {
                    DataGridCellInfo dataGridCellInfo = new DataGridCellInfo(cell);
                    //theGrid.SelectedCells.Add(dataGridCellInfo);
                    if (cell.Background is SolidColorBrush s)
                        return s.Color;
                }
            }
            return Colors.White;
        }

        public static DataGridCell GetCell(DataGrid dataGrid, DataGridRow rowContainer, int column)
        {
            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
                if (presenter == null)
                {
                    /* if the row has been virtualized away, call its ApplyTemplate() method 
                     * to build its visual tree in order for the DataGridCellsPresenter
                     * and the DataGridCells to be created */
                    rowContainer.ApplyTemplate();
                    presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
                }
                if (presenter != null)
                {
                    DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                    if (cell == null)
                    {
                        /* bring the column into view
                         * in case it has been virtualized away */
                        dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
                        cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                    }
                    return cell;
                }
            }
            return null;
        }
        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }
        bool mouseInWindow = false;
        private void theGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            mouseInWindow = true;
            theGrid.Background = new SolidColorBrush(Colors.LightSteelBlue);
            for (int i = 0; i < theGrid.Items.Count; i++)
                for (int j = 1; j < theGrid.Columns.Count; j++)
                {
                    Color color = GetCellBackground(i, j);
                    color.A = 0xff;
                    if (color == Colors.White)
                        SetCellBackground(i, j, Colors.LightSteelBlue);
                }

        }

        private void theGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            mouseInWindow = false;
            theGrid.Background = new SolidColorBrush(Colors.LightGray);
            for (int i = 0; i < theGrid.Items.Count; i++)
                for (int j = 1; j < theGrid.Columns.Count; j++)
                {
                    if (GetCellBackground(i, j) == Colors.LightSteelBlue)
                        SetCellBackground(i, j, Colors.White);
                }
        }

        private void cbRawValues_Checked(object sender, RoutedEventArgs e)
        {
            Draw(false);
        }
    }
}
