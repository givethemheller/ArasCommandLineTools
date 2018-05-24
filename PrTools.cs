using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aras.IOM;
using MasToAras.Structures;


namespace MasToAras
{
    class PrTools
    {
        private Aras.IOM.Innovator theConnection;
        public PrTools(Aras.IOM.Innovator theConnection)
        {
            this.theConnection = theConnection;
        }
        
        public void clearPrWorkflow(string PartnumberSearch, string PrStringSearch)
        {
            Item thePartSearch = null;
            thePartSearch = this.theConnection.newItem("PR", "get");
            thePartSearch.loadAML("<Item type = \"PR\" action = \"get\" select = \"affected_item,created_by_id,created_by_manager,created_by_id,created_on,item_number,priority,state,title,"
                + "wc_building_too,wc_common_problems,wc_number_of_effected_parts,created_by_id,created_on,modified_by_id,modified_on,locked_by_id,major_rev,css,current_state,keyed_name\" page = \"1\" pagesize = \"500\" maxRecords = \"\" >"
                + "<title condition = \"like\" >%" + PrStringSearch + "%</title >"
                + " <affected_item>"
                + " <Item type = \"Change Controlled Item\" action = \"get\" >"
                + " <keyed_name condition = \"like\">%" + PartnumberSearch + "%</keyed_name>"
                + "</Item>"
                + "</affected_item>"
                + "</Item>");

            var results = thePartSearch.apply();
            int count = results.getItemCount();
            if (count < 1)
                throw new Exception("FindItemsList returned zero results");
            int i;
            Console.WriteLine("This many PR workflows found: "+count);
            Console.ReadKey();
            for (i = 0; i < count; i++)
            {
                Item addToList = results.getItemByIndex(i);
                //Console.WriteLine(addToList.getProperty("item_number"));
                Item theWorkflow = this.findWorkflowProcess(addToList.getProperty("item_number"));
                //Console.WriteLine(theWorkflow);
                Console.WriteLine(theWorkflow.getProperty("name"));
                List<Item> workflowItemList = this.findWorkflowItem(theWorkflow.getProperty("name"));

                foreach (Item theItem in workflowItemList) {
                    theItem.setAction("delete");
                    Console.WriteLine(theItem.apply());
                }

                // now  need to find all the linked workflowsItems                
                // thos are dead, so now we should be able to delete them
                theWorkflow.setAction("delete");
                Console.WriteLine(theWorkflow.apply());
            }
            Console.WriteLine("This Many Lines of PRS:" + count);
        }

        public Item findWorkflowProcess(string workflowProcessName) {
            //Console.WriteLine(workflowItemName);
            Item searchWorkFlowProcess = this.theConnection.newItem("Workflow Process", "get");
            string aml = "<Item type = \"Workflow Process\" action = \"get\" select = \"active_date,closed_date,description,name,process_owner,created_by_id,created_on,modified_by_id,modified_on,locked_by_id,major_rev,css,current_state,keyed_name\" page = \"1\" pagesize = \"300\" maxRecords = \"\" >"
                + "<name condition=\"eq\">" + workflowProcessName + "</name>"
                + "</Item>";
            //Console.WriteLine(aml);
            searchWorkFlowProcess.loadAML(aml);
            var results = searchWorkFlowProcess.apply();
            int count = results.getItemCount();
            if (count < 1)
                return null;
            int i;
            for (i = 0; i < count; i++)
            {
                Item addToList = results.getItemByIndex(i);
                //Console.WriteLine(addToList.getProperty("item_number"));
                return addToList;
            }
            return null;
        }
        public List<Item> findWorkflowItem(string workflowItemName) {
            Item workflowItemSearch = null;
            List<Item> returnList = new List<Item>();
            workflowItemSearch = this.theConnection.newItem("Workflow", "get");
            workflowItemSearch.loadAML("<Item type = \"Workflow\" action = \"get\" select = \"related_id,source_type,created_by_id,created_on,modified_by_id,modified_on,locked_by_id,major_rev,css,current_state,keyed_name,related_id(active_date,closed_date,description,name,process_owner,created_by_id,created_on,modified_by_id,modified_on,locked_by_id,major_rev,css,current_state,keyed_name),source_id\" page = \"1\" pagesize = \"\" maxRecords = \"\" >"
                + "<related_id >"
                + "<Item type = \"Workflow Process\" action = \"get\" >"
                + "<keyed_name condition = \"eq\" >" + workflowItemName + "</keyed_name >"
                + "</Item >"
                + "</related_id >"
                + "</Item >");
            var workflowItemResults = workflowItemSearch.apply();
            int workFlowItemCount = workflowItemResults.getItemCount();
            if (workFlowItemCount < 1)
                throw new Exception("FindItemsList returned zero results");
            for (int j = 0; j < workFlowItemCount; j++)
            {
                Item theItem = workflowItemResults.getItemByIndex(j);
                returnList.Add(theItem);
            }
            return (returnList);
        }
    }
}
