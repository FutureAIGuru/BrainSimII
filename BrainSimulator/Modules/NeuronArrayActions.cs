using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;


namespace BrainSimulator
{
    public partial class NeuronArray
    {
        private void HandleProgrammedActions()
        {
            foreach (NeuronArea na in areas)
            {
                if (na.CommandLine == null) continue;
                string command = na.CommandLine;
                int commandEnd = na.CommandLine.IndexOf(' ');
                if (commandEnd > 0)
                    command = na.CommandLine.Substring(0, commandEnd);
                Object[] parameters = new Object[1];
                parameters[0] = na;
                Type theType = this.GetType();
                //MethodInfo[] Methods = theType.GetMethods(); //this will list the available functions (with some effort)
                //Type para = theMethod.GetParameters()[0].ParameterType;
                MethodInfo theMethod = theType.GetMethod(command);
                if (theMethod != null)
                    theMethod.Invoke(this, parameters);
            }
        }



        public struct Entry
        {
            public string name;
            public float[] neuronValues;
            public int compareType;
        }
        class KBItem
        {
            public List<Entry> entries;
            public float matchValue;
            public float usage;
        }
        List<KBItem> KBContent = new List<KBItem>();

        public void KB(NeuronArea na)
        {
            //parse command string and get input values
            string[] parameters = na.CommandLine.Split(' ');
            KBItem currentValues = new KBItem();
            currentValues.entries = new List<Entry>();
            Entry newEntry = new Entry();
            NeuronArea naOutput = null;
            for (int i = 0; i < parameters.Length; i++)
            {
                string param = parameters[i];
                int compareType = 0;
                switch (param)
                {
                    //find inputs
                    case "-ip": //single bit proximity match
                        compareType = 1;
                        goto case "-i";
                    case "-ie": //exact match
                        compareType = 2;
                        goto case "-i";
                    case "-i": //normal bit match
                        NeuronArea naI = FindAreaByLabel(parameters[i + 1]);
                        if (naI == null) break;
                        newEntry.name = naI.Label;
                        newEntry.neuronValues = new float[naI.NeuronCount];
                        newEntry.compareType = compareType;
                        naI.BeginEnum();
                        for (int j = 0; j < naI.NeuronCount; j++)
                            newEntry.neuronValues[j] = naI.GetNextNeuron().LastCharge;
                        currentValues.entries.Add(newEntry);
                        break;
                    //find outputs
                    case "-o":
                        naOutput = FindAreaByLabel(parameters[i + 1]);
                        break;
                }
            }
            // is the item already in the KB
            int foundIndex = -1;
            float foundValue = float.MaxValue;
            for (int i = 0; i < KBContent.Count; i++)
            {
                KBItem contentItem = KBContent[i];
                contentItem.usage--;
                contentItem.matchValue = 0;
                for (int j = 0; j < contentItem.entries.Count; j++)
                {
                    Entry entry = contentItem.entries[j];
                    //calculate the match value
                    contentItem.matchValue += CompareItems(entry, currentValues.entries[j], entry.compareType);
                }
                if (contentItem.matchValue < foundValue)
                {
                    foundValue = contentItem.matchValue;
                    foundIndex = i;
                }
            }
            if (foundIndex >= 0)
            {
                KBContent[foundIndex].usage += 10;
            }
            // item is not in the KB...add it
            if (foundIndex == -1 || foundValue > 1)
            {
                if (KBContent.Count > 3) //memory is full...forget the least-used item.
                {
                    float lowestUsage = KBContent[0].usage;
                    int lowestUsageIndex = 0;
                    for (int i = 1; i < KBContent.Count; i++)
                        if (KBContent[i].usage < lowestUsage)
                        {
                            lowestUsage = KBContent[i].usage;
                            lowestUsageIndex = i;
                        }
                    KBContent.RemoveAt(lowestUsageIndex);
                }
                currentValues.usage = 0;
                KBContent.Add(currentValues);
                foundIndex = KBContent.Count - 1;
                foundValue = 0;
            }

            if (naOutput != null && foundValue <= 1 && foundIndex != -1)
            {
                naOutput.BeginEnum();
                int j = 0;
                Entry firstOutput = KBContent[foundIndex].entries[0];

                for (Neuron n = naOutput.GetNextNeuron(); n != null; n = naOutput.GetNextNeuron())
                {
                    if (j >= firstOutput.neuronValues.Length) break;
                    n.CurrentCharge = n.LastCharge = firstOutput.neuronValues[j];
                    j++;
                }
            }

            //put the confidence in the first neuron
            Neuron n1 = na.GetNeuronAt(0);
            n1.CurrentCharge = n1.LastCharge = 1 / (1 + foundValue);
        }
        //calculates the distance between two items
        //item type exact: max value for any error
        //item type multibit: count the number of erroneous bits
        //item type singlebit: count the distance between the bit and the target
        public float CompareItems(Entry entry1, Entry entry2, int compareType)
        {
            float value = 0;
            if (entry1.neuronValues.Length != entry2.neuronValues.Length) return float.MaxValue;
            if (compareType == 0) //normal match
            {
                for (int i = 0; i < entry1.neuronValues.Length; i++)
                {
                    float diff = Math.Abs(entry1.neuronValues[i] - entry2.neuronValues[i]);
                    if (diff > 0.2f) value += diff;
                }
            }
            if (compareType == 1) //proximity match
            {
                for (int i = 0; i < entry1.neuronValues.Length; i++)
                {
                    float diff1, diff2, diff3;
                    if (i == 0)
                    {
                        diff1 = Math.Abs(entry1.neuronValues[i] - entry2.neuronValues[i]);
                        diff2 = Math.Abs(entry1.neuronValues[i + 1] - entry2.neuronValues[i]);
                        diff3 = Math.Abs(entry1.neuronValues[i] - entry2.neuronValues[i + 1]);
                    }
                    else if (i == entry1.neuronValues.Length - 1)
                    {
                        diff1 = Math.Abs(entry1.neuronValues[i] - entry2.neuronValues[i]);
                        diff2 = Math.Abs(entry1.neuronValues[i] - entry2.neuronValues[i - 1]);
                        diff3 = Math.Abs(entry1.neuronValues[i - 1] - entry2.neuronValues[i]);
                    }
                    else
                    {
                        diff1 = Math.Abs(entry1.neuronValues[i] - entry2.neuronValues[i]);
                        diff2 = Math.Abs(entry1.neuronValues[i + 1] - entry2.neuronValues[i]);
                        diff3 = Math.Abs(entry1.neuronValues[i - 1] - entry2.neuronValues[i]);
                    }
                    float diff = Math.Min(Math.Min(diff1, diff2), diff3);
                    value += diff;
                }
            }
            if (compareType == 2) //exact match
            {
                for (int i = 0; i < entry1.neuronValues.Length; i++)
                {
                    float diff = Math.Abs(entry1.neuronValues[i] - entry2.neuronValues[i]);
                    if (diff > 0.1f) return float.MaxValue;
                }

            }

            return value;
        }

        //looks for a beginning match only
        private NeuronArea FindAreaByCommand(string command)
        {
            return areas.Find(na => na.CommandLine.IndexOf(command) == 0);
        }

        //needs a complete match
        private NeuronArea FindAreaByLabel(string label)
        {
            return areas.Find(na => na.Label.Trim() == label);
        }

        //these must be vertically-oriented single-column areas
        private int GetPropertyNeuronIndex(string command, float value)
        {
            NeuronArea na = FindAreaByCommand(command);
            if (na == null) return -1;
            //scale the value
            int height = na.LastNeuron - na.FirstNeuron;
            int offset = (int)((float)height * value);
            return na.FirstNeuron + offset;
        }

        DateTime[] lastFiring = null;
        public void Hebbian(NeuronArea theArea)
        {
            //we work with clock time stamps so things work regardless of how fast the engine is running

            //set up the last-firing array
            if (lastFiring == null)
            {
                lastFiring = new DateTime[theArea.NeuronCount];
            }

            //make sure every inUse input  has a small-wieght connection to every inUse output.
            NeuronArea na = new NeuronArea(theArea.FirstNeuron, theArea.LastNeuron, "", "copy",0);
            theArea.BeginEnum();
            for (Neuron n = theArea.GetNextNeuron(); n != null; n = theArea.GetNextNeuron())
            {
                //iterate through the input neurons
                if (n.InUse())
                {
                    na.BeginEnum();
                    for (Neuron n1 = na.GetNextNeuron(); n1 != null; n1 = na.GetNextNeuron())
                    {
                        //iterate through the output neurons
                        if (n1.InUse() && n1 != n)
                        {
                            Synapse s = n.FindSynapse(n1.Id);
                            if (s == null)
                                n.AddSynapse(n1.Id, 0.001f, this, false);
                        }
                    }
                }
            }

            int count = 0;
            theArea.BeginEnum();
            for (Neuron n = theArea.GetNextNeuron(); n != null; n = theArea.GetNextNeuron())
            {
                if (n.LastCharge > .90)
                {
                    DateTime currentFiring = DateTime.Now;
                    lastFiring[count] = currentFiring;
                    for (int i = 0; i < lastFiring.Length; i++)
                    {
                        if (lastFiring[i] != null)
                        {
                            TimeSpan ts = currentFiring.Subtract(lastFiring[i]);
                            if (ts.TotalSeconds < 3)
                            {
                                Neuron source = theArea.GetNeuronAt(i);
                                Neuron target = MainWindow.theNeuronArray.neuronArray[n.Id];
                                Synapse s = source.FindSynapse(target.Id);
                                if (s != null)
                                {
                                    float newWeight = s.Weight + 0.1f;
                                    if (newWeight > .95) newWeight = .95f;
                                    source.AddSynapse(target.Id, newWeight, this, false);
                                }
                            }
                        }
                    }
                }
                count++;
            }
        }

        
        public void AddToKB(NeuronArea na)
        {
            NeuronArea naProps = FindAreaByCommand("PropertyArea");
            NeuronArea naVF = FindAreaByCommand("Objects");
            if (naVF == null || naProps == null) return;

            naVF.BeginEnum();
            //fire each neuron in the visual field
            for (Neuron n = naVF.GetNextNeuron(); n != null; n = naVF.GetNextNeuron())
            {
                //clear the firing of the neurons in the KB and the property area
                naVF.ClearAllNeurons();
                naProps.ClearAllNeurons();

                //fire the neuron for the object we're testing for
                n.LastCharge = 1;
                n.CurrentCharge = 1;
                n.Fire1(this);
                n.Fire2(Generation);

                int propertiesCount = 0;
                naProps.BeginEnum();
                //fire each neuron in the properties area 
                for (Neuron nProp = naProps.GetNextNeuron(); nProp != null; nProp = naProps.GetNextNeuron())
                {
                    nProp.Fire2(Generation);//force the next generation
                    nProp.Fire1(this);
                    if (nProp.LastCharge > .99)
                        propertiesCount++;
                }
                if (propertiesCount == 0) break;
                //now check to see what fired in the KB
                bool fired = false;
                na.BeginEnum();
                //Check each neuron in the KB
                for (Neuron nKB = na.GetNextNeuron(); nKB != null; nKB = na.GetNextNeuron())
                {
                    if (nKB.CurrentCharge > 0.90)
                    {
                        fired = true;
                        nKB.LastCharge = 1;
                        break;
                    }
                }
                //if no neurons fired in the KB add the pattern to the KB
                if (!fired)
                {
                    //allocate a new neuron in the KB
                    Neuron nKB = na.GetFreeNeuron();
                    if (nKB != null)
                    {
                        //fire each neuron in the visual field
                        naProps.BeginEnum();
                        for (Neuron nProp = naProps.GetNextNeuron(); nProp != null; nProp = naProps.GetNextNeuron())
                        {
                            if (nProp.LastCharge > .99)
                            {
                                nProp.AddSynapse(nKB.Id, 1 / (float)propertiesCount, this, false);
                                nProp.AddSynapse(nKB.Id + 4 * rows, 1, this, false);
                                Neuron nAlt = neuronArray[nKB.Id + 2 * rows];
                                nAlt.AddSynapse(nProp.Id, 1, this, false);
                            }
                            else
                                nProp.AddSynapse(nKB.Id, -1, this, false);
                        }
                    }
                }
                n.LastCharge = 1;
                n.CurrentCharge = 1;
            }
        }


