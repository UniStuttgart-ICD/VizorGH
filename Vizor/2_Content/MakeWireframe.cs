using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using VizorLibs;

namespace Vizor._2_Content
{
    /// <summary>  
    /// The MakeWireframe component is responsible for creating wireframe objects for augmented reality (AR) spaces.  
    /// It takes input parameters such as target anchor devices, breps, names, colors, widths, display rules,  
    /// and optional point inputs to generate wireframe objects.  
    ///  
    /// The component processes the input data to create SceneWireframeObject instances, which define the wireframe's  
    /// properties such as points, name, layer, material, width, and operation. These wireframes are then registered  
    /// for AR visualization.  
    ///  
    /// Inputs:  
    /// - Target Anchor (Device): The device to anchor the wireframe to.  
    /// - Breps (List<Brep>): List of breps to generate the wireframes.  
    /// - Names (List<string>): Names for the wireframes (applied to all if only one is provided).  
    /// - Colors (List<Color>): Colors for the wireframes (applied to all if only one is provided).  
    /// - Widths (List<int>): Widths for the wires in mm (applied to all if only one is provided).  
    /// - Display Rules (List<string>): Rules for display behavior (e.g., persistent, session, step, flange).  
    /// - Optional Point Input (GH_Structure<GH_Point>): Points to connect for wireframe generation (breps ignored if provided).  
    ///  
    /// Outputs:  
    /// - Output (string): Summary of registered AR wireframes.  
    /// - Wireframe Objects (List<SceneWireframeObject>): The generated wireframe objects.  
    /// - Debug Polylines (List<PolylineCurve>): Polylines representing the wireframes for debugging purposes.  
    ///  
    /// This component is part of the VizorGH plugin and is categorized under "2_Content".  
    /// </summary>

    public class MakeWireframe : GH_Component
    {
        private Device anchorDevice;
        public List<SceneWireframeObject> wireframes;
        private List<Brep> breps;
        private List<Point3d> points;
        private List<string> names;
        private List<int> widths;
        private List<System.Drawing.Color> colours;
        private List<string> materials;
        private List<string> rules;
        private GH_Structure<GH_Point> rawPoints;

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MakeWireframe()
          : base("Scene Wireframe Object", "Wireframe",
              "Create a wireframe object for the AR space",
              "VizorGH", "2_Content")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Target Anchor", "T", "target device to anchor the wireframe to", GH_ParamAccess.item);
            pManager.AddBrepParameter("Breps", "B", "list of breps to generate the wireframes", GH_ParamAccess.list);

            pManager.AddTextParameter("Names", "N", "list of wireframe names (if only one is provided it will be applied to all)", 
                GH_ParamAccess.list, "wireframe");
            pManager.AddColourParameter("Colors", "C", "list of colours for the wireframe (if only one is provided it will be applied to all)", 
                GH_ParamAccess.list, System.Drawing.Color.FromArgb(255, 237, 9, 153));
            pManager.AddIntegerParameter("Widths", "W", "list of widths for each wire in mm (if only one is provided it will be applied to all)", 
                GH_ParamAccess.list, 5);
            pManager.AddTextParameter("Display Rules", "R", "list of rules, e.g., one of [persistent, session, step, flange] (if only one is provided it will be applied to all)", 
                GH_ParamAccess.list, "session");
            pManager.AddPointParameter("Optional Point Input", "P", "list of points to connect (brep will be ignored)", GH_ParamAccess.tree);
            pManager[6].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "out", "name of registered AR wireframes", GH_ParamAccess.item);
            pManager.AddGenericParameter("Wireframe Objects", "W", "Wireframe objects", GH_ParamAccess.list);
            //pManager.AddCurveParameter("Debug Polylines", "P", "Polylines representing the wireframes", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            breps = new List<Brep>();
            names = new List<string>();
            widths= new List<int>();
            colours = new List<System.Drawing.Color>();
            materials = new List<string>();
            rules = new List<string>();
            rawPoints = new GH_Structure<GH_Point>();
            if (!DA.GetData(0, ref anchorDevice)) return;
            if (!DA.GetDataList(1, breps)) return;
            if (!DA.GetDataList(2, names)) return;
            if (!DA.GetDataList(3, colours)) return;
            if (!DA.GetDataList(4, widths)) return;
            if (!DA.GetDataList(5, rules)) return;
            DA.GetDataTree(6, out rawPoints);
            foreach (System.Drawing.Color col in colours)
            {
                materials.Add(String.Format("{0},{1},{2},{3}", col.R, col.G, col.B, col.A));
            }

