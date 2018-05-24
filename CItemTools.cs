using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aras.IOM;
using MasToAras.Structures;

namespace MasToAras
{
    class CItemTools
    {
        Aras.IOM.Innovator theConnection;
        public CItemTools(Aras.IOM.Innovator theConnection) {
            this.theConnection = theConnection;
        }
        /// <summary>
        /// Finds the item based upon user input. optional revision level input
        /// </summary>
        /// <param name="item_number"> item property must be defined as item_number in ARAS</param>
        /// <param name="revision"></param>
        /// <returns></returns>
        public Item findItem(string itemType, string item_number, string revision = null, bool newestGeneration = true )
        {
            // accepts the part we want to enter from the list and the part it is a sub assembly of.
            Item thePartSearch = null;
            try
            {
                thePartSearch = this.theConnection.newItem(itemType.ToString(), "get");

                if (revision != null)
                {
                    thePartSearch.setAttribute("select", "item_number, id, classification, description, major_rev, name, generation");
                    thePartSearch.setProperty("major_rev", revision);
                    thePartSearch.setProperty("item_number", item_number);
                    thePartSearch.setProperty("generation", "0");
                    thePartSearch.setPropertyCondition("generation", "ge");
                    thePartSearch.setAttribute("orderBy", "generation desc");
                }
                else
                {
                    thePartSearch.setAttribute("select", "item_number, id, classification, description, major_rev, name, generation");
                    thePartSearch.setProperty("item_number", item_number);
                    thePartSearch.setAttribute("orderBy", "major_rev desc");
                }

                var results = thePartSearch.apply();
                int count = results.getItemCount();
                if (count > 1 && newestGeneration == false)
                {
                    throw new Exception("More than one part found ItemType:" + itemType + " ItemNumber:" + item_number + " Revision" + revision);
                }
                else if (count == 1 || (newestGeneration == true && count > 0))
                {
                    Item foundPart = results.getItemByIndex(0);
                    return foundPart;
                }
                else
                {
                    //Console.WriteLine("Part:"+part+" Not Found, Should be Added");
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception:" + e.ToString());
                Console.ReadKey();

                return thePartSearch;
            }

        }

        /// <summary>
        /// returns a list of all items of a specified type
        /// </summary>
        /// <param name="ItemType">Aras Item Type</param>
        /// <returns></returns>
        public List<Item> findItemsList(string ItemType)
        {
            List<Item> returnList = new List<Item>();
            Item thePartSearch = this.theConnection.newItem(ItemType.ToString(), "get");
            thePartSearch.setAttribute("select", "*");
            var results = thePartSearch.apply();
            int count = results.getItemCount();
            if (count < 1)
                throw new Exception("FindItemsList returned zero results. Item Type defintion:" + ItemType);
            int i;
            for (i = 0; i < count; i++)
            {
                Item addToList = results.getItemByIndex(i);
                returnList.Add(addToList);
            }
            return returnList;
        }
        public Item findItemById(string ID, string itemType)
        {
            Item thePartSearch = this.theConnection.newItem(itemType.ToString(), "get");

            thePartSearch.setAttribute("select", "item_number, id, classification, description, major_rev, name, generation");
            thePartSearch.setProperty("id", ID);
            var results = thePartSearch.apply();
            int count = results.getItemCount();
            if (count > 1)
            {
                throw new Exception("More than one item found by ID ID:" + ID + " ItemType" + itemType);
            }
            else if (count == 1)
            {
                Item foundPart = results.getItemByIndex(0);
                return foundPart;
            }
            else
            {
                //Console.WriteLine("Part:"+part+" Not Found, Should be Added");
                return null;
            }
        }
        public Item findBomItemsByParentItem(Item parent) {
            var bomSearch = this.theConnection.newItem("Part BOM", "get");
            bomSearch.setProperty("source_id", parent.getID());
            bomSearch.setPropertyCondition("source_id", "eq");
            var results = bomSearch.apply();
            int count = results.getItemCount();
            if (count < 1)
            {
                return null;
            }
            else
            {
                return results;
            }
        }
        public Item findBomItem(MasterStructure incommingMasPart, Item parent = null, Item child=null) {
            CItemTools itemTool = this;
            // load up the parent item from the master structure item if its not sent in
            if(parent == null)
                parent = itemTool.findItem("Part", incommingMasPart.parentPart, incommingMasPart.parentRevision.ToString());
            // get the BOM item. If the rev is 999, get the most recent
            // 999 is a key to indicate that we are looking for the most current revision. if its not 999, find the most recent rev
            if (child == null) {
                if (incommingMasPart.revisionLevel != 999)
                    child = itemTool.findItem("Part", incommingMasPart.partNumber, incommingMasPart.revisionLevel.ToString());
                else
                    child = itemTool.findItem("Part", incommingMasPart.partNumber);
            }
            if (child == null || parent == null) {
                throw new Exception();
            }
            string childQuant = incommingMasPart.bomQuantity.ToString();
            // check to see if the BOM item is in the parent assembly
            var bomSearch = this.theConnection.newItem("Part BOM", "get");
            bomSearch.setAttribute("select", "related_id,quantity,source_id");
            bomSearch.setProperty("related_id", child.getID());
            bomSearch.setPropertyCondition("related_id", "eq");
            bomSearch.setProperty("quantity", childQuant);
            bomSearch.setPropertyCondition("quantity", "eq");
            bomSearch.setProperty("source_id", parent.getID());
            bomSearch.setPropertyCondition("source_id", "eq");
            var results = bomSearch.apply();
            int count = results.getItemCount();
            if (count < 1)
            {
                return null;
            }
            else
            {
                //Console.WriteLine("Item exsists in BOM already incomming:" + incommingMasPart);
                return results.getItemByIndex(0);
            }
        }
       
        public Item getUserById(string ID)
        {
            // currently unused function

            Item userSearch = this.theConnection.newItem("User", "get");
            userSearch.setAttribute("select", "*");
            userSearch.setProperty("id", ID);
            userSearch.setPropertyCondition("id", "eq");
            Item userResults = userSearch.apply();
            int userCount = userResults.getItemCount();
            if (userCount == 1)
            {
                // we have found the user
                // get their managers id
                Item user = userResults.getItemByIndex(0);
                return user;
            }
            else
                return null;
        }
        /// <summary>
        /// adds a part to ARAS. Does not check for revisions or handle BOMS
        /// </summary>
        /// <param name="incommingPart"></param>
        /// <returns></returns>
        public Item addStandardPart(MasterStructure incommingPart) {
            //CItemTools itemTools = new CItemTools(this.theConnection);
            // lets see if the root assembly exsists and add it if not
            // if the incomming part has a parent assembly, lets make sure its in there and add it
            Item thePart = null;
            // make sure there is something there before searching
            if (incommingPart.parentPart != null) {
                thePart = this.findItem("part", incommingPart.parentPart);
            }
            // if its not there and its called out, add it
            if (thePart == null && incommingPart.parentPart != null)
            {
                thePart = this.addPart(incommingPart.parentPart, incommingPart.parentPart+" "+incommingPart.parentPartName, "", "Assembly");
                // you only get an ID back when you add a part, so go fetch the big mamma
                thePart = this.findItem("Part", incommingPart.parentPart);
                //Console.WriteLine("Added Part: " + thePart);
            }
            // there is a parent out there, its defined, its been found. Make sure its designated as an Assembly
            else if(thePart != null && thePart.getProperty("classification") != "Assembly") {
                thePart.setAction("lock");
                thePart.apply();
                //foundPart.setProperty("description", "");
                thePart.setAttribute("version", "0");
                thePart.setAction("update");
                thePart.setProperty("classification", "Assembly");
                thePart.apply();
                thePart.setAction("unlock");
                thePart.apply();
            }
            // finally, set the parent part to the correct revision
            this.rollUpRevisions(thePart, incommingPart.parentRevision);
            // parent assembly is in. lets look for the child part.
            Item mainPart = this.findItem("Part", incommingPart.partNumber);
            if (mainPart == null)
            {
                mainPart = this.addPart(incommingPart.partNumber, incommingPart.partNumber+" "+incommingPart.comments, incommingPart.comments, "Component");
            }
            else {
                // the item is there, make sure hte item doesn't contain our description
                if (mainPart.getProperty("description") != null && !mainPart.getProperty("description").Contains(incommingPart.comments))
                {
                    // its in there, but there may be some notes that haven't been added, so I'll add them.
                    Console.WriteLine(incommingPart.partNumber + " Updated Description Added:" + incommingPart.comments + " resulting in:" + mainPart.getProperty("description"));
                    mainPart.setAction("lock");
                    mainPart.apply();
                    mainPart.setAttribute("version", "0");
                    mainPart.setAction("update");
                    mainPart.setProperty("description", mainPart.getProperty("description") + " " + incommingPart.comments);
                    mainPart.apply();
                    mainPart.setAction("unlock");
                    mainPart.apply();
                    //
                }
                else {
                    //Console.WriteLine""
                }

            }
            // child parts that are boms and have revisions should get rolled up, because they are part of the list of boms
            // components do not have revisions
            return mainPart;



        }
        public Item addPart(string itemNumber, string name, string description, string type )
        {
            Console.WriteLine("Adding the Part: " + itemNumber);
            if (name.Length > 64) {
                name = name.Substring(0, 64);
            }

            Item thePart = this.theConnection.newItem("Part", "add");
            thePart.setProperty("item_number", itemNumber);
            thePart.setProperty("name", name);
            thePart.setProperty("description", description);
            thePart.setProperty("classification", type);
            var result =  thePart.apply();
            string error = result.getErrorString();
            if (error != "")
                throw new Exception("Exception on part add:"+ error);
            // Promote the item to Released
            Item promoted = this.theConnection.newItem("part", "get");
            promoted.setID(thePart.getID());
            promoted.apply();
            promoted.promote("Released", "Manual Release");
            var result1 = promoted.apply();
            string error1 = result1.getErrorString();
            if (error1 != "")
                throw new Exception("Exception on part promote:" + error);
            return promoted;
        }
        public Item rollUpRevisions(Item thePart, int TargetRev)
        {
            if (TargetRev <= 99)
            {
                Item highestRevPart = this.findItem("Part", thePart.getProperty("item_number"));
                
                string majorRev = highestRevPart.getProperty("major_rev");
                int arasRevLevel = int.Parse(majorRev);
                if(arasRevLevel < TargetRev)
                    Console.WriteLine("cranking revision level from:" + arasRevLevel + " to:" + TargetRev + "" + thePart.getProperty("item_number"));
                while (arasRevLevel < TargetRev)
                {
                    //Console.WriteLine();
                    //Console.WriteLine();
                    // go ahead and revision the part, then fetcht he revision level to try and satisfy the while loop
                    this.revisionPart(highestRevPart);
                    highestRevPart = this.findItem("part", thePart.getProperty("item_number"));
                    majorRev = highestRevPart.getProperty("major_rev");
                    arasRevLevel = int.Parse(majorRev);
                    //Console.WriteLine("Wrote:"+ arasRevLevel + " Of:" + thePart.Revision);
                }
                return highestRevPart;

            }
            else
                return null;
            
        }
        public Item revisionPart(Item thePart)
        {
            CItemTools itemsTool = new CItemTools(this.theConnection);
            thePart.setAction("version");
            var results = thePart.apply();
            string errorString = results.getErrorString();
            if (errorString != "")
                throw new Exception("Exception on part promote:" + errorString);
            // the new revision is currently locked, so unlock it to release it
            Item newRevision = itemsTool.findItem("part", thePart.getProperty("item_number"));
            newRevision.setAction("unlock");
            results = newRevision.apply();
            newRevision.promote("Released", "Manual Release");
            // new revision is released, move on
            results = newRevision.apply();
            errorString = results.getErrorString();
            return thePart;
        }
        public void clearOldPartBoms(List<MasterStructure> masParts, ArasTools theToolConnection) {
            // select the individual BOM Levels & revision levels. This will show us all of the assemblies
            var justTheAssemblies = masParts.Select(c => new { c.parentPart, c.parentRevision }).Distinct().ToList(); ;

            //List<Item> allBomItems = this.findItemsList("Part Bom");
            foreach ( var masterStructureItem in justTheAssemblies) {
                // grab each bom list in the items
                List<MasterStructure> partsInMasterAssy = masParts.FindAll(m => m.parentPart == masterStructureItem.parentPart && m.parentRevision == masterStructureItem.parentRevision);
                // Get the parent parts ARAS item and id so we can find its BOM elements
                Item arasParentPartItem = this.findItem("Part", masterStructureItem.parentPart, masterStructureItem.parentRevision.ToString());
                // get the BOM list for the assembly level
                Item bomItems = this.findBomItemsByParentItem(arasParentPartItem);
                for (int i = 0; i < bomItems.getItemCount(); i++) {
                    Item theBomItem = bomItems.getItemByIndex(i);
                    string arasChildAssemblyPartId = theBomItem.getProperty("related_id");
                    string quantity = theBomItem.getProperty("quantity");
                    //Console.WriteLine("ChildAssembly Part Related ID"+ arasChildAssemblyPartId);
                    Item childPart = this.findItemById(arasChildAssemblyPartId, "Part");
                    // lets get the child parts part number, quantity and revision number, then check if its in the parts in assy bit.
                    string partNumber = childPart.getProperty("item_number");
                    string revision = childPart.getProperty("major_rev");
                    
                    Console.WriteLine("Part Number:" + partNumber + " Major Revision:" +revision +" Bom Quantity:" +quantity);
                    List<MasterStructure> findPartInAssy = partsInMasterAssy.FindAll(n => n.partNumber == partNumber && n.revisionLevel.ToString() == revision && n.bomQuantity.ToString() == quantity);
                    if (findPartInAssy.Count() < 1)
                    {
                        Console.WriteLine("Item in ARAS BOM not exsistent in Master Structure Document");
                    }
                    else if (findPartInAssy.Count() > 1)
                    {
                        Console.WriteLine("Multiple instances of part in BOM");
                    }
                    else {
                        // do nothing
                        
                    }
                }
            }
            return;
        }
    }
}
