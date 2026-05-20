//using Grasshopper.Kernel.Special;
//using Grasshopper.Kernel;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Vizor._2_Content
//{
//    public class DisplayRule : GH_Component
//    {
//        // Constructor: Defines the component's unique name, nickname, description, and category
//        public DisplayRule()
//          : base("Display Rule", "Rule", 
//                "Select a valid display rule", "VizorGH", "2_Content")
//        {
//        }

//        // Register inputs and outputs (here, an array of strings to define the list items)
//        public override void CreateAttributes()
//        {
//            base.CreateAttributes();
//        }


//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddTextParameter("Items", "I", "Array of items for the value list", GH_ParamAccess.list);
//        }

//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddTextParameter("Selected Value", "V", "The selected value from the list", GH_ParamAccess.item);
//        }

//        // The method that gets called when the component is triggered
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {
//            // Retrieve input data: the array of strings for the value list
//            List<string> itemList = new List<string>();
//            if (!DA.GetDataList(0, itemList)) return;

//            // Create the custom value list
//            GH_ValueList valueList = CustomValueLists("CustomList", "Custom Value List", 0, itemList.ToArray());

//            // You can now use the value list in your component (for instance, displaying it in a custom UI, linking it to an input, etc.)
//            // For this example, we will simply output the first value from the list.
//            string selectedValue = valueList.Items[0].Description;

//            // Set output data: the selected value from the list
//            DA.SetData(0, selectedValue);
//        }

//        // Your existing method for creating the custom value list
//        protected GH_ValueList CustomValueLists(string NickName, string Name, int InputIndex, string[] ItemArr)
//        {
//            // Create a new GH_ValueList component
//            GH_ValueList valueList = new GH_ValueList();

//            // Set the nickname and name of the ValueList
//            valueList.NickName = NickName;
//            valueList.Name = Name;

//            // Add the items from the ItemArr array to the ValueList
//            foreach (string item in ItemArr)
//            {
//                // Add each string item to the value list as a new item
//                valueList.Items.Add(new GH_ValueListItem(item, item));  // The second parameter is the value associated with the item
//            }

//            // Return the value list (or do more work if necessary)
//            return valueList;
//        }
//        public override Guid ComponentGuid => new Guid("E0C029F4-3645-43F5-957F-D579F94575C0");

//    }
//}