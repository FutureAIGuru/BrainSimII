using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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
            if (MainWindow.theNeuronArray.networkNotes == "")
                MainWindow.theNeuronArray.networkNotes = "Purpose:\n\rThings to try:\n\rCurrent state of development:\n\rNotes:\n\r";
            string theNotes = MainWindow.theNeuronArray.networkNotes;
            if (theNotes.IndexOf("<") != 0) //for backward compatibility from before these were RTF
            {
                mainRTB.AppendText(theNotes);
                mainRTB.IsReadOnly = false;
            }
            else
            {
                if (!showToolBar)
                {
                    mainToolBar.Visibility = Visibility.Collapsed;
                    mainRTB.IsReadOnly = true;
                    CancelButton.Visibility = Visibility.Collapsed;
                }
                else
                    mainRTB.IsReadOnly = false;

                //reformat so all hyperlinks in text are converted to hot links
                int beg = Math.Min(theNotes.Length - 1, 1000);
                while (theNotes.IndexOf("http", beg) != -1)
                {
                    beg = theNotes.IndexOf("http", beg);
                    int end = theNotes.IndexOfAny(new char[] { ' ', '<' }, beg);
                    string url = theNotes.Substring(beg, end - beg);
                    if (url[url.Length - 1] == '.') url = url.Remove(url.Length - 1);
                    string newString = "</Run><Hyperlink NavigateUri='" + url + "' >" + url + "</Hyperlink><Run>";
                    theNotes = theNotes.Remove(beg, end - beg);
                    theNotes = theNotes.Insert(beg, newString);
                    beg = beg + newString.Length;
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

                //strip all the hot links back out again
                int beg = 0;
                while (xamlText.IndexOf("<Hyperlink", beg) != -1)
                {
                    beg = xamlText.IndexOf("<Hyperlink", beg);
                    int end = xamlText.IndexOf(">", beg);
                    xamlText = xamlText.Remove(beg, end - beg + 1);
                }
                xamlText = xamlText.Replace("</Hyperlink>", "");

                MainWindow.theNeuronArray.networkNotes = xamlText;
            }
            MainWindow.theNeuronArray.hideNotes = (bool)checkBox.IsChecked;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //we use the text showing, not the hyperlink address
        //You have to press ctrl to follow hyperlinks when editing
        private void MainRTB_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;
            if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) > 0 || (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) > 0 || rtb.IsReadOnly)
            {
                if (e.OriginalSource is Run r)
                {
                    if (r.Parent is Hyperlink hyperlink)
                    {
                        Uri innerText = new Uri(r.Text);
                        Process.Start(new ProcessStartInfo(innerText.AbsoluteUri));
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