        struct NeuronCount
        {
            public int count;
            public float color;
            public int[] byRow;
            public int[] byCol;
        }

        public void FindRectangles(NeuronArea na)
        {
            na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
            List<NeuronCount> theCounts = new List<NeuronCount>();
            //clear the visual field
            NeuronArea naVF = FindAreaByCommand("Objects");
            if (naVF == null) return;
            naVF.BeginEnum();
            for (Neuron n = naVF.GetNextNeuron(); n != null; n = naVF.GetNextNeuron())
                n.DeleteAllSynapes();

            //count the instances of firings (of specific colors) per row and column
            for (int i = X1; i < X2; i++)
                for (int j = Y1; j < Y2; j++)
                {
                    int neuronIndex = GetNeuronIndex(i, j);
                    int outputIndex = neuronIndex + (X2 - X1 + 1) * rows;
                    float n = neuronArray[neuronIndex].LastCharge;
                    if (n != 0)
                    {
                        NeuronCount Y;
                        int X = theCounts.FindIndex(x => x.color == n);
                        if (X < 0)
                        {
                            Y = new NeuronCount();
                            Y.color = n;
                            Y.count = 0;
                            Y.byCol = new int[X2 - X1];
                            Y.byRow = new int[Y2 - Y1];
                            theCounts.Add(Y);
                        }
                        else
                        {
                            Y = theCounts[X];
                        }
                        Y.byRow[j - Y1]++;
                        Y.byCol[i - X1]++;
                    }
                }
            foreach (NeuronCount Y in theCounts)
            {
                bool occluded = false;
                int minX = -1; int minY = -1; int maxX = -1; int maxY = -1;
                for (int i = 0; i < Y.byRow.Length; i++)
                {
                    if (Y.byRow[i] == 2)
                        if (minY == -1) minY = i;
                        else maxY = i;
                    else if (Y.byRow[i] != 0) occluded = true;
                }
                for (int i = 0; i < Y.byCol.Length; i++)
                {
                    if (Y.byCol[i] >= 2)
                        if (minX == -1) minX = i;
                        else maxX = i;
                    else if (Y.byCol[i] != 0) occluded = true;
                }
                if (minX != -1 && maxX != -1 && minY != -1 && maxY != -1)
                {
                    //a rectangle was found!!
                    float width = X2 - X1;
                    float height = Y2 - Y1;
                    float centerX = (float)(1 + minX + (maxX - minX) / 2) / width;
                    float centerY = (float)(1 + minY + (maxY - minY) / 2) / height;
                    float sizeX = (maxX - minX) / width;
                    float sizeY = (maxY - minY) / height;
                    //aspect ratio is scaled from 0 (maxy) to 1 (maxX);
                    float aspect = sizeY / 2 - sizeX / 2 + .5f;

                    Neuron n = naVF.GetFreeNeuron();
                    int targetNeuronIndex = GetPropertyNeuronIndex("CenterX", centerX);
                    if (targetNeuronIndex != -1)
                        n.AddSynapse(targetNeuronIndex, 1, this, false);
                    targetNeuronIndex = GetPropertyNeuronIndex("CenterY", centerY);
                    if (targetNeuronIndex != -1)
                        n.AddSynapse(targetNeuronIndex, 1, this, false);
                    targetNeuronIndex = GetPropertyNeuronIndex("CenterY", centerY);
                    if (targetNeuronIndex != -1)
                        n.AddSynapse(targetNeuronIndex, 1, this, false);
                    targetNeuronIndex = GetPropertyNeuronIndex("SizeX", sizeX);
                    if (targetNeuronIndex != -1)
                        n.AddSynapse(targetNeuronIndex, 1, this, false);
                    targetNeuronIndex = GetPropertyNeuronIndex("SizeY", sizeY);
                    if (targetNeuronIndex != -1)
                        n.AddSynapse(targetNeuronIndex, 1, this, false);
                    targetNeuronIndex = GetPropertyNeuronIndex("Color", Y.color);
                    if (targetNeuronIndex != -1)
                        n.AddSynapse(targetNeuronIndex, 1, this, false);
                    targetNeuronIndex = GetPropertyNeuronIndex("Aspect", aspect);
                    if (targetNeuronIndex != -1)
                        n.AddSynapse(targetNeuronIndex, 1, this, false);
                    targetNeuronIndex = GetPropertyNeuronIndex("ObjectType", 0);
                    if (targetNeuronIndex != -1)
                    {
                        if (sizeX == sizeY)
                            n.AddSynapse(targetNeuronIndex + 1, 1, this, false);
                        else if (occluded)
                            n.AddSynapse(targetNeuronIndex + 2, 1, this, false);
                        else
                            n.AddSynapse(targetNeuronIndex, 1, this, false);
                    }
                    n.CurrentCharge = n.LastCharge = 1;
                }
            }
        }

        //loads an image from a file...analogous to the simView
        int fileCounter = 0;
        int countDown = 0;
        List<string> dirs = null;
        Bitmap bitmap1;
        public void LoadImage(NeuronArea na)
        {
            if (countDown == 0)
            {
                if (fileCounter == 0)
                    dirs = new List<string>(Directory.EnumerateFiles("E:\\Charlie\\Documents\\Brainsim\\Images"));
                countDown = 3;
                bitmap1 = new Bitmap(dirs[fileCounter]);
                fileCounter++;
                if (fileCounter >= dirs.Count) fileCounter = 0;
            }

            na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
            countDown--;

            for (int i = X1 + 1; i < X2 - 1; i++)
                for (int j = Y1 + 1; j < Y2 - 1; j++)
                {
                    int neuronIndex = GetNeuronIndex(i, j);
                    Neuron n = MainWindow.theNeuronArray.neuronArray[neuronIndex];
                    int x = (i - X1) * bitmap1.Width / (X2 - X1);
                    int y = (j - Y1) * bitmap1.Height / (Y2 - Y1);
                    System.Drawing.Color c = bitmap1.GetPixel(x, y);
                    if (c.R != 255 || c.G != 255 || c.B != 255)
                    {
                        n.CurrentCharge = n.LastCharge = 1 - (float)c.R / 255.0f;
                    }
                    else
                    {
                        n.CurrentCharge = n.LastCharge = 0;
                    }
                }
        }
    }
}
