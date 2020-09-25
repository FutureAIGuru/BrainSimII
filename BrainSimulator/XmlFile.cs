using BrainSimulator.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            FileStream file = File.Open(fileName, FileMode.Open);

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
            theNeuronArray.Initialize(arraySize);
            neuronNodes = xmldoc.GetElementsByTagName("Neuron");

            for (int i = 0; i < neuronNodes.Count; i++)
            {
                XmlElement neuronNode = (XmlElement)neuronNodes[i];
                XmlNodeList idNodes = neuronNode.GetElementsByTagName("Id");
                if (idNodes.Count < 1) continue;
                int id = -1;
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
                            n.label = node.InnerText;
                            break;
                        case "Model":
                            Enum.TryParse(node.InnerText, out Neuron.modelType theModel);
                            n.model = theModel;
                            break;
                        case "LeakRate":
                            float.TryParse(node.InnerText, out float leakRate);
                            n.leakRate = leakRate;
                            break;
                        case "LastCharge":
                            float.TryParse(node.InnerText, out float lastCharge);
                            n.LastCharge = lastCharge;
                            n.currentCharge = lastCharge;
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
                                        case "IsHebbian":
                                            bool.TryParse(synapseAttribNode.InnerText, out bool isheb);
                                            s.isHebbian = isheb;

                                            break;
                                    }
                                }
                                n.AddSynapse(s.targetNeuron, s.weight, s.isHebbian);
                            }
                            break;
                    }
                }
                theNeuronArray.SetCompleteNeuron(n);
            }
            return true;
        }

        public static bool Save(NeuronArray theNeuronArray, string fileName)
        {
            string tempFile = System.IO.Path.GetTempFileName();
            FileStream file = File.Create(tempFile);

            XmlSerializer writer = new XmlSerializer(typeof(NeuronArray), GetModuleTypes());
            writer.Serialize(file, theNeuronArray);
            file.Position = 0; ;

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(file);

            XmlElement root = xmldoc.DocumentElement;
            XmlNode neuronsNode = xmldoc.CreateNode("element", "Neurons", "");
            root.AppendChild(neuronsNode);

            for (int i = 0; i < theNeuronArray.arraySize; i++)
            {
                Neuron n = theNeuronArray.GetCompleteNeuron(i);
                if (n.inUse)
                {
                    string label = theNeuronArray.GetNeuronLabel(n.id);
                    //this is needed bacause inUse is true if any synapse points to this neuron--we don't need to bother with that if it's the only thing 
                    if (n.synapses.Count != 0 || label != "" || n.lastCharge != 0 || n.leakRate != 0.1f
                        || n.model != Neuron.modelType.Std)
                    {
                        XmlNode neuronNode = xmldoc.CreateNode("element", "Neuron", "");
                        neuronsNode.AppendChild(neuronNode);

                        XmlNode attrNode = xmldoc.CreateNode("element", "Id", "");
                        attrNode.InnerText = n.id.ToString();
                        neuronNode.AppendChild(attrNode);

                        if (n.lastCharge != 0)
                        {
                            attrNode = xmldoc.CreateNode("element", "LastCharge", "");
                            attrNode.InnerText = n.lastCharge.ToString();
                            neuronNode.AppendChild(attrNode);
                        }
                        if (n.leakRate != 0.1f)
                        {
                            attrNode = xmldoc.CreateNode("element", "LeakRate", "");
                            attrNode.InnerText = n.leakRate.ToString();
                            neuronNode.AppendChild(attrNode);
                        }
                        if (n.model != Neuron.modelType.Std)
                        {
                            attrNode = xmldoc.CreateNode("element", "Model", "");
                            attrNode.InnerText = n.model.ToString();
                            neuronNode.AppendChild(attrNode);
                        }
                        if (label != "")
                        {
                            attrNode = xmldoc.CreateNode("element", "Label", "");
                            attrNode.InnerText = label;
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

                                if (s.isHebbian)
                                {
                                    attrNode = xmldoc.CreateNode("element", "IsHebbian", "");
                                    attrNode.InnerText = "true";
                                    synapseNode.AppendChild(attrNode);
                                }
                            }
                        }
                    }
                }
            }
            file.Position = 0;
            xmldoc.Save(file);
            file.Close();
            File.Copy(tempFile, fileName, true);
            File.Delete(tempFile);

            return true;
        }

    }
}
