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
using System.IO;

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
