using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace CsEngineTest
{
    public class XmlFile
    {
        //these are aliased for XML purposes so that old xml file format will still work with new object contents
        [XmlType(TypeName = "Neuron")]
        public class N
        {
            public int Id;
            public float LastCharge = 0;
            public float LastChargeInt;
            public float CurrentCharge;
            public float CurrentChargeInt;
            public float LeakRate = 0.1f;
            public long LastFired;
            public bool KeepHistory;
            public modelType Model = modelType.Std;
            public bool inUse = false;
            public List<S> Synapses;
            public List<S> SynapsesFrom;
            public string Label = "";
        };
        [XmlType(TypeName = "Synapse")]
        public class S
        {
            public bool IsHebbian = false;
            public int TargetNeuron;
            public float Weight = 1.0f;
        }
        [XmlType(TypeName = "NeuronArray")]
        public class NA
        {
            public string networkNotes;
            public bool hideNotes;
            public bool ShowSynapese;
            public long Generation;
            public int EngineSpeed;
            public int arraySize;
            public int rows;
        }
        static NA aNeuronArray;
        public static bool Load(ref NeuronHandler theNeuronArray, string fileName)
        {

            FileStream file = File.Open(fileName, FileMode.Open);

            XmlSerializer reader1 = new XmlSerializer(typeof(NA));//, GetModuleTypes());
            aNeuronArray = (NA)reader1.Deserialize(file);
            file.Close();

            XmlDocument xmldoc = new XmlDocument();
            XmlNodeList neuronNodes;
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            xmldoc.Load(fs);
            fs.Close();

            int arraySize = aNeuronArray.arraySize;

            theNeuronArray = new NeuronHandler();
            theNeuronArray.Initialize(arraySize);
            NeuronPartial n1 = theNeuronArray.GetPartialNeuron(1);

            neuronNodes = xmldoc.GetElementsByTagName("Neuron");

            for (int i = 0; i < neuronNodes.Count; i++)
            {
                //this is the hard way...we could parse by hand
                XmlDocument tempDoc = new XmlDocument();

                XmlElement neuronNode = (XmlElement)neuronNodes[i];
                XmlNode importNode = tempDoc.ImportNode(neuronNode, true);
                tempDoc.AppendChild(importNode);

                MemoryStream stream = new MemoryStream();
                tempDoc.Save(stream);
                stream.Flush();
                stream.Position = 0;

                XmlSerializer reader = new XmlSerializer(typeof(N));
                //reader.UnknownAttribute += Reader_UnknownAttribute;
                //reader.UnknownElement += Reader_UnknownElement;
                //reader.UnknownNode += Reader_UnknownNode;
                //reader.UnreferencedObject += Reader_UnreferencedObject;
                N n = (N)reader.Deserialize(stream);
                if (n != null)
                {
                    int id = n.Id;
                    theNeuronArray.SetNeuronCurrentCharge(id, n.CurrentCharge);
                    if (n.Label != "")
                        theNeuronArray.SetNeuronLabel(id, n.Label);
                    if (n.LeakRate != 0.1f)
                        theNeuronArray.SetNeuronLeakRate(id, n.LeakRate);
                    if (n.Model != modelType.Std)
                        theNeuronArray.SetNeuronModel(id, (int)n.Model);
                    foreach (S s in n.Synapses)
                    {
                        int model = 0;
                        if (s.IsHebbian) model = 1;
                        theNeuronArray.AddSynapse(id, s.TargetNeuron, s.Weight, model, false);
                    }
                }

                //stream.Position = 0;
                //StreamReader sr = new StreamReader(stream);
                //string x = sr.ReadToEnd();
            }
            return true;
        }

        private static void Reader_UnreferencedObject(object sender, UnreferencedObjectEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void Reader_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void Reader_UnknownElement(object sender, XmlElementEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void Reader_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static bool Save(ref NeuronHandler theNeuronArray, string fileName)
        {
            string tempFile = System.IO.Path.GetTempFileName();
            FileStream file = File.Create(tempFile);

            XmlSerializer writer = new XmlSerializer(typeof(NA));//, GetModuleTypes());
            writer.Serialize(file, aNeuronArray);
            file.Position = 0; ;

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(file);

            XmlElement root = xmldoc.DocumentElement;
            XmlNode neuronsNode = xmldoc.CreateNode("element", "Neurons", "");
            root.AppendChild(neuronsNode);

            for (int i = 0; i < theNeuronArray.GetArraySize(); i++)
            {
                NeuronPartial n = theNeuronArray.GetPartialNeuron(i);
                if (n.inUse && n.id != 0)
                {
                    string label = theNeuronArray.GetNeuronLabel(n.id);
                    List<Synapse> theSynapses = theNeuronArray.GetSynapsesList(i);
                    //this is needed bacause inUse is true if any synapse points to this neuron--we don't need to bother with 
                    if (theSynapses.Count != 0 || label != "" || n.lastCharge != 0 || n.leakRate != 0.1f
                        || n.model != modelType.Std)

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
                        if (n.model != modelType.Std)
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
                        if (theSynapses.Count > 0)
                        {
                            XmlNode synapsesNode = xmldoc.CreateNode("element", "Synapses", "");
                            neuronNode.AppendChild(synapsesNode);
                            foreach (Synapse s in theSynapses)
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
                                attrNode.InnerText = s.target.ToString();
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
