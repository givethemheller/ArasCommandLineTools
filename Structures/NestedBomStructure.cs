using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasToAras.Structures
{
    class NestedBomStructure
    {
        public string amPartNumber { get; set; }
        public string amDescription { get; set; }
        public string componentPartNumber { get; set; }
        private int _indentLevel { get; set; }
        public string indentLevel
        {

            get { return _indentLevel.ToString(); }

            set
            {
                if (value != null)
                {
                    int check = 0;
                    Int32.TryParse(value, out check);
                    _indentLevel = Math.Abs(check);
                    //_indentLevel = check;
                }
            }
        }
        public string quantity { get; set; }
        public string componentDescription { get; set; }
        public string comment { get; set; }
        public override string ToString()
        {
            return (" Component PN:" + componentPartNumber.ToString() + " IndentLevel:" + _indentLevel.ToString());
        }
        public int getIndentLevel()
        {
            return _indentLevel;
        }

        public List<NestedBomStructure> convertCsvToList(Stream theStream) {
            var incomingStream = new StreamReader(theStream);
            CsvReader theReader = new CsvReader(incomingStream);
            var records = theReader.GetRecords<NestedBomStructure>();
            int counter = 0;
            List<NestedBomStructure> recordList = new List<NestedBomStructure>();
            foreach (NestedBomStructure aRecord in records)
            {
                counter++;
                recordList.Add(aRecord);
            }
            Console.WriteLine("Data imported.");
            return recordList;
        }
    }
}
