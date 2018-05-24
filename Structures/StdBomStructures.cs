using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasToAras.Structures
{
    class StdBomStructures
    {
        // top level part number
        /// <summary>
        /// The overhead assembly part number
        /// </summary>
        public string BillNo { get; set; }
        // revision level of top level component
        /// <summary>
        /// The revision of the Bill No
        /// </summary>
        public int Revision { get; set; }
        /// <summary>
        /// The order that they are in the  BOM. The order is naturally sorted in order
        /// </summary>
        /*public int LineKey { get; set; }
        /// <summary>
        /// appears to be te exact same thing as line key
        /// </summary>
        public Int64 LineSeqNo { get; set; }
        /// <summary>
        /// the part number of the sub assembly
        /// </summary>
        */
        public string billName { get; set; }

        public string ComponentItemCode { get; set; }
        // the revision level of the component revision. This is often a *, so we'll need to work around that type error
        /// <summary>
        /// Hidden component revision level, configured to allow for handling bad values on CSV import
        /// </summary>
        /*private int _ComponentRevsion { get; set; }*/

        /*/// <summary>
        /// The assembly parts part number revision. Processed to handle bad data
        /// </summary>
        public string ComponentRevision
        {
            get { return _ComponentRevsion.ToString(); }
            set
            {
                string input = value;
                //Console.WriteLine(input);
                //bool passed = Int32.TryParse(value, out check);
                if (input != "*" && input != "") {
                    _ComponentRevsion = int.Parse(input);
                    //Console.WriteLine("The parsed value was:" + input + " the accepted value was" + _ComponentRevsion);
                }                    
                else
                    _ComponentRevsion = 0;
                
                
            }
        }*/
        /// <summary>
        /// defines the sub component type. a /4 means its essentially a comment from TED and not actually a part
        /// </summary>
        /*public int ItemType { get; set; }
        // these are attached to the comments that are attached to non item bom components (item type 4) and to normal items (type 1)
        // need to be added to the parts
        /// <summary>
        /// Unused - makes the import easier
        /// </summary> */
        // quantity - this can be taken in through the other structure as well
        public int? QuantityPerBill { get; set; }
        

        public string ComponentDesc { get; set; }
        /// <summary>
        /// Unused - makes the import easier
        /// </summary>
        /*public string BillType { get; set; }
        public string CommentText { get; set; }
        /// <summary>
        /// Unused - makes the import easier
        /// </summary>
        public string UnitOfMeasure { get; set; }*/

        
/*
        // some weird reference that... well, seems to be a comment. So, add it to the comments
        /// <summary>
        /// Unused - makes the import easier
        /// </summary>
        public string StandardUnitCost { get; set; }
        /// <summary>
        /// Unused - makes the import easier
        /// </summary>
        public string UDF_REF { get; set; } 
        
        public int getComponentRevision()
        {
            return this._ComponentRevsion;
        }*/
        public override string ToString()
        {
            // send back some pretty text
            //return "BillNo:" + BillNo + " Revision:" + Revision + " LineKey:" + LineKey + " LineSeq:" + LineSeqNo + " ComponentCode" + ComponentItemCode + " ComponentRevision:" + ComponentRevision + " Item Type:" + ItemType + " CommentText:" + CommentText + " Part Quantity:" + QuantityPerBill + " UDF_REF:" + UDF_REF;
            return "BillNo:" + BillNo + " Revision:" + Revision + " ComponentCode" + ComponentItemCode;

        }


        public List<StdBomStructures> convertCsvToList(Stream theStream)
        {
            var incomingStream = new StreamReader(theStream);
            CsvReader theReader = new CsvReader(incomingStream);
            var records = theReader.GetRecords<StdBomStructures>();
            int counter = 0;
            List<StdBomStructures> recordList = new List<StdBomStructures>();
            foreach (StdBomStructures aRecord in records)
            {
                counter++;
                recordList.Add(aRecord);
            }
            Console.WriteLine("Data imported.");
            return recordList;
        }
    }
}
