using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Markup;
using System.Xml;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for NotesDialob.xaml
    /// </summary>
    public partial class NotesDialog : Window
    {
        public NotesDialog(bool showToolBar = false)
        {
            InitializeComponent();
            string theNotes = MainWindow.theNeuronArray.networkNotes;
            if (theNotes.IndexOf("<") != 0) //for backward compatibility from before these were RTF
            {
                mainRTB.AppendText(theNotes);
            }
            else
            {
                if (!showToolBar)
                {
                    mainToolBar.Visibility = Visibility.Collapsed;
                    mainRTB.IsReadOnly = true;
                    CancelButton.Visibility = Visibility.Collapsed;
                }
                
                StringReader stringReader = new StringReader(theNotes);
                XmlReader xmlReader = XmlReader.Create(stringReader);
                Section sec = XamlReader.Load(xmlReader) as Section;
                FlowDocument doc = new FlowDocument();
                while (sec.Blocks.Count > 0)
                    doc.Blocks.Add(sec.Blocks.FirstBlock);
                mainRTB.Document = doc;
                checkBox.IsChecked = false;
            }
            Owner = MainWindow.thisWindow;
            ShowInTaskbar = false;

        }

        private void OKbutton_Click(object sender, RoutedEventArgs e)
        {
            if (!mainRTB.IsReadOnly)
            {
                TextRange range;
                var a = mainRTB.Document;
                range = new TextRange(mainRTB.Document.ContentStart, mainRTB.Document.ContentEnd);
                MemoryStream stream = new MemoryStream();
                range.Save(stream, DataFormats.Xaml);
                string xamlText = Encoding.UTF8.GetString(stream.ToArray());
                MainWindow.theNeuronArray.networkNotes = xamlText;
                MainWindow.theNeuronArray.hideNotes = (bool)checkBox.IsChecked;
            }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
