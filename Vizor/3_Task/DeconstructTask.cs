using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Vizor.Properties;
using VizorLibs;

namespace Vizor._3_Task
{
    /// <summary>  
    /// Deconstruct Task Object Component  
    ///  
    /// This component is used to deconstruct a GeneralTaskObject into its individual properties and outputs them for further use.  
    ///  
    /// Inputs:  
    /// - Task Objects (Tasks): A GeneralTaskObject containing task-related data.  
    ///  
    /// Outputs:  
    /// - Task ID (ID): The unique identifier of the task.  
    /// - Task Name (TName): A human-readable description of the task.  
    /// - Target Device Name (DName): The name of the device intended to execute the task.  
    /// - Scene Meshes (Meshes): A list of meshes present in the scene content.  
    /// - Scene Texts (Texts): A list of text curves present in the scene content.  
    /// - Scene Wires (Wires): A list of wireframe polylines present in the scene content.  
    /// - Instruction Text (Instruction): A textual guide for the task.  
    /// - Robot Trajectory Frames (RFrames): A list of TCP frames for robotic tasks.  
    /// - Safety Zone Boundary (Bound): A list of meshes defining the safety zone boundary for robotic tasks.  
    /// - Associated Skill (Skill): A description of the skill associated with the task.  
    /// - Estimated Duration (Duration): The estimated duration of the task.  
    /// </summary>
    
    public class DeconstructTask : GH_Component
    {
        private GeneralTaskObject task;
        
        /// <summary>
        /// Initializes a new instance of the DeconstructTask class.
        /// </summary>
        public DeconstructTask()
          : base("Deconstruct Task Object", "Deconstruct",
              "visualise the content inside the task object", "VizorGH", "3_Task")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Task Objects", "Tasks", "task object", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Task ID", "ID", "task id", GH_ParamAccess.item);
            pManager.AddTextParameter("Task Name", "TName", "names of the task (human-readable description)", GH_ParamAccess.item);
            pManager.AddTextParameter("Target Device Name", "DName", "intended device to execute the task", GH_ParamAccess.item);
            
            pManager.AddMeshParameter("Scene Meshes", "Meshes", "meshes in the scene content (if it exists)", GH_ParamAccess.list);
            pManager.AddCurveParameter("Scene Texts", "Texts", "texts in the scene content (if it exists)", GH_ParamAccess.list);
            pManager.AddCurveParameter("Scene Wires", "Wires", "wireframes in the scene content (if it exists)", GH_ParamAccess.list);
            
            pManager.AddTextParameter("Instruction Text", "Instruction", "text to guide the task", GH_ParamAccess.item);
            pManager.AddGenericParameter("Robot Trajectory Frames", "RFrames", "TCP frames for robotic tasks", GH_ParamAccess.list);
            pManager.AddGenericParameter("Safety Zone Boundary", "Bound", "safety zone boundary mesh in robotic tasks", GH_ParamAccess.list);
            pManager.AddTextParameter("Associated Skill", "Skill","skill description", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Estimated Duration", "Duration", "estimated duration", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetData(0, ref task)) return;

            DA.SetData(0, task.id);
            DA.SetData(1, task.name);
            DA.SetData(2, task.gTarget.name);

            List<Mesh> meshes = new List<Mesh>();
            foreach (SceneGeometryObject g in task.gContentObject.geomObjects)
            {
                meshes.Add(g.gMesh);
            }
            DA.SetDataList(3, meshes);

            List<Curve> textCurves= new List<Curve>();
            Rhino.DocObjects.DimensionStyle style = new Rhino.DocObjects.DimensionStyle();
            foreach (SceneTextObject t in task.gContentObject.textObjects)
            {
                TextEntity te = new TextEntity();
                te.PlainText = t.text;
                te.Plane = t.plane;
                foreach (Curve c in te.CreateCurves(style, true))
                {
                    textCurves.Add(c);
                }
            }
            DA.SetDataList(4, textCurves);
            
            List<Polyline> polys= new List<Polyline>();
            
            foreach (SceneWireframeObject w in task.gContentObject.wireObjects)
            {
                polys.Add(new Polyline(w.points));
            }
            DA.SetDataList(5, polys);

            DA.SetData(6, task.instruction);

            if (task.gTrajectoryObject != null)
            {
                DA.SetDataList(7, task.gTrajectoryObject.gTrajectoryFrames);
            }

            List<Mesh> bounds = new List<Mesh>();
            if (task.gSafetyZoneObject != null)
            {
                foreach (SceneGeometryObject g in task.gSafetyZoneObject.boundaries)
                {
                    bounds.Add(g.gMesh);
                }
            }
            DA.SetDataList(8, bounds);

            DA.SetData(9, task.skill);
            DA.SetData(10, task.deadline);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.TaskDecon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("efec098a-72cb-44a6-a113-8bff140f6baa"); }
        }
    }
}