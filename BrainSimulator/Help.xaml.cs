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
            dontShow.IsChecked = !showHelp;
            if (urlToShow == "")
            {
                string fullpath = Path.GetFullPath("./resources/getting started.htm");
                Uri theUri = new Uri("file:///" + fullpath);
                theBrowser.Navigate(theUri);
            }
            else
            {
                theBrowser.Navigate(urlToShow);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["ShowHelp"] = !dontShow.IsChecked;
            Properties.Settings.Default.Save();
            Close();
        }
    }
}
