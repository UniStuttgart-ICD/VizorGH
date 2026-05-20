//using Grasshopper.Kernel;
//using Vizor._1_System;
//using Rhino.Geometry;
//using System;
//using System.Collections.Generic;

//namespace Vizor._2_Human
//{
//    public class MonitorWorkArea : VizorBaseComponent
//    {
//        /// <summary>
//        /// Initializes a new instance of the MyComponent1 class.
//        /// </summary>
//        public MonitorWorkArea()
//          : base("Monitor Work Area", "Monitor",
//              "configure the workspace tracker based on the work areas", "4_Utilities")
//        {
//            isListener = false;
//        }

//        /// <summary>
//        /// Registers all the input parameters for this component.
//        /// </summary>
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddBooleanParameter("Disable Update", "Disable", "disable workspace udpate, default on", GH_ParamAccess.item, false);
//            pManager.AddGenericParameter("AR Devices", "Devices", "list of registered AR devices", GH_ParamAccess.list);
//            pManager.AddTextParameter("Workspace Name", "Name", "name of the workspace", GH_ParamAccess.item);
//            pManager.AddNumberParameter("Approach Distance", "Approach", "distance from which to trigger", GH_ParamAccess.item);
//            pManager.AddTextParameter("Operation Type", "Type", "oneshot, repeated", GH_ParamAccess.item, "oneshot");
//        }

//        /// <summary>
//        /// Registers all the output parameters for this component.
//        /// </summary>
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddTextParameter("Output", "Out", "status output", GH_ParamAccess.item);
//        }

//        /// <summary>
//        /// This is the method that actually does the work.
//        /// </summary>
//        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {

//        }

//        /// <summary>
//        /// Provides an Icon for the component.
//        /// </summary>
//        protected override System.Drawing.Bitmap Icon
//        {
//            get
//            {
//                //You can add image files to your project resources and access them like this:
//                return Vizor.Properties.Resources.Monitor_Work_Area;
//            }
//        }

//        /// <summary>
//        /// Gets the unique ID for this component. Do not change this ID after release.
//        /// </summary>
//        public override Guid ComponentGuid
//        {
//            get { return new Guid("1083875e-9e6e-4574-88e0-36f75a629db2"); }
//        }
//    }
//}