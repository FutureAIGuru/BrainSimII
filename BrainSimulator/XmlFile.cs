using BrainSimulator.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace BrainSimulator
{
    public class XmlFile
    {
        //this is the set of moduletypes that the xml serializer will save
        static private Type[] GetModuleTypes()
        {
            Type[] listOfBs = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                               from assemblyType in domainAssembly.GetTypes()
                               where assemblyType.IsSubclassOf(typeof(ModuleBase))
                               //                               where typeof(ModuleBase).IsAssignableFrom(assemblyType)
                               select assemblyType).ToArray();
            List<Type> list = new List<Type>();
            for (int i = 0; i < listOfBs.Length; i++)
                list.Add(listOfBs[i]);
            list.Add(typeof(PointPlus));
            list.Add(typeof(DisplayParams));
            return list.ToArray();
        }

        public static bool Load(ref NeuronArray theNeuronArray, string fileName)
        {
            FileStream file;
            try
            {
                file = File.Open(fileName, FileMode.Open,FileAccess.Read);
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not open file because: " + e.Message);
                return false;
            }

            MainWindow.thisWindow.SetProgress(0, "Loading Network File");
            theNeuronArray = new NeuronArray();

            XmlSerializer reader1 = new XmlSerializer(typeof(NeuronArray), GetModuleTypes());
            theNeuronArray = (NeuronArray)reader1.Deserialize(file);
            file.Close();

            //the above automatically loads the content of the neuronArray object but can't load the neurons themselves
            //because of formatting changes
            XmlDocument xmldoc = new XmlDocument();
            XmlNodeList neuronNodes;
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            xmldoc.Load(fs);
            fs.Close();

            int arraySize = theNeuronArray.arraySize;
            theNeuronArray.Initialize(arraySize, theNeuronArray.rows);
            neuronNodes = xmldoc.GetElementsByTagName("Neuron");

            for (int i = 0; i < neuronNodes.Count; i++)
            {
                var progress = i / (float)neuronNodes.Count;
                progress *= 100;
                if (MainWindow.thisWindow.SetProgress(progress, ""))
                {
                    MainWindow.thisWindow.SetProgress(100, "");
                    return false;
                }

                XmlElement neuronNode = (XmlElement)neuronNodes[i];
                XmlNodeList idNodes = neuronNode.GetElementsByTagName("Id");
                int id = i; //this is a hack to read files where all neurons were included but no Id's
                if (idNodes.Count > 0)
                    int.TryParse(idNodes[0].InnerText, out id);
                if (id == -1) continue;

                Neuron n = theNeuronArray.GetNeuron(id);
                n.Owner = theNeuronArray;
                n.id = id;

                foreach (XmlElement node in neuronNode.ChildNodes)
                {
                    string name = node.Name;
                    switch (name)
                    {
                        case "Label":
                            n.Label = node.InnerText;
                            break;
                        case "Model":
                            Enum.TryParse(node.InnerText, out Neuron.modelType theModel);
                            n.model = theModel;
                            break;
                        case "LeakRate":
                            float.TryParse(node.InnerText, out float leakRate);
                            n.leakRate = leakRate;
                            break;
                        case "AxonDelay":
                            int.TryParse(node.InnerText, out int axonDelay);
                            n.axonDelay = axonDelay;
                            break;
                        case "LastCharge":
                            if (n.model != Neuron.modelType.Color)
                            {
                                float.TryParse(node.InnerText, out float lastCharge);
                                n.LastCharge = lastCharge;
                                n.currentCharge = lastCharge;
                            }
                            else //is color
                            {
                                int.TryParse(node.InnerText, out int lastChargeInt);
                                n.LastChargeInt = lastChargeInt;
                                n.currentCharge = lastChargeInt; //current charge is not used on color neurons
                            }
                            break;
                        case "ShowSynapses":
                            bool.TryParse(node.InnerText, out bool showSynapses);
                            if (showSynapses)
                                MainWindow.arrayView.AddShowSynapses(n.id);
                            break;
                        case "Synapses":
                            theNeuronArray.SetCompleteNeuron(n);
                            XmlNodeList synapseNodess = node.GetElementsByTagName("Synapse");
                            foreach (XmlNode synapseNode in synapseNodess)
                            {
                                Synapse s = new Synapse();
                                foreach (XmlNode synapseAttribNode in synapseNode.ChildNodes)
                                {
                                    string name1 = synapseAttribNode.Name;
                                    switch (name1)
                                    {
                                        case "TargetNeuron":
                                            int.TryParse(synapseAttribNode.InnerText, out int target);
                                            s.targetNeuron = target;
                                            break;
                                        case "Weight":
                                            float.TryParse(synapseAttribNode.InnerText, out float weight);
                                            s.weight = weight;
                                            break;
                                        case "IsHebbian": //Obsolete: backwards compatibility
                                            bool.TryParse(synapseAttribNode.InnerText, out bool isheb);
                                            if (isheb) s.model = Synapse.modelType.Hebbian1;
                                            else s.model = Synapse.modelType.Fixed;
                                            break;
                                        case "Model":
                                            Enum.TryParse(synapseAttribNode.InnerText, out Synapse.modelType model);
                                            s.model = model;
                                            break;
                                    }
                                }
                                n.AddSynapse(s.targetNeuron, s.weight, s.model);
                            }
                            break;
                    }
                }
                theNeuronArray.SetCompleteNeuron(n);
            }
            MainWindow.thisWindow.SetProgress(100, "");
            return true;
        }

        public static bool CanWriteTo(string fileName)
        {
            return CanWriteTo(fileName, out _);
        }
        public static bool CanWriteTo(string fileName,out string message)
        {
            FileStream file1;
            message = "";
            if (File.Exists(fileName))
            {
                try
                {
                    file1 = File.Open(fileName, FileMode.Open);
                    file1.Close();
                    return true;
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return false;
                }
            }
            return true;

        }
        public static bool Save(NeuronArray theNeuronArray, string fileName)
        {
            //Check for file access
            if (!CanWriteTo(fileName,out string message))
            {
                MessageBox.Show("Could not save file because: " + message);
                return false;
            }

            MainWindow.thisWindow.SetProgress(0, "Saving Network File");

            string tempFile = System.IO.Path.GetTempFileName();
            FileStream file = File.Create(tempFile);

            Type[] extraTypes = GetModuleTypes();
            try
            {
                XmlSerializer writer = new XmlSerializer(typeof(NeuronArray), extraTypes);
                writer.Serialize(file, theNeuronArray);
            }
            catch (Exception e)
            {
                MessageBox.Show("Xml file write failed because: " + e.Message);
                return false;
            }
            file.Position = 0; ;

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(file);

            XmlElement root = xmldoc.DocumentElement;
            XmlNode neuronsNode = xmldoc.CreateNode("element", "Neurons", "");
            root.AppendChild(neuronsNode);

            for (int i = 0; i < theNeuronArray.arraySize; i++)
            {
                var progress = i / (float)theNeuronArray.arraySize;
                progress *= 100;
                if (MainWindow.thisWindow.SetProgress(progress, ""))
                {
                    MainWindow.thisWindow.SetProgress(100, "");
                    return false;
                }
                Neuron n = theNeuronArray.GetNeuronForDrawing(i);
                if (n.inUse || n.Label != "")
                {
                    n = theNeuronArray.GetCompleteNeuron(i);
                    string label = n.Label;
                    //this is needed bacause inUse is true if any synapse points to this neuron--we don't need to bother with that if it's the only thing 
                    if (n.synapses.Count != 0 || label != "" || n.lastCharge != 0 || n.leakRate != 0.1f
                        || n.model != Neuron.modelType.IF)
                    {
                        XmlNode neuronNode = xmldoc.CreateNode("element", "Neuron", "");
                        neuronsNode.AppendChild(neuronNode);

                        XmlNode attrNode = xmldoc.CreateNode("element", "Id", "");
                        attrNode.InnerText = n.id.ToString();
                        neuronNode.AppendChild(attrNode);

                        if (n.model != Neuron.modelType.IF)
                        {
                            attrNode = xmldoc.CreateNode("element", "Model", "");
                            attrNode.InnerText = n.model.ToString();
                            neuronNode.AppendChild(attrNode);
                        }
                        if (n.model != Neuron.modelType.Color && n.lastCharge != 0)
                        {
                            attrNode = xmldoc.CreateNode("element", "LastCharge", "");
                            attrNode.InnerText = n.lastCharge.ToString();
                            neuronNode.AppendChild(attrNode);
                        }
                        if (n.model == Neuron.modelType.Color && n.LastChargeInt != 0)
                        {
                            attrNode = xmldoc.CreateNode("element", "LastCharge", "");
                            attrNode.InnerText = n.LastChargeInt.ToString();
                            neuronNode.AppendChild(attrNode);
                        }
                        if (n.leakRate != 0.1f)
                        {
                            attrNode = xmldoc.CreateNode("element", "LeakRate", "");
                            attrNode.InnerText = n.leakRate.ToString();
                            neuronNode.AppendChild(attrNode);
                        }
                        if (n.axonDelay != 0)
                        {
                            attrNode = xmldoc.CreateNode("element", "AxonDelay", "");
                            attrNode.InnerText = n.axonDelay.ToString();
                            neuronNode.AppendChild(attrNode);
                        }
                        if (label != "")
                        {
                            attrNode = xmldoc.CreateNode("element", "Label", "");
                            attrNode.InnerText = label;
                            neuronNode.AppendChild(attrNode);
                        }
                        if (MainWindow.arrayView.IsShowingSnapses(n.id))
                        {
                            attrNode = xmldoc.CreateNode("element", "ShowSynapses", "");
                            attrNode.InnerText = "True";
                            neuronNode.AppendChild(attrNode);
                        }
                        if (n.synapses.Count > 0)
                        {
                            XmlNode synapsesNode = xmldoc.CreateNode("element", "Synapses", "");
                            neuronNode.AppendChild(synapsesNode);
                            foreach (Synapse s in n.synapses)
                            {
                                XmlNode synapseNode = xmldoc.CreateNode("element", "Synapse", "");
                                synapsesNode.AppendChild(synapseNode);

                                if (s.weight != 1)
                                {
                                    attrNode = xmldoc.CreateNode("element", "Weight", "");
                                    attrNode.InnerText = s.weight.ToString();
                                    synapseNode.AppendChild(attrNode);
                                }

                                attrNode = xmldoc.CreateNode("element", "TargetNeuron", "");
                                attrNode.InnerText = s.targetNeuron.ToString();
                                synapseNode.AppendChild(attrNode);

                                if (s.model != Synapse.modelType.Fixed)
                                {
                                    attrNode = xmldoc.CreateNode("element", "Model", "");
                                    attrNode.InnerText = s.model.ToString();
                                    synapseNode.AppendChild(attrNode);
                                }
                            }
                        }
                    }
                }
            }
            //a way to get the xml as a string
            //using (var stringWriter = new StringWriter())
            //using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            //{
            //    xmldoc.WriteTo(xmlTextWriter);
            //    xmlTextWriter.Flush();
            //    string xxx = stringWriter.GetStringBuilder().ToString();
            //}

            file.Position = 0;
            xmldoc.Save(file);
            file.Close();
            try
            {
                File.Copy(tempFile, fileName, true);
                File.Delete(tempFile);
            }
            catch(Exception e)
            {
                MainWindow.thisWindow.SetProgress(100, "");
                MessageBox.Show("Could not save file because: " + e.Message);
                return false;
            }
            MainWindow.thisWindow.SetProgress(100, "");

            return true;
        }

    }
}
