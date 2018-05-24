using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Aras.IOM;
using CsvHelper;
using System.Timers;
using MasToAras.Structures;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;

namespace MasToAras
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            string userInput = "";
            //string rawAML = "<AML><Item type=\"File\" id=\"763E4E3C005E4D70B1D0763A1F75F38C\" action=\"add\"><checkedout_path /><file_type keyed_name=\"JPEG Image\" type=\"FileType\">992B142FCE6C4EF8B71417220CD7E92D</file_type><mimetype>image/jpeg</mimetype><actual_filename>C:\\Users\\tom\\Pictures\\317A thumbmail.png</actual_filename><filename>317A thumbmail.png</filename><Relationships><Item type=\"Located\" id=\"71DE550313744E029E2C61CBA44134F8\" action=\"add\"><file_version>4</file_version><related_id keyed_name=\"Default\" type=\"Vault\">67BBB9204FE84A8981ED8313049BA06C</related_id><sort_order>128</sort_order><source_id keyed_name=\"1st Floor.Names and Title.jpg\" type=\"File\">763E4E3C005E4D70B1D0763A1F75F38C</source_id></Item></Relationships></Item></AML>";
            //ArasTools theTool = new ArasTools();
            //var results = theTool.theConnection.applyAML(rawAML);
            //if (results.getErrorString() != "")
            //    Console.WriteLine("Error String:" + results.getErrorString());

            while (userInput != "0")
            {
                Console.WriteLine("1. to delete parts and BOMs. \n2:Clear Old BOM Items \n3. Open/Save Operation \n4. Test manager function \n5. relink parts in aras export \n6. Push PR's" +
                    " to current revs \n7. BOM Import Tool\n 8. Update Part States \n 9. Clear PR's on part and string search \n 0. to exit");
                userInput = Console.ReadLine();
                if (userInput == "1")
                {

                    ArasTools deleteTool = new ArasTools();
                    Console.WriteLine("Are you sure you want to delete all files? Y, N");
                    string confirm = Console.ReadLine();
                    if (confirm == "Y")
                        deleteTool.deleteAllItemTypes("Part");
                    else
                        Console.WriteLine("delete aborted");

                }

                else if (userInput == "2")
                {
                    ArasTools theArasTool = new ArasTools();
                    MasterStructure master = new MasterStructure();
                    master.importBom();
                    List<MasterStructure> theBoms = master.ProcessedBom;
                    CItemTools itemTools = new CItemTools(theArasTool.theConnection);
                    itemTools.clearOldPartBoms(theBoms, theArasTool);
                }
                else if (userInput == "3")
                {
                    ArasTools readSaveTool = new ArasTools();
                    Console.WriteLine("Open and save all files? Y, N");
                    string confirm = Console.ReadLine();
                    if (confirm == "Y" || confirm == "y")
                    {
                        Console.WriteLine("Name of the item types");
                        string itemType = Console.ReadLine();
                        readSaveTool.openAndSave(itemType);
                    }
                    else
                        Console.WriteLine("Re-Save aborted aborted");
                }
                else if (userInput == "4")
                {
                    ArasTools readSaveTool = new ArasTools();
                    Console.WriteLine("Test function? Y, N");
                    string confirm = Console.ReadLine();
                    if (confirm == "Y" || confirm == "y")
                        readSaveTool.setManager();
                    else
                        Console.WriteLine("Re-Save aborted aborted");
                }
                // standard BOM import
                else if (userInput == "5")
                {
                    ArasExportXmlTool xmlTool = new ArasExportXmlTool();
                    xmlTool.rFolderAffectedItemRelink();

                }
                else if (userInput == "6")
                {
                    ArasTools arasTool = new ArasTools();
                    arasTool.pushPrsUpRevisions();
                }

                else if (userInput == "7")
                {
                    // 1. remove all of the BOMs
                    ArasTools arasTools = new ArasTools();
                    MasterStructure master = new MasterStructure();
                    master.importBom();
                    List<MasterStructure> theBoms = master.ProcessedBom;
                    //Console.WriteLine("Deleting All BOMs & Parts");
                    System.Threading.Thread.Sleep(1000);
                    arasTools.deleteAllItemTypes("Part BOM");
                    arasTools.deleteAllItemTypes("Part");
                    // 2. add any parts in the MAS bom output that aren't currently in the db
                    // 3. push all revisions up, based upon the MAS bom output
                    // 4. Recreate the BOM's based upon the BOM Mas output
                    // 5. reassociate all of the PR's w/ the most current BOMS
                    // 6. Re-Associate all of the serials w/ the most current revisions



                    Console.WriteLine("Adding parts in records that are non exsistent");
                    System.Threading.Thread.Sleep(1000);
                    CItemTools itemTools = new CItemTools(arasTools.theConnection);
                    int total = theBoms.Count();
                    int soFar = 0;
                    Stopwatch sw = Stopwatch.StartNew();

                    // creating a list with new StdBomStructures Items. There will be nore parent part in this object
                    // the purpose of this list is to hold part numbers and revisions that have already been added to the database
                    // this will allow us to avoid a lot of queries looking for parts during the run.
                    List<MasterStructure> trackingList = new List<MasterStructure>();
                    foreach (MasterStructure bomItem in theBoms)
                    {
                        MasterStructure currentPartTracker = new MasterStructure();
                        currentPartTracker.partNumber = bomItem.partNumber;
                        currentPartTracker.revisionLevel = bomItem.revisionLevel;
                        // The "locator" array is basically a search to make sure that the component we are looking at hasn't already been 
                        // added to the ARAS database. There is a lot of overlap in a BOM structure, so this saves some query time.
                        var locator = trackingList.FindAll(m => m.partNumber == bomItem.partNumber && m.revisionLevel == bomItem.revisionLevel);
                        var parentLocator = trackingList.FindAll(m => m.partNumber == bomItem.parentPart && m.revisionLevel == bomItem.parentRevision);
                        // check and see if the part has been added to the list of things we have already entered
                        if (locator.Count() < 1 || parentLocator.Count() < 1)
                        {
                            Item addedPart = itemTools.addStandardPart(bomItem);
                            currentPartTracker.partArasItem = addedPart;
                            trackingList.Add(currentPartTracker);
                            MasterStructure addParentToTracker = new MasterStructure();
                            addParentToTracker.partNumber = bomItem.parentPart;
                            addParentToTracker.revisionLevel = bomItem.parentRevision;
                            trackingList.Add(addParentToTracker);

                            soFar++;
                            Console.WriteLine("check " + soFar);
                            int remainder = soFar % 300;
                            if (remainder == 0)
                            {
                                sw.Stop();
                                double avgRunTime = (sw.Elapsed.TotalSeconds / soFar);
                                double totalRunTime = total * avgRunTime;
                                //NonBlockingConsole.WriteLine("Start:"+ sw.Elapsed.TotalMilliseconds/1000+" Total Count"+total+" Estimated Run Time:" + totalRunTime);
                                Console.WriteLine("Estimated Run Time:" + totalRunTime);
                                sw.Start();
                            }
                        }

                    }

                    //Console.WriteLine("Continue w/ PR Push");
                    //System.Threading.Thread.Sleep(5000);
                    //theArasTool.pushPrsUpRevisions();
                    Console.WriteLine("Continue w/ BOM Push");
                    System.Threading.Thread.Sleep(1000);
                    //var errorFile = new StreamWriter(@"errorLog.csv");
                    //using (errorFile)
                    //{
                    //    var writer = new CsvWriter(errorFile);
                    Parallel.ForEach(theBoms, (bomItem) => { arasTools.addPartToBom(bomItem); });
                    /*  foreach (MasterStructure bomItem in theBoms)
                      {
                          arasTools.addPartToBom(bomItem);
                      }*/
                    //}
                }
                else if (userInput == "8") {
                    MasterStructure master = new MasterStructure();
                    List<MoldsStatus> theStatusObjects = master.importMoldStatus();
                    ArasTools theTool = new ArasTools();
                    theTool.updateAmStatus(theStatusObjects);
                }
                else if (userInput == "9")
                {
                    Console.WriteLine("P.s. your screwed if you don't lock all the PR's before doing this. ok?");
                    Console.ReadLine();
                    ArasTools theArasTool = new ArasTools();
                    Console.WriteLine("Enter a section of part number for the affected item search. Hope you know what your doing. Careful....");
                    string partNumber = Console.ReadLine();
                    Console.WriteLine("Enter a string search to delete on. e.g. \'bubbles\'.  Hope you know what your doing. Careful....");
                    string searchSTring = Console.ReadLine();
                    PrTools thePrTool = new PrTools(theArasTool.theConnection);
                    thePrTool.clearPrWorkflow(partNumber, searchSTring);
                }
            }
            
        }

    }
    public static class NonBlockingConsole
    {
        private static BlockingCollection<string> m_Queue = new BlockingCollection<string>();

        static NonBlockingConsole()
        {
            var thread = new Thread(
              () =>
              {
                  while (true) Console.WriteLine(m_Queue.Take());
              });
            thread.IsBackground = true;
            thread.Start();
        }

        public static void WriteLine(string value)
        {
            m_Queue.Add(value);
        }
    }
}




