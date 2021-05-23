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
        DateTime timeRemaining;

        public ProgressDialog()
        {
            InitializeComponent();
            startTime = DateTime.Now;
            timeLabel.Content = "";
            Owner = MainWindow.thisWindow;
        }

        public bool SetProgress(float value, string label)
        {
            if (value == 100)
            {
                MainWindow.thisWindow.MainMenu.IsEnabled = true;
                MainWindow.thisWindow.MainToolBar.IsEnabled = true;
                cancelPressed = true;
                CancelProgressBar();
            }
            else if (value == 0)
            {
                cancelPressed = false;
                if (label != "")
                    theLabel.Text = label;
                this.Visibility = Visibility.Visible;

                startTime = DateTime.Now;
                MainWindow.thisWindow.MainMenu.IsEnabled = false;
                MainWindow.thisWindow.MainToolBar.IsEnabled = false;
                theProgressBar.Value = 0;
                cancelPressed = false;
                timeLabel.Content = "Calculating Estimated Duration...";
            }
            ProcessProgress(value);
            return cancelPressed;
        }

        private bool ProcessProgress(float value)
        {
            // value is range 0 to 100, we can calculate the total time from time spent till now...
            DateTime currentTime = DateTime.Now;
            TimeSpan elapsedTime = currentTime - startTime;
            if (value == 0.0) value = 0.000001F;   // avoid infinity factor for very large tasks...
            float factor = (100 - value);
            TimeSpan remainingTime = TimeSpan.FromTicks((long)(elapsedTime.Ticks * factor));

            if (value < 0.1)
            {
                timeLabel.Content = "Awaiting first progress update...";
            }
            else if(remainingTime.TotalSeconds > 0 && elapsedTime.TotalSeconds + remainingTime.TotalSeconds < 120)
            {
                // recompute for seconds left rather than ETA...
                factor = (100 - value) / 100;
                remainingTime = TimeSpan.FromTicks((long)(elapsedTime.Ticks * factor));
                timeLabel.Content = (int)remainingTime.TotalSeconds + " seconds remaining";
            }
            else
            {
                // recompute for ETA rather than time left.
                timeRemaining = DateTime.Now;
                timeRemaining = timeRemaining.AddSeconds(remainingTime.TotalSeconds);
                if (startTime.Date == timeRemaining.Date)
                {
                    timeLabel.Content = "Time Elapsed: " + string.Concat(elapsedTime.ToString()).Substring(0, 8) + 
                                        "\nAppr. Finish Time: " + string.Concat(timeRemaining.ToShortTimeString());
                }
                else
                {
                    timeLabel.Content = "Time Elapsed: " + string.Concat(elapsedTime.ToString()).Substring(0, 8) +
                                        "\nAppr. Finish Time: " + string.Concat(timeRemaining);
                }
            }
            theProgressBar.Value = value;
            MainWindow.thisWindow.UpdateFreeMem();
            return cancelPressed;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            cancelPressed = true;
            startTime = DateTime.Now;
            MainWindow.thisWindow.MainMenu.IsEnabled = true;
            MainWindow.thisWindow.MainToolBar.IsEnabled = true;
            CancelProgressBar();
        }

        public void CancelProgressBar()
        {
            this.Hide();
            this.Visibility = Visibility.Collapsed;
        }
    }

}