            wireframes = new List<SceneWireframeObject>();
            //List<PolylineCurve> polylines = new List<PolylineCurve>();
            string output = "";

            int wirePoints = 0;
            if (rawPoints.IsEmpty || rawPoints.PathCount != breps.Count)
            {
                for (int i = 0; i < breps.Count; i++)
                {
                    string rule = rules.Count == 1 ? rules[0] : rules[i];
                    Brep b = (rule == "flange") ? breps[i] : VizorUtilities.TransformVisualisation(breps[i], anchorDevice);
                    b.Edges.RemoveNakedMicroEdges(0.001);

                    points = new List<Point3d>();
                    foreach (Curve c in b.DuplicateEdgeCurves())
                    {
                        points.Add(c.PointAtStart);
                        points.Add(c.PointAtNormalizedLength(0.5));
                        points.Add(c.PointAtEnd);
                    }

                    //if (points.Count > 1)
                    //{
                    //    Polyline polyline = new Polyline(points);
                    //    polylines.Add(new PolylineCurve(polyline));
                    //}

                    string name = "Wire_" + names[0];
                    if (names.Count == 1)
                    {
                        if (breps.Count == 1)
                            name = "Wire_" + names[0];
                        else
                            name = "Wire_" + names[0] + i.ToString();
                    }
                    else
                    {
                        name = "Wire_" + names[i];
                    }

                    wireframes.Add(new SceneWireframeObject
                    {
                        points = points.ToArray(),
                        name = name,
                        layer = VizorUtilities.GetLayerFromRule(rule, anchorDevice),
                        material = materials.Count == 1 ? materials[0] : materials[i],
                        width = widths.Count == 1 ? widths[0] : widths[i],
                        operation = VizorUtilities.GetOperationFromRule(rule, anchorDevice)
                    });

                    wirePoints += points.Count / 3;
                    output += String.Format("{0}, {1} wires\n", wireframes[i].name, points.Count / 3);
                }
            }
            else
            {
                int i = 0;
                foreach (GH_Path p in rawPoints.Paths)
                {
                    string rule = rules.Count == 1 ? rules[0] : rules[i];

                    List<GH_Point> branch = rawPoints.get_Branch(p) as List<GH_Point>;
                    if (branch == null) continue;

                    points = new List<Point3d>();
                    for (int b = 0; b < branch.Count; b++)
                    {
                        points.Add((Point3d)branch[b].Value);
                    }

                    //if (points.Count > 1)
                    //{
                    //    Polyline polyline = new Polyline(points);
                    //    polylines.Add(new PolylineCurve(polyline));
                    //}

                    wireframes.Add(new SceneWireframeObject
                    {
                        points = points.ToArray(),
                        name = names.Count == 1 ? (names[0] + i.ToString()) : names[i],
                        layer = VizorUtilities.GetLayerFromRule(rule, anchorDevice),
                        material = materials.Count == 1 ? materials[0] : materials[i],
                        width = widths.Count == 1 ? widths[0] : widths[i],
                        operation = VizorUtilities.GetOperationFromRule(rule, anchorDevice)
                    });

                    wirePoints += points.Count;
                    output += String.Format("(raw point input): {0}, {1} wires\n", wireframes[i].name, points.Count);
                    i += 1;
                }
            }

            this.Message = String.Format("{0} wires ({1})", wirePoints, VizorUtilities.GetTypeSummary(rules));
            DA.SetData(0, output);
            DA.SetDataList(1, wireframes);
            //DA.SetDataList(2, polylines);

        }

        /// <summary>
        /// Custom preview display for wireframe objects in the Rhino viewport
        /// </summary>
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (wireframes == null || wireframes.Count == 0)
                return;

            // Render all wireframe objects using ContentUtilities
            foreach (SceneWireframeObject wireframe in wireframes)
            {
                ContentUtilities.DrawWireframe(args, wireframe);
            }
        }

        /// <summary>
        /// Override bounding box to include wireframe geometry
        /// </summary>
        public override BoundingBox ClippingBox
        {
            get
            {
                BoundingBox bbox = BoundingBox.Empty;

                if (wireframes != null)
                {
                    foreach (SceneWireframeObject wireframe in wireframes)
                    {
                        bbox.Union(ContentUtilities.GetWireframeBoundingBox(wireframe));
                    }
                }

                return bbox;
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Vizor.Properties.Resources.Wireframe;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7b6e87d9-0187-4cce-bca1-d7393fd01070"); }
        }
    }
}