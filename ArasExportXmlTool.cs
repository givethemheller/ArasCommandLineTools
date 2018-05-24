using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Aras.IOM;

namespace MasToAras
{
    class ArasExportXmlTool
    {
        /// <summary>
        /// reads a folder from the user and then loops through the directory recursively. 
        /// Each xml file is ran through recursively.
        /// any xml node that has a type="Change Controlled Item" will have that item
        /// searched upon. What ever positive search match is returned, that item id is used to replace the 
        /// item id in the xml file
        /// </summary>
        public void rFolderAffectedItemRelink() {
            using (var fbd = new FolderBrowserDialog())
            {
                // lets make the aras tool for working on this
                ArasTools theTool = new ArasTools();
                CItemTools itemTool = new CItemTools(theTool.theConnection);
                DialogResult result = fbd.ShowDialog();
                string[] files = null;

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    files = Directory.GetFiles(fbd.SelectedPath, "*.xml", SearchOption.AllDirectories);

                    System.Windows.Forms.MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                }
                //string[] fileList = Directory.GetFiles("C:\\", "*.xml", SearchOption.AllDirectories); //new DirectoryInfo("c:\\users\\tom").GetFiles(" *.xml", SearchOption.AllDirectories);

                /*Parallel.ForEach(files, (theFile) =>
                {
                    XmlDocument readDoc = new XmlDocument();
                    readDoc.Load(theFile);
                    Console.WriteLine(theFile);
                    TraverseNodes(readDoc.ChildNodes, itemTool);
                    readDoc.Save(@theFile);


                    XmlNodeList list = readDoc.SelectNodes("//Item[@id='xxxx']");
                    foreach (XmlNode n in list)
                    {

                    }
                });*/
                foreach (string fileName in files)
                {
                    XmlDocument readDoc = new XmlDocument();
                    readDoc.Load(fileName);
                    Console.WriteLine(fileName);
                    TraverseNodes(readDoc.ChildNodes, itemTool);
                    readDoc.Save(@fileName);

                }
            }
        }
        private static void TraverseNodes(  XmlNodeList nodes, CItemTools itemTool)
        {     
            foreach (XmlNode node in nodes)
            {
                // Do something with the node.
                if (node.Attributes != null && node.Attributes["type"] != null) {
                    // fix part links
                    if (node.Attributes["type"].Value == "Change Controlled Item" || node.Attributes["type"].Value == "Part") {
                        //Console.WriteLine(node.Attributes["type"].Value);
                        //Console.WriteLine(node.InnerText);
                        //Console.WriteLine(node.Attributes["keyed_name"].Value);
                        string oldId = node.InnerText;
                        string partNumber = node.Attributes["keyed_name"].Value;
                        if (partNumber == "AS-UTA")
                            partNumber = "AS-UTA-006";
                        Item arasItem =  itemTool.findItem("Part", partNumber);
                        while (arasItem == null) {
                            Console.WriteLine("Part not found - Enter a new Part Number:"+partNumber);
                            partNumber = Console.ReadLine();
                            arasItem = itemTool.findItem("Part", partNumber);  
                        }
                        if (arasItem != null)
                        {
                            //Console.WriteLine("Old Item ID" + oldId + " New ID:" + arasItem.getID() + "");
                            node.InnerText = arasItem.getID();
                            node.Attributes["keyed_name"].Value = partNumber;
                            //Console.WriteLine("New Item ID" + node.InnerText + " New ID:" + arasItem.getID() + "");
                        }
                        else {
                            
                            Console.WriteLine("Item not found on ARAS" + partNumber);
                        }
                    }                   
                }               
                TraverseNodes( node.ChildNodes, itemTool);
            }     
        }
    }
}
