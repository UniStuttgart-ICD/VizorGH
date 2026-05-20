using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Vizor._1_System;

namespace Vizor._4_Task
{
    /// <summary>  
    /// The MakeTaskSeries component generates a series of task names based on the provided inputs.  
    /// It allows customization of task execution modes, including sequential/parallel and individual/shared modes.  
    ///  
    /// **Inputs:**  
    /// - Task Name (N): A list of task names. If only one name is provided, it will be sequentially labeled based on Task Count and Start From.  
    /// - Task Count (C): The number of tasks to generate.  
    /// - Start From (F): The starting index for task numbering (default is 0).  
    /// - Parallel Mode (P): A boolean list indicating sequential (false) or parallel (true) execution (default is false).  
    /// - Shared Mode (S): A boolean list indicating individual (false) or shared (true) execution (default is false).  
    /// - Team Size (T): A list specifying the required team size for each task (default is 2).  
    ///  
    /// **Outputs:**  
    /// - Task Series (Series): A list of task names decorated with execution mode and team size information.  
    /// </summary>
    
    public class MakeTaskSeries : VizorBaseComponent
    {
        private string name;
        private int count;
        private int startFrom;
        private List<int> teamSize;
        private List<string> names;
        private List<bool> parallelMode;
        private List<bool> sharedMode;
        private List<string> series;
        /// <summary>
        /// Initializes a new instance of the MakeTaskSeries class.
        /// </summary>
        public MakeTaskSeries()
          : base("Make Task Series", "TSeries",
              "Create a task series including execution modes",
              "3_Task")
        {
            isListener = false;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Task Name", "N", 
                "task name (if one name is supplied, it will be labelled sequentially based on Task-Count and Start-From)", 
                GH_ParamAccess.list, "Sample Task");
            pManager.AddIntegerParameter("Task Count", "C", "number of tasks to generate", 
                GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Start From", "F", "optional index the start from, default is 0", 
                GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Parallel Mode", "P", 
                "optional boolean for sequential/parallel designation, default is false (sequential)", 
                GH_ParamAccess.list, false);
            pManager.AddBooleanParameter("Shared Mode", "S", 
                "optional boolean for shared/individual designation, default is false (individual)", 
                GH_ParamAccess.list, false);
            pManager.AddIntegerParameter("Team Size", "T",
                "optional specification for the required team size, default is 2",
                GH_ParamAccess.list, 2);
            //foreach (int i in new int[5] { 0, 1, 2, 3, 4 })
            //{
            //    pManager[i].Optional = true;
            //}
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Task Series", "Series", "series of task names", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            names = new List<string>();
            DA.GetDataList(0, names);
            DA.GetData(1, ref count);
            DA.GetData(2, ref startFrom);

            parallelMode = new List<bool>();
            sharedMode = new List<bool>();
            teamSize = new List<int>();
            DA.GetDataList(3, parallelMode);
            DA.GetDataList(4, sharedMode);
            DA.GetDataList(5, teamSize);

            series = new List<string>();

            if (names.Count == 1)
            {
                // if only base name is provided, automatically generate the sequence based on provided count
                name = names[0];
                for (int i = startFrom; i < startFrom + count; i++)
                {
                    series.Add(String.Format("{0} {1}", name, i));
                }
            }
            else
            {
                // if a list of names is provided, use the names directly
                series = names;
                if (!AssertInput("parallel mode", parallelMode.Count, names.Count))
                    return;
                if (!AssertInput("share mode", sharedMode.Count, names.Count))
                    return;
                if (!AssertInput("team size", teamSize.Count, names.Count))
                    return;
            }

            // decorate the name based on the execution mode
            for (int i = 0; i<series.Count; i++)
            {
                bool par = parallelMode.Count == 1 ? parallelMode[0] : parallelMode[i] ;
                bool shar = sharedMode.Count == 1 ? sharedMode[0] : sharedMode[i];
                int size = teamSize.Count == 1 ? teamSize[0] : teamSize[i];
                series[i] = String.Format("{0}{1}_{2}_{3}", par?"P":"N", shar?"S":"I", size.ToString(), series[i]);
            }

            DA.SetDataList(0, series);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Vizor.Properties.Resources.Task_Series;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d66d05ad-6746-4510-9032-ca0abce8db58"); }
        }
    }
}