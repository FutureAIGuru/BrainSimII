using System.Windows;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for ModuleDescription.xaml
    /// </summary>
    public partial class ModuleDescription : Window
    {
        public ModuleDescription(string shortDescription, string longDescription)
        {
            InitializeComponent();
            textBlock.Text = "Summary:\n\r" + shortDescription + "\n\r\n\rDetails:\n\r" + longDescription;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
