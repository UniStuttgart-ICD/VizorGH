using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Vizor._1_System;
using VizorLibs;

namespace Vizor._3_Machine
{
    /// <summary>  
    /// The MakeWorkArea component is responsible for creating a collection of work area objects,  
    /// each defined by a unique ID and a boundary rectangle. These work areas are oriented relative  
    /// to a specified anchor device, which serves as the origin (0,0,0) for the areas.  
    ///  
    /// Inputs:  
    /// - Device Anchor: The target device that defines the origin point for the work areas.  
    /// - Area IDs: A list of unique identifiers for each work area.  
    /// - Boundary Rectangles: A list of rectangles defining the boundaries of the work areas.  
    ///  
    /// Outputs:  
    /// - Output: A summary message indicating the number of work areas generated.  
    /// - Work Areas: A list of WorkAreaObject instances representing the created work areas.  
    /// - Mesh Areas: A list of SceneGeometryObject instances representing the mesh geometry of the work areas.  
    ///  
    /// Functionality:  
    /// - Validates input data to ensure the number of IDs matches the number of boundary rectangles.  
    /// - Creates WorkAreaObject instances for each boundary rectangle and associates them with the anchor device.  
    /// - Generates corresponding mesh geometry for visualization.  
    /// - Publishes the work area data to a ROS topic for integration with other systems.  
    ///  
    /// Notes:  
    /// - The component uses ROSMessageHandler to advertise and publish work area data.  
    /// - If the input data is invalid, an error message is displayed in the Grasshopper interface.  
    /// </summary>
    public class MakeWorkArea : GH_Component
    {
        private Device anchorDevice;
        private List<Rectangle3d> boundaries;
        private List<int> area_ids;
        private List<WorkAreaObject> areaObjects;
        private List<SceneGeometryObject> meshGeomObjects;

        /// <summary>
        /// Initializes a new instance of the MakeSafetyZone class.
        /// </summary>
        public MakeWorkArea()
          : base("Make Work Area", "Area",
              "Create a work area object with a collection of sub-areas",
              "VizorGH", "2_Content")
        {
        }

        /// <summary>
        /// Define the name, zone IDs and boundary geometries for each safety zone. 
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Device Anchor", "A", "the target device that sets the 0,0,0 point of the area (for HRC, this should be the robot obejct)", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Area IDs", "ID", "ids to associate with each work area", GH_ParamAccess.list);
            pManager.AddRectangleParameter("Boundary Rectangles", "B", "list of boundary rectangles", GH_ParamAccess.list);
        }

        /// <summary>
        /// Outputs the status message and the SafetyZoneObject
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "out", "Output summary", GH_ParamAccess.item);
            pManager.AddGenericParameter("Work Areas", "A", "Area objects", GH_ParamAccess.list);
            pManager.AddGenericParameter("Mesh Areas", "M", "Mesh objects", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetData(0, ref anchorDevice)) return;

            area_ids = new List<int>();
            boundaries = new List<Rectangle3d>();
            if (!DA.GetDataList(1, area_ids)) return;
            if (!DA.GetDataList(2, boundaries)) return;

            if (area_ids.Count != boundaries.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "length of the ID list does not match the provided boundaries");
                return;
            }

            areaObjects = new List<WorkAreaObject>();
            meshGeomObjects = new List<SceneGeometryObject>();
            VizorLibs.MessageTypes.WorkAreasMsg workAreasMsg = new VizorLibs.MessageTypes.WorkAreasMsg(boundaries.Count);

            // publish this data to the workspace tracking node
            ROSMessageHandler.Advertise(anchorDevice.wscObj, "/WorkZone/areas", "vizor_package/WorkAreas");
            for (int i = 0; i < boundaries.Count; i++)
            {
                Rectangle3d bound = boundaries[i];
                areaObjects.Add(new WorkAreaObject(anchorDevice, area_ids[i], bound));
                meshGeomObjects.Add(SceneGeometryObject.WokrAreaGeometry(areaObjects[i]));
                workAreasMsg.id[i] = i;
                workAreasMsg.size_x[i] = (float) bound.X.Length;
                workAreasMsg.size_y[i] = (float) bound.Y.Length;
                workAreasMsg.centre_x[i] = (float) bound.Center.X;
                workAreasMsg.centre_y[i] = (float) bound.Center.Y;
            }
            ROSMessageHandler.PublishWorkArea(anchorDevice.wscObj, workAreasMsg);
            this.Message = String.Format("{0} areas", areaObjects.Count);

            DA.SetData(0, String.Format("{0} work areas generated for {1}", boundaries.Count, anchorDevice.name));
            DA.SetDataList(1, areaObjects);
            DA.SetDataList(2, meshGeomObjects);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Vizor.Properties.Resources.Work_Area;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3ec4d7c4-933a-49c2-aa15-dc3a23452db6"); }
        }
    }
}