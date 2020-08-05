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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Threading;
using System.Runtime;

namespace SizeTest2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class Neuron
    {
        public int Value;
        int otherValue;
        //35GB

        List<int> abc;
        //42GB
        public Neuron(int i)
        {
            Value = i;
            //abc = new List<int>(0);
            //90
            abc = new List<int>();
            //90
        }

        long longVal;
        //92GB
        public enum modelType { Std, Color, FloatValue, LIF, Random };
        modelType model = modelType.Std;
        //100GB
        String label ="";
        //118 (null) //117 ""
        private bool keepHistory = false;
        //118
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }
        BackgroundWorker bgw = new BackgroundWorker();
        DispatcherTimer dt = new DispatcherTimer();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            theBar.Maximum = boxCount * boxSize;
            bgw.DoWork += Bgw_DoWork;
            bgw.RunWorkerAsync();
            dt.Tick += Dt_Tick;
            dt.Interval = new TimeSpan(0, 0, 0, 0, 500);
            dt.Start();
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            int allocated = 0;
            for (int i = 0; i < theList.Count; i++)
                allocated += theList[i].Count;
            theBar.Value = allocated;
        }

        const int boxCount = 1000;
        const int boxSize = 1000000;
        List<List<Neuron>> theList = new List<List<Neuron>>(boxCount);

        private void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            GC.Collect(3, GCCollectionMode.Forced, true);
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Debug.WriteLine("Starting");

            for (int i = 0; i < boxCount; i++)
                theList.Add(new List<Neuron>(boxSize));
            Debug.WriteLine("Allocated Initial List " + sw.Elapsed);
            GC.Collect(3, GCCollectionMode.Forced, true);
            Debug.WriteLine("GC " + sw.Elapsed);

            Parallel.For(0, boxCount, j =>
            {
                for (int i = 0; i < boxSize; i++)
                    theList[j].Add(new Neuron(i));
            });
            Debug.WriteLine("Allocated Array " + sw.Elapsed);
            GC.Collect(3, GCCollectionMode.Forced, false);
            Debug.WriteLine("GC " + sw.Elapsed);

            Parallel.For(0, boxCount, j =>
            {
                for (int i = 0; i < boxSize; i++)
                    if (theList[j][i].Value % 3 == 0)
                        theList[j][i].Value++;
            });

            Debug.WriteLine("Incremented Array " + sw.Elapsed);
            GC.Collect(3, GCCollectionMode.Forced, false);
            Debug.WriteLine("GC " + sw.Elapsed);
        }
    }
}
