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
            theBrowser.Navigating += TheBrowser_Navigating;
        }

        private void TheBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            MainWindow.OpenApp(e.Uri.ToString());
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

        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.OpenApp("https://futureai.guru/BrainSimRegister.aspx");
        }
    }
}
