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
        DateTime startTime;
        public ProgressDialog()
        {
            InitializeComponent();
            startTime = DateTime.Now;
        }
        public bool SetProgress(float value, string label)
        {
            if (value == 100)
            {
                this.Hide();
                this.Visibility = Visibility.Collapsed;
            }
            else if (value == 0)
            {
                this.Show();
                this.Visibility = Visibility.Visible;
                if (label != "")
                    theLabel.Text = label;
                startTime = DateTime.Now;
                cancelPressed = false;
            }
            else
            {
                DateTime currentTime = DateTime.Now;
                TimeSpan elapsedTime = currentTime - startTime;
                float factor = (100 - value) / value;
                TimeSpan remainingTime = TimeSpan.FromTicks((long)(elapsedTime.Ticks * factor));
                timeLabel.Content = (int)elapsedTime.TotalSeconds + "s Elapsed    " + (int)remainingTime.TotalSeconds + "s Remaining";
            }
            theProgressBar.Value = value;
            return cancelPressed;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            cancelPressed = true;
            this.Hide();
            this.Visibility = Visibility.Collapsed;
        }
    }

}
