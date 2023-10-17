using System.Windows;

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
            MainWindow.OpenApp(MainWindow.webURL+"/technologies/BrainSimII-download");
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
