using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aras.IOM;

namespace MasToAras.Structures
{
    class MasterStructure
    {
        public string partNumber;
        public string partName;
        public string comments;
        public int revisionLevel;
        public int nestedLevel;
        public string parentPart;
        public int parentRevision;
        public string parentPartName;
        public int? bomQuantity;
        public Item partArasItem { get; set; }
        public Item parentPartArasItem {get; set;}
        public List<MasterStructure> ProcessedBom { get; set; }

        public MasterStructure() {
            this.ProcessedBom = new List<MasterStructure>();
        }

        public override string ToString()
        {
            return ("Part Number:"+ partNumber + " Comments"+ comments + " Revision Level"+ revisionLevel + " Nested Level"+ nestedLevel + " Parenty Part"+ parentPart + " Parent Revision"+ parentRevision + " Bom Quantity"+ bomQuantity);
        }
        public List<MoldsStatus> importMoldStatus() {
            Stream myStream;
            Console.WriteLine("Select the Mold Status CSV");
            myStream = this.openCsvStream();
            List<MoldsStatus> returnList = new List<MoldsStatus>();
            using (myStream)
            {
                var incomingStream = new StreamReader(myStream);
                CsvReader theReader = new CsvReader(incomingStream);
                var records = theReader.GetRecords<MoldsStatus>();
                foreach (MoldsStatus item in records) {
                    returnList.Add(item);
                }
                return returnList;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">std or nested</param>
        public void importBom()
        {
            string input = "";
            Stream myStream;
            while (input != "0")
            {
                Console.WriteLine("Select a standard, Non Nested CSV file");
                myStream = this.openCsvStream();
                using (myStream)
                {
                    // read the csv file in

                        var incomingStream = new StreamReader(myStream);
                        CsvReader theReader = new CsvReader(incomingStream);
                        List<StdBomStructures> theBoms = new List<StdBomStructures>();
                        var records = theReader.GetRecords<StdBomStructures>();
                        this.processStdBom(records);
                        //Console.ReadLine();
                        // break the lop since we ran
                        input = "0";
                        /*if (input == "1")
                        {

                        }
                        else if (input == "2")
                        {
                            List<NestedBomStructure> theBoms = new List<NestedBomStructure>();
                            // use the object defition to generate the objects from the csv
                            var records = theReader.GetRecords<NestedBomStructure>();
                            //this.ProcessedBom = this.processNestedBom(records);
                            foreach (NestedBomStructure item in records)
                            {
                                theBoms.Add(item);
                            }
                        }*/
                }
            }
            

        }
        public List<MasterStructure> processNestedBom(IEnumerable<NestedBomStructure> theList) {
            List<MasterStructure> returnList = new List<MasterStructure>();
            foreach (NestedBomStructure thePart in theList) {
                MasterStructure theAddingObj = new MasterStructure();
                theAddingObj.parentPart = thePart.amPartNumber;
                
                returnList.Add(theAddingObj);
            }
            throw new Exception("function not complete ProcessNestedBom Line 66 oif MasterStructure");
            return null;
            //return returnList;
        }
        public List<MasterStructure> processStdBom(IEnumerable<StdBomStructures> theList) {
            List<MasterStructure> returnList = new List<MasterStructure>();
            
            foreach (StdBomStructures thePart in theList) {
                // items of type 4 are comments that hsouldnt be in the system. leave them alone
                //if (thePart.ItemType != 4) {
                    MasterStructure theAddingObj = new MasterStructure();
                    theAddingObj.parentPart = thePart.BillNo;
                    theAddingObj.parentRevision = thePart.Revision;
                    theAddingObj.partNumber = thePart.ComponentItemCode;
                    theAddingObj.parentPartName = thePart.billName;
                    //theAddingObj.revisionLevel = thePart.getComponentRevision();
                    theAddingObj.bomQuantity = thePart.QuantityPerBill;
                    theAddingObj.comments = thePart.ComponentDesc;
                    returnList.Add(theAddingObj);
                Console.WriteLine(theAddingObj);
                //}

            }
            this.ProcessedBom = returnList;
            return returnList;
        }
        public Stream openCsvStream() {
            Stream myStream = null;

            Console.WriteLine("Please select a CSV file");
            OpenFileDialog fileOpen = new OpenFileDialog();
            fileOpen.InitialDirectory = "c:\\users\\tom\\Documents";
            fileOpen.Filter = "Files| *.csv";
            fileOpen.FilterIndex = 2;
            fileOpen.RestoreDirectory = true;
            if (fileOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = fileOpen.OpenFile()) != null)
                    {
                        return myStream;
                    }
                    else
                        return null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Original error: " + ex.Message);
                    return null;
                }

            }
            else
                return null;
        }
    }
}
