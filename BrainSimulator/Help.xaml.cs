using System;
using System.IO;
using System.Windows;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class Help : Window
    {
        public Help(string urlToShow = "")
        {
            InitializeComponent();
            bool showHelp = (bool)Properties.Settings.Default["ShowHelp"];
            theCheckBox.IsChecked = !showHelp;
            if (urlToShow == "")
            {
                string fullpath = Path.GetFullPath("./resources/getting started.htm");
                Uri theUri = new Uri("file:///" + fullpath);
                theBrowser.Navigate(theUri);
            }
            else
            {
                theBrowser.Navigate(urlToShow);
                theCheckBox.Visibility = Visibility.Collapsed;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (theCheckBox.Visibility == Visibility.Visible)
            {
                Properties.Settings.Default["ShowHelp"] = !theCheckBox.IsChecked;
                Properties.Settings.Default.Save();
            }
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string fullpath = Path.GetFullPath("./resources/getting started.htm");
            Uri theUri = new Uri("file:///" + fullpath);
            theBrowser.Navigate(theUri);
        }
    }
}
