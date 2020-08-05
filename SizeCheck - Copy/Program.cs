using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;

namespace SizeCheck
{
    class Program
    {

        static Neuron[] theArray;
        static void Main(string[] args)
        { 
            List<Neuron[]> theList = new List<Neuron[]>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10; i++)
            {
                int size = (int)Math.Pow(10, 8);
                Console.WriteLine("Starting");

                theArray = new Neuron[size];
                Console.WriteLine("Allocated Array " + sw.Elapsed);

                Parallel.For(0, size, j => theArray[j] = new Neuron());
                theList.Add(theArray);
                Console.WriteLine("'New'ed the entries " +i + " " + sw.Elapsed);
            }
            Console.WriteLine("Press any key");
            Console.ReadKey();


        }
    }
}
