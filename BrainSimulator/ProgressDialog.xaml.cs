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
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        bool cancelPressed = false;
        public ProgressDialog()
        {
            InitializeComponent();
        }
        public bool SetProgress(float value, string label)
        {
            if (value == 0)
            {
                this.Show();
                this.Visibility = Visibility.Visible;
                if (label != "")
                    theLabel.Text = label;
                cancelPressed = false;
            }
            if (value == 100)
            {
                this.Hide();
                this.Visibility = Visibility.Collapsed;
            }
            theProgressBar.Value = value;
            return cancelPressed;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            cancelPressed = true;
        }
    }

}
