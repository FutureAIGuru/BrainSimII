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
    /// Interaction logic for GetUpdateDialog.xaml
    /// </summary>
    public partial class GetUpdateDialog : Window
    {
        public GetUpdateDialog()
        {
            InitializeComponent();
        }

        private void ButtonGetUpdate_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CheckForUpdates = cbDontAsk.IsChecked == false;
            Properties.Settings.Default.Save();
            MainWindow.OpenApp("https://futureai.guru/BrainSimDownload.aspx");
            this.Close();
            MainWindow.thisWindow?.Close();
        }

        private void ButtonIgnore_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CheckForUpdates = cbDontAsk.IsChecked == false;
            Properties.Settings.Default.Save();

            this.Close();
        }
    }
}
