using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            char inKey = 'c';

            while (inKey != 'x')
            {
                Console.Write("Enter leak rate: ");
                string input = Console.ReadLine();
                if (float.TryParse(input, out float leakrate))
                {
                    Console.WriteLine("Leak Rate: " + leakrate);
                    for (int i = 4; i < 30; i+=4)
                    {
                        float charge, w;
                        GetWeight(leakrate, i, out charge, out w);
                        Console.WriteLine(i + ":  Accum Leakage: " + charge + "  Synapse Weight Needed: " + w);
                    }
                }
                GetWeight(leakrate, 4, out float charge4, out float w4);
                GetWeight(leakrate, 10, out float charge10, out float w10);
                GetWeight(leakrate, 20, out float charge20, out float w20);
                float diff = Math.Abs(w4 - w20);
                Console.WriteLine(diff);
                Console.Write("X to exit: ");
                inKey = Console.ReadKey().KeyChar;
            }
        }

        private static void GetWeight(float leakrate, int i, out float charge, out float w)
        {
            charge = (float)Math.Pow(leakrate, i);
            w = 1 / (1 + charge);
        }
    }
}
