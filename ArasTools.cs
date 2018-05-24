using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aras.IOM;
using System.Timers;
using MasToAras.Structures;
using CsvHelper;


namespace MasToAras
{
    class ArasTools
    {
        bool entryFailure = false;
        public Aras.IOM.Innovator theConnection;
        HttpServerConnection conn;
        List<NestedBomStructure> NestedBomRecords;
        List<StdBomStructures> StdBomRecords;
        public ArasTools()
        {
            // unused
            this.theConnection = this.getInnovatorObject();
        }

        public Aras.IOM.Innovator getInnovatorObject(string url = "****", string db = "Production", string user = "tom", string password = "****") //cBfvyGLm3uGZxwan ChillPenguin
        {
            Console.WriteLine("URL:" + url + " DB:" + db + " User:" + user + " Continue? y or n");
            if ( Console.ReadLine().ToLower() == "y")
            {
                this.conn = IomFactory.CreateHttpServerConnection(url, db, user, password);
                Item login_result = conn.Login();
                if (login_result.isError())
                    throw new Exception("Login failed");
                Aras.IOM.Innovator inn = IomFactory.CreateInnovator(conn);
                Console.WriteLine("Seems we have a connection");
                return inn;
            }
            else
                throw new Exception("User refused to connect");

        }
        public void setNestedBomOjects(List<NestedBomStructure> incomingList) {
            this.NestedBomRecords = incomingList;
        }
        public void setStdBomObjects(List<StdBomStructures> incommingList) {
            this.StdBomRecords = incommingList;
        }
        public void NestedBomEntry(int rowLocation)
        {
            /*NestedBomStructure amAssy;
            // is it the last record
            if (rowLocation == (NestedBomRecords.Count()-1))
            {
                Console.WriteLine("end of records. Enter to quit.");
                Console.ReadKey();
                return entryFailure;
            }
            // its not the last record
            else
            {
                // what part are we looking at in the stack
                NestedBomStructure theRecord = this.NestedBomRecords[rowLocation];
                bool isAssy = this.partHasBom(NestedBomRecords, rowLocation);
                string parentPart;
                if (theRecord.getIndentLevel() == 1)
                {
                    // this is a special case where the part is at the top of the bom and the part is not defined
                    // e.g. AM-375A does nto have a line, it is only represented by the first column in the bom print out
                    //Console.WriteLine("1. root part found" + theRecord.amPartNumber+" Find root part or add it");
                    // find out if the AM is in the system
                    parentPart = theRecord.amPartNumber;
                    this.addPart(parentPart, true, null, "0", theRecord.componentDescription, theRecord.amDescription);
                    //Console.ReadKey();
                }
                else
                {
                    parentPart = this.findRootPart(this.NestedBomRecords, rowLocation);
                    //Console.WriteLine("2. Part is:" + records[rowLocation].componentPartNumber.ToString() + " Indent is:" + records[rowLocation].getIndentLevel() + " Root Part Is:" + parentPart);
                }
                this.addPart(theRecord.componentPartNumber, isAssy, parentPart, theRecord.quantity, theRecord.componentDescription, theRecord.amDescription);
                decimal completed = 0.000m;
                if (rowLocation % 1000 == 0) {
                    // resetting the connection
                    Console.WriteLine("------------------------------------------------------------------------------");
                    this.conn.Logout();
                    this.theConnection = null;
                    this.theConnection = this.getInnovatorObject(); 
                }
                completed = Decimal.Divide( rowLocation, NestedBomRecords.Count());
                Console.WriteLine("We are X% Complete"+ completed);
                return true;
            }
            //First, check if its the root part
            */
        }

        

        public string findRootPart(List<NestedBomStructure> records, int rowLocation)
        {
            int searchLocation = 1;
            int root = rowLocation;
            int parent;
            while (records[rowLocation - searchLocation].getIndentLevel() >= records[rowLocation].getIndentLevel())
            {
                searchLocation++;
            }
            parent = rowLocation - searchLocation;
            return (records[parent].componentPartNumber.ToString());
        }
        public bool partHasBom(List<NestedBomStructure> records, int rowLocation)
        {
            if (records[rowLocation + 1].getIndentLevel() > records[rowLocation].getIndentLevel())
            {
                return true;
            }
            else
                return false;

        }
        public Item addPartToBom(MasterStructure lineItem) {
            // Ted has added a revision level of 999 for the moat master configuration.
            // i dont know HTF MAS allows him to do that, but oh well... just ignore it
            // he has also added 0 quantity items that are essentially work instructions
            if (lineItem.parentRevision != 999 || lineItem.bomQuantity != 0)
            {
                CItemTools itemTool = new CItemTools(theConnection);
                
                // get the BOM item. If the rev is 999, get the most recent
                Item Child;
                string childQuant = lineItem.bomQuantity.ToString();

                Item parent = itemTool.findItem("Part", lineItem.parentPart, lineItem.parentRevision.ToString());
                if (lineItem.revisionLevel != 999)
                    Child = itemTool.findItem("Part", lineItem.partNumber, lineItem.revisionLevel.ToString());
                else
                    Child = itemTool.findItem("Part", lineItem.partNumber);
                if (parent != null && Child != null) {
                    Item findBomItem = itemTool.findBomItem(lineItem, parent, Child);

                    if (findBomItem == null)
                    {
                        var bomItem = this.theConnection.newItem("Part BOM", "add");
                        if (childQuant == "0" || childQuant == "" || childQuant ==  null)
                            childQuant = "1";
                        bomItem.setProperty("related_id", Child.getID());
                        bomItem.setProperty("quantity", childQuant);
                        parent.addRelationship(bomItem);
                        var result = parent.apply();
                        string error = result.getErrorString();
                        if (error != "")
                            throw new Exception("Exception on part add:" + error);
                        Console.WriteLine(lineItem + " BOM added to" + parent.getProperty("item_number"));
                    }
                    else
                    {

                    }
                }
                
                /*// load up the parent item from the master structure item
                Item parent = itemTool.findItem("Part", lineItem.parentPart, lineItem.parentRevision.ToString());
                // get the BOM item. If the rev is 999, get the most recent
                Item Child;
                string childQuant = lineItem.bomQuantity.ToString();
                // 999 is a key to indicate that we are looking for the most current revision. if its not 999, find the most recent rev
                if (lineItem.revisionLevel != 999)
                    Child = itemTool.findItem("Part", lineItem.partNumber, lineItem.revisionLevel.ToString());
                else
                    Child = itemTool.findItem("Part", lineItem.partNumber);

                // check to see if the BOM item is in the parent assembly
                var bomSearch = this.theConnection.newItem("Part BOM", "get");
                bomSearch.setAttribute("select", "related_id,quantity,source_id");
                bomSearch.setProperty("related_id", Child.getID());
                bomSearch.setPropertyCondition("related_id", "eq");
                bomSearch.setProperty("quantity", childQuant);
                bomSearch.setPropertyCondition("quantity", "eq");
                bomSearch.setProperty("source_id", parent.getID());
                bomSearch.setPropertyCondition("source_id", "eq");
                var results = bomSearch.apply();
                int count = results.getItemCount();
                if (count < 1)
                {


                }
                else {

                }*/
                    
                //Console.ReadLine();
                return parent;
            }
            else
                return null;
            
        }

        public void deleteAllItemTypes(string itemType) {
            Item thePartSearch = this.theConnection.newItem(itemType, "get");
            Item results = thePartSearch.apply();
            this.deleteItemsByList(results);
            return;
        }
        public void deleteItemsByList(Item theItems) {
            int count = theItems.getItemCount();
            List<Item> listit = new List<Item>();
            for (int i = 0; i < count; i++)
            {
                Item deleteMe = theItems.getItemByIndex(i);
                listit.Add(deleteMe);
                //deleteMe.setAction("delete");
                //deleteMe.apply();

            }
            Parallel.ForEach(listit, (listItem) =>
            {
                listItem.setAction("delete");
                listItem.apply();
                //Console.WriteLine("deleted");
            });
            return;
        }
        public void openAndSave(string itemType) {
            // cranks through every part, opens and saves it. Forces changes onto the part
            var bomSearch = this.theConnection.newItem(itemType.ToString(), "get");
            //bomSearch.setAttribute("select", "related_id,quantity,source_id");
            var results = bomSearch.apply();
            int count = results.getItemCount();
            if (count > 0)
            {
                for (int counter = 0; counter < count; counter++) {
                    Item foundPart = results.getItemByIndex(counter);
                    //Console.WriteLine("action" + foundPart.getProperty("item_number"));
                    foundPart.setAction("lock");
                    foundPart.apply();
                    //foundPart.setProperty("description", "");
                    foundPart.setAction("update");
                    foundPart.apply();
                    foundPart.setAction("unlock");
                    foundPart.apply();
                    if ((counter % 100)==0)
                        Console.WriteLine("Working on it:" +counter+" of:" + count);
                }

            }
        }
        public void setManager()
        {
            // cranks through every part, opens and saves it. Forces changes onto the part
            var prSearch = this.theConnection.newItem("WC PR", "get");
            //prSearch.setAttribute("select", "item_number, created_by_id");
            prSearch.setProperty("item_number", "PR-100055");
            prSearch.setPropertyCondition("item_number", "eq");
            var results = prSearch.apply();
            int count = results.getItemCount();
            if (count > 0)
            {
                for (int counter = 0; counter < count; counter++)
                {
                    Item foundPR = results.getItemByIndex(counter);
                    //Console.WriteLine("action" + foundPart.getProperty("item_number"));

                    string authorId = foundPR.getProperty("created_by_id");
                    //Item author = foundPR.getPropertyItem("created_by_id");
                    //Item AuthorSearch = this.getUserById(authorId);
                    Item findAuthor = this.theConnection.newItem("User", "get");
                    findAuthor.setAttribute("select", "*");
                    findAuthor.setProperty("id", authorId);
                    findAuthor.setPropertyCondition("id", "eq");
                    Item userResults = findAuthor.apply();
                    Item author = userResults.getItemByIndex(0);

                    Item manager = author.getPropertyItem("manager");
                    Item findManager = this.theConnection.newItem("User", "get");
                    findManager.setAttribute("select", "*");
                    findManager.setProperty("id", manager.getID());
                    findManager.setPropertyCondition("id", "eq");
                    Item findManagerResults = findManager.apply();
                    Item managerItem = findManagerResults.getItemByIndex(0);
                    Item managerIdentity = managerItem.getPropertyItem("owned_by_id");
                    foundPR.setAction("lock");
                    foundPR.apply();
                    foundPR.setPropertyItem("owned_by_id", managerIdentity);
                    foundPR.setAction("update");

                    foundPR.apply();
                    foundPR.setAction("unlock");
                    foundPR.apply();
                }

            }

        }

        public bool movePartToRevLevel(StdBomStructures thePart) {
            bool success = false;
            CItemTools itemsTool = new CItemTools(this.theConnection);
            // go find the item we are attempting to revision
            Item theArasPart = itemsTool.findItem("part", thePart.BillNo.ToString(), thePart.Revision.ToString());
            // found the revision. no need to increment.
            if (theArasPart != null)
            {
                string arasRevLevel = theArasPart.getProperty("major_rev");
                Console.WriteLine("Revision Found:" + arasRevLevel + " MAS Rev:" + thePart.Revision.ToString() + " Imported Part Data:" + thePart.ToString());
            }
            else if(thePart.Revision <= 99 && theArasPart == null) { 
                
                // find the highest revision part
                Item highestRevLevelPart = itemsTool.findItem("part",thePart.BillNo.ToString());
                if (highestRevLevelPart != null)
                {
                    Console.WriteLine("cranking revision level:" + thePart.ToString());
                    string majorRev = highestRevLevelPart.getProperty("major_rev");
                    int arasRevLevel = int.Parse(majorRev);
                    while (arasRevLevel < thePart.Revision)
                    {
                        // go ahead and revision the part, then fetcht he revision level to try and satisfy the while loop
                        this.revisionPart(highestRevLevelPart);
                        highestRevLevelPart = itemsTool.findItem("part", thePart.BillNo.ToString());
                        majorRev = highestRevLevelPart.getProperty("major_rev");
                        arasRevLevel = int.Parse(majorRev);
                        //Console.WriteLine("Wrote:"+ arasRevLevel + " Of:" + thePart.Revision);
                    }
                }
                else
                    Console.WriteLine("Missing Part or part rev in excess of Aras Limit " + thePart);

            }
            return success;
        }

        public Item revisionPart(Item thePart) {
            CItemTools itemsTool = new CItemTools(this.theConnection);
            thePart.setAction("version");
            Item returnedAction = thePart.apply();
            // the new revision is currently locked, so unlock it to release it
            Item newRevision = itemsTool.findItem("part", thePart.getProperty("item_number"));
            //newRevision.setAction("unlock");
            //newRevision.apply();
            newRevision.promote("Released", "Manual Release");
            // new revision is released, move on
            newRevision.apply();
            return thePart;
        }

        public void updateRevs(List<MasterStructure> theBoms,  bool multiThread = false) {
            // looks at the standard bom records and runs through updating all of the assemblies. 
            // make sure the list is set that your working on
            if (StdBomRecords.Count() == 0)
                throw new Exception("Standard BOM Records are not defined");
            // we need to sort out all of the unique BOM's and their revision numbers
            // e.g. we find np-242-01 and it has revs 0, 1, 3. We run through it and rev up until we get there
            //List<StdBomStructures> distinctBills = this.StdBomRecords.GroupBy(p => new { p.BillNo, p.Revision }).Select(g => g.First()).ToList();
            var longWatch = System.Diagnostics.Stopwatch.StartNew();
            List<MasterStructure> distinctBills2 = theBoms.OrderByDescending(item => item.revisionLevel).GroupBy(p => p.partNumber).Select(g => g.First()).ToList();
            int totalCount = distinctBills2.Count();
            if (multiThread == true) {
                Parallel.ForEach(distinctBills2, (bomRevision) =>
                { 
                    var watch = System.Diagnostics.Stopwatch.StartNew(); 
                    //this.movePartToRevLevel(bomRevision);
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;                   
                });
                Parallel.ForEach(this.StdBomRecords, (bomItem) =>
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    //this.updateBomRevisions(bomItem);
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                });
            }
            else {
                
                /*foreach (StdBomStructures bomRevision in distinctBills2)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    this.movePartToRevLevel(bomRevision);
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    
                }*/
            }
            longWatch.Stop();
            var longElapsedMs = longWatch.ElapsedMilliseconds;
            Console.WriteLine("Process Time:" + longElapsedMs);

        }
        /// <summary>
        /// Re-Assigns PR's to the highest level revision assembly available for part numbers
        /// </summary>
        public void pushPrsUpRevisions() {
            CItemTools itemsTool = new CItemTools(this.theConnection);
            List<Item> thePrSearch = itemsTool.findItemsList("PR");
            Parallel.ForEach(thePrSearch, (thePR) =>
            { //(Item thePR in thePrSearch){
                // get the PR and its associated parts
                // should be the ID's for the parts
                Console.WriteLine("parallel processing pr");
                string affectedPart = thePR.getProperty("affected_item");
                string rootCausePart = thePR.getProperty("root_cause_part");
                Item affectedPartItem = itemsTool.findItemById(affectedPart, "part");
                Item rootCausePartItem = itemsTool.findItemById(rootCausePart, "part");
                // lock item for changes
                thePR.setAction("lock");
                thePR.apply();
                // fix the affected item part relationship if its set
                if (affectedPartItem != null)
                {
                    // get the item number, get the part, set the part
                    string affectedPartItemNumber = affectedPartItem.getProperty("item_number");
                    // default behavior is to higest rev when rev is not set
                    Item affectedPartItemLatestRev = itemsTool.findItem("part", affectedPartItemNumber);
                    thePR.setProperty("affected_item", affectedPartItemLatestRev.getID());
                }
                // fix the root cause item part relationship if its set
                if (rootCausePartItem != null)
                {
                    // get the item number, get the part, set the part
                    string rootCausePartItemNumber = rootCausePartItem.getProperty("item_number");
                    // default behavior is to higest rev when rev is not set
                    Item rootCausePartItemLatestRev = itemsTool.findItem("part", rootCausePartItemNumber);
                    thePR.setProperty("root_cause_part", rootCausePartItemLatestRev.getID());
                }
                // apply changes, unlock
                thePR.setAction("update");
                thePR.apply();
                thePR.setAction("unlock");
                // current part is version and unlocked.
                thePR.apply();



            });
        }
        /// Depracated
        /// <summary>
        /// takes in an input row from MAS and updates the BOMs in ARAS
        /// works on the current level revision
        /// </summary>
        /// 
        /*public void updateBomRevisions(MasterStructure masterBomItem) {
            // turns out that there are parts in the newest version of thre BOM export that Ted provided me.
            // check if component rev ==0, not worth running if so
            CItemTools itemsTool = new CItemTools(this.theConnection);
            if (masterBomItem.revisionLevel != 0)
            {
                Item sourcItem = itemsTool.findItem("part", masterBomItem.parentPart);
                Item bomItemNewRev = itemsTool.findItem("part", masterBomItem.partNumber, masterBomItem.revisionLevel.ToString());
                Item bomItemCurrentRev = itemsTool.findItem("part", masterBomItem.partNumber, "0");
                Console.ReadLine();
                // description is in the masBomItem.comment
                if (sourcItem != null && bomItemCurrentRev != null && bomItemCurrentRev != null)
                {
                    Console.WriteLine(masterBomItem.ToString());
                    Console.ReadLine();
                    var bomSearch = this.theConnection.newItem("Part BOM", "get");
                    //bomSearch.setAction("lock");
                    //bomSearch.apply();
                    bomSearch.setAttribute("select", "related_id,quantity,source_id");
                    bomSearch.setProperty("related_id", bomItemCurrentRev.getID());
                    bomSearch.setPropertyCondition("related_id", "eq");
                    bomSearch.setProperty("quantity", masterBomItem.bomQuantity.ToString());
                    bomSearch.setPropertyCondition("quantity", "eq");
                    bomSearch.setProperty("source_id", sourcItem.getID());
                    bomSearch.setPropertyCondition("source_id", "eq");
                    var results = bomSearch.apply();
                    int count = results.getItemCount();
                    if (count == 1)
                    {
                        //var bomItem = this.theConnection.newItem("Part BOM", "add");
                        //if (quantity == "0")
                        //   quantity = "1";
                        //bomItem.setProperty("related_id", thePart.getID());
                        //bomItem.setProperty("quantity", quantity);
                        //parent.addRelationship(bomItem);
                        //parent.apply();
                        //System.Threading.Thread.Sleep(100);

                    }

                    // apply changes, unlock
                    bomSearch.setAction("update");
                    bomSearch.apply();
                    bomSearch.setAction("unlock");
                    // current part is version and unlocked.
                    bomSearch.apply();

                }
                else
                {

                    Console.WriteLine("Item not in current DB" + masterBomItem.ToString());
                    Console.WriteLine("Current Part Search Results:" + bomItemCurrentRev);
                    Console.ReadLine();
                }
            }
            else {
                Console.WriteLine(masterBomItem.revisionLevel);
                
            }
            
            
        }*/
        public void updateAmStatus(List<MoldsStatus> theMoldsData) {
            CItemTools itemTool = new CItemTools(this.theConnection);
            foreach (MoldsStatus statusItem in theMoldsData) {
                Item thePart = itemTool.findItem("Part", statusItem.AMNum);
                if (thePart != null)
                {
                    string action = null;
                    switch (statusItem.Status)
                    {
                        case "Obsolete":
                            action = "Obsolete";
                            break;
                        case "Prototype":
                            action = "Preliminary";
                            break;
                        case "In Design":
                            action = "Preliminary";
                            break;
                        case "Inactive":
                            action = "Superseded";
                            break;
                        case "Active":
                            action = "Released";
                            break;
                        default: action = "void";
                            break;
                    }
                    if (action != "void") {
                        Console.WriteLine("Part Updated:" + statusItem.AMNum + " State:"+statusItem.Status);
                        thePart.promote(action, "Manual Revision Move");
                        var results = thePart.apply();
                        
                    }

                }
                else {
                    Console.WriteLine("Part Not found From Molds DB:"+statusItem.AMNum);
                }
            }
            return;
        }
    }
}
