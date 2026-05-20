using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using VizorLibs;

namespace Vizor._3_Control
{
    /// <summary>
    /// The SafetyZoneConfig component is responsible for creating safety zone configurations based on work areas.  
    /// It takes input parameters to define the zones, processes the data, and outputs the resulting safety zone objects.  
    /// 
    /// Inputs:  
    /// - Zone Name (N): A unique name for the safety zone configuration (list of strings).  
    /// - Area IDs (I): Comma-separated area IDs to associate with the zone (list of strings).  
    /// - Work Areas (A): Work area objects to create the zone from (list of generic objects).  
    /// - Solid Mode (S): Boolean indicating whether the zone is visualized as a solid patch or boundary lines.  
    /// - Alert Distance (D): Offset distance in meters to trigger alerts when approaching boundaries (number).  
    ///  
    /// Outputs:  
    /// - Output (out): A summary string indicating the number of safety zones created.  
    /// - Zone Configurations (Zones): A list of SafetyZoneObject instances representing the created safety zones.  
    /// </summary>

    public class SafetyZoneConfig : GH_Component
    {
        private List<string> zone_ids_raw;
        private List<string> names;
        private List<WorkAreaObject> areaObjects;
        private List<SafetyZoneObject> zoneObjects;
        bool solid;
        double alert_distance;

        /// <summary>
        /// This takes a list of work area definitions and creat the safety zone configurations 
        /// Multiple work areas may comprise a single safety zone
        /// </summary>
        public SafetyZoneConfig()
          : base("Define Safety Zone", "Safety Zone",
              "Create safety zone configurations based on work areas",
              "VizorGH", "3_Task")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Zone Name", "N", "unique name of the safety zone configuration", 
                GH_ParamAccess.list, "Safety");
            pManager.AddTextParameter("Area IDs", "I", "area ids to associate with the zone, as a comma-separated list", GH_ParamAccess.list);
            pManager.AddGenericParameter("Work Areas", "A", "work areas to create the zone from", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Solid Mode", "S", "is the zone visualised as solid patch or boundary lines", 
                GH_ParamAccess.item, false);
            pManager.AddNumberParameter("Alert Distance", "D", "offset distance (in metres) to trigger alerts when user approaches the boundaries, default 1 metre", 
                GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "out", "Output summary", GH_ParamAccess.item);
            pManager.AddGenericParameter("Zone Configurations", "Zones", "Safety zone objects", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // union the zones and create boundary geometries
            names = new List<string>();
            zone_ids_raw = new List<string>();
            areaObjects = new List<WorkAreaObject>();
            DA.GetDataList(0, names);
            if (!DA.GetDataList(1, zone_ids_raw)) return;
            if (!DA.GetDataList(2, areaObjects)) return;
            DA.GetData(3, ref solid);
            DA.GetData(4, ref alert_distance);

            if (areaObjects.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "area objects is empty");
                return;
            }

            Plane zone_plane = areaObjects[0].rectangle.Plane; // assume all rectangles are on the same plane
            
            zoneObjects = new List<SafetyZoneObject>();
            for (int i = 0; i < zone_ids_raw.Count; i++)
            {
                //// set geometry rule to "session-passive"
                //SceneGeometryObject go = (boundaries.Count == 1 ? boundaries[0] : boundaries[i]);
                //go.operation = "session-passive";

                // convert raw comma-separated id string to an array of integer ids for the areas
                string[] res = zone_ids_raw[i].Split(',');
                int[] zone_ids = new int[res.Length];
                zone_ids = Array.ConvertAll(res, int.Parse);

                // union boundaries specified with the IDs
                List<Curve> curves = new List<Curve>();
                for (int j = 0 ; j < zone_ids.Length; j++)
                {
                    curves.Add(areaObjects[j].rectangle.ToNurbsCurve());
                }
                Curve[] unioned_curves = Curve.CreateBooleanUnion(curves,0.001);

                if (unioned_curves == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "could not process boundary rectangles");
                    return;
                }

                // turn boundaries into visualisation objects
                Brep[] breps = new Brep[unioned_curves.Length];

                if (solid)
                {
                    // shows the boundary as a solid patch
                    breps = Brep.CreatePlanarBreps(unioned_curves, 0.001);
                }
                else
                {
                    // shows the boundary with offset inwards 5 cm
                    float offset = VizorUtilities.RhinoUnitMultiplier() * 0.05f;
                    
                    for (int k = 0; k < unioned_curves.Length; k++)
                    {
                        Curve bound = unioned_curves[k];
                        Curve b1 = bound.Offset(zone_plane, offset, 0.001, CurveOffsetCornerStyle.None)[0];
                        Curve b2 = bound.Offset(zone_plane, -offset, 0.001, CurveOffsetCornerStyle.None)[0];
                        Curve b = b1.GetLength() < b2.GetLength() ? b1 : b2;
                        breps[k] = Brep.CreateFromLoft(new Curve[] { bound, b }, bound.PointAtStart, bound.PointAtEnd, LoftType.Straight, false)[0];
                    }
                }

                string name = names.Count != 1 ? names[i] : names[0] + i.ToString();
                zoneObjects.Add(new SafetyZoneObject
                {
                    identifier = name, zone_ids = zone_ids,
                    alert_distance = (float) alert_distance,
                    boundaries = CreateZoneCollection(breps, name),
                });
            }
            DA.SetData(0, String.Format("Created {0} safety zones", names.Count));
            DA.SetDataList(1, zoneObjects);
        }


        private SceneGeometryObject[] CreateZoneCollection(Brep[] breps, string name)
        {
            List<SceneGeometryObject> gos = new List<SceneGeometryObject>();
            int index = 0;
            foreach (Brep brep in breps)
            {
                Mesh mesh = Mesh.CreateFromBrep(brep, new MeshingParameters(0.01, 0.001))[0];
                string indexed_name = String.Format("{0}_{1}", name, index);
                gos.Add(CreateZoneGeometryObject(indexed_name, mesh, areaObjects[0].target));
                index += 1;
            }
            return gos.ToArray();
        }

        private SceneGeometryObject CreateZoneGeometryObject(string name, Mesh m, Device target)
        {
            string layer = "";
            if (target is RobotObject) 
            {
                layer = "Machines/" + target.name + "/platform_transform/g_persistent";
            }
            else if (target is ARSystemDevice) 
            {
                layer = "ARSpace/" + target.name + "/g_persistent";
            }
            else if (target is InputDevice) 
            {
                layer = "ARSpace/" + target.name + "/platform_transform/g_persistent";
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "invalid device target");
            }
            return new SceneGeometryObject
            {
                operation = "session-passive", // safety zones default to session - passive
                layer = layer,
                name = name,
                material = String.Format("{0},{1},{2},{3}", 255, 0, 0, 255), // safety zones default to red
                gMesh = m
            };
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Vizor.Properties.Resources.SafetyZone;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("85baf117-c74a-4cbb-9d8a-723572eec403"); }
        }
    }
}