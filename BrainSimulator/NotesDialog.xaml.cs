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

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for NotesDialob.xaml
    /// </summary>
    public partial class NotesDialog : Window
    {
        public NotesDialog()
        {
            InitializeComponent();
            textBox.Text = MainWindow.theNeuronArray.networkNotes;
            checkBox.IsChecked = false;
        }

        private void OKbutton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.theNeuronArray.networkNotes = textBox.Text;
            MainWindow.theNeuronArray.hideNotes = (bool) checkBox.IsChecked;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
