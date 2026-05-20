using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using VizorLibs;
using Vizor._1_System;

namespace Vizor._2_Content
{
    /// <summary>
    /// The PreviewContent component displays AR content in the Rhino viewport.
    /// This allows users to preview AR content before sending it to devices.
    /// 
    /// Inputs:
    ///   - Content Objects: A list of SceneContentObject, SceneGeometryObject, 
    ///     SceneWireframeObject, or SceneTextObject to preview in the viewport.
    ///   
    /// Outputs:
    ///   - Output: A summary of the content being previewed.
    ///   
    /// This component renders all sub-objects (meshes, wireframes, and texts) 
    /// when preview is enabled.
    /// </summary>
    public class PreviewContent : GH_Component
    {
        private List<SceneContentObject> contentList;
        private List<SceneGeometryObject> meshList;
        private List<SceneWireframeObject> wireList;
        private List<SceneTextObject> textList;

        /// <summary>
        /// Initializes a new instance of the PreviewContent class.
        /// </summary>
        public PreviewContent()
          : base("Preview Scene Content", "Preview",
              "Display AR content in the Rhino viewport",
              "VizorGH", "2_Content")
        {
            contentList = new List<SceneContentObject>();
            meshList = new List<SceneGeometryObject>();
            wireList = new List<SceneWireframeObject>();
            textList = new List<SceneTextObject>();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Content Objects", "Content", 
                "AR Content Objects (SceneContentObject, SceneGeometryObject, SceneWireframeObject, or SceneTextObject)", 
                GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Out", "Summary of the content being previewed", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Reset all stored objects
            contentList.Clear();
            meshList.Clear();
            wireList.Clear();
            textList.Clear();
            
            List<object> inputObjects = new List<object>();
            if (!DA.GetDataList(0, inputObjects) || inputObjects.Count == 0)
            {
                DA.SetData(0, "No content objects provided");
                this.Message = "No content";
                return;
            }

            // Process each input object
            int unsupportedCount = 0;
            foreach (object obj in inputObjects)
            {
                if (obj == null)
                {
                    unsupportedCount++;
                    continue;
                }

                // Unwrap if it's a GH_ObjectWrapper
                object unwrappedObject = obj;
                if (obj is GH_ObjectWrapper wrapper)
                {
                    unwrappedObject = wrapper.Value;
                }

                if (unwrappedObject == null)
                {
                    unsupportedCount++;
                    continue;
                }

                // Determine the type of object and add to appropriate list
                if (unwrappedObject is SceneContentObject contentObj)
                {
                    contentList.Add(contentObj);
                }
                else if (unwrappedObject is SceneGeometryObject meshObj)
                {
                    meshList.Add(meshObj);
                }
                else if (unwrappedObject is SceneWireframeObject wireObj)
                {
                    wireList.Add(wireObj);
                }
                else if (unwrappedObject is SceneTextObject textObj)
                {
                    textList.Add(textObj);
                }
                else
                {
                    unsupportedCount++;
                }
            }

            // Generate summary output
            GenerateSummary(DA, inputObjects.Count, unsupportedCount);
        }

        private void GenerateSummary(IGH_DataAccess DA, int totalCount, int unsupportedCount)
        {
            // Count total sub-objects from SceneContentObjects
            int totalMeshes = meshList.Count;
            int totalWires = wireList.Count;
            int totalTexts = textList.Count;

            foreach (SceneContentObject content in contentList)
            {
                totalMeshes += content.geomObjects != null ? content.geomObjects.Length : 0;
                totalWires += content.wireObjects != null ? content.wireObjects.Length : 0;
                totalTexts += content.textObjects != null ? content.textObjects.Length : 0;
            }

            string output = String.Format(
                "Total Objects: {0}\n" +
                "- Content Groups: {1}\n" +
                "- Standalone Meshes: {2}\n" +
                "- Standalone Wireframes: {3}\n" +
                "- Standalone Texts: {4}\n" +
                "\nTotal Rendered:\n" +
                "- Meshes: {5}\n" +
                "- Wireframes: {6}\n" +
                "- Texts: {7}",
                totalCount - unsupportedCount,
                contentList.Count,
                meshList.Count,
                wireList.Count,
                textList.Count,
                totalMeshes,
                totalWires,
                totalTexts);

            if (unsupportedCount > 0)
            {
                output += String.Format("\n\nWarning: {0} unsupported object(s) ignored", unsupportedCount);
            }

            this.Message = String.Format("{0} obj\n{1}M {2}W {3}T", 
                totalCount - unsupportedCount, totalMeshes, totalWires, totalTexts);

            DA.SetData(0, output);
        }

        /// <summary>
        /// Custom preview display for mesh objects in the Rhino viewport
        /// </summary>
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);

            // Render SceneContentObject meshes
            foreach (SceneContentObject content in contentList)
            {
                if (content.geomObjects != null)
                {
                    foreach (SceneGeometryObject geom in content.geomObjects)
                    {
                        ContentUtilities.DrawMeshShaded(args, geom);
                    }
                }
            }

            // Render standalone meshes
            foreach (SceneGeometryObject geom in meshList)
            {
                ContentUtilities.DrawMeshShaded(args, geom);
            }
        }

        /// <summary>
        /// Custom preview display for wireframes and text objects in the Rhino viewport
        /// </summary>
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            // Render SceneContentObject wireframes and texts
            foreach (SceneContentObject content in contentList)
            {
                if (content.wireObjects != null)
                {
                    foreach (SceneWireframeObject wireframe in content.wireObjects)
                    {
                        ContentUtilities.DrawWireframe(args, wireframe);
                    }
                }

                if (content.textObjects != null)
                {
                    foreach (SceneTextObject textObj in content.textObjects)
                    {
                        ContentUtilities.DrawText(args, textObj);
                    }
                }
            }

            // Render standalone wireframes
            foreach (SceneWireframeObject wireframe in wireList)
            {
                ContentUtilities.DrawWireframe(args, wireframe);
            }

            // Render standalone texts
            foreach (SceneTextObject textObj in textList)
            {
                ContentUtilities.DrawText(args, textObj);
            }
        }

        /// <summary>
        /// Override bounding box to include all sub-objects
        /// </summary>
        public override BoundingBox ClippingBox
        {
            get
            {
                BoundingBox bbox = BoundingBox.Empty;

                // Include SceneContentObject geometry
                foreach (SceneContentObject content in contentList)
                {
                    if (content.geomObjects != null)
                    {
                        foreach (SceneGeometryObject geom in content.geomObjects)
                        {
                            bbox.Union(ContentUtilities.GetMeshBoundingBox(geom));
                        }
                    }

                    if (content.wireObjects != null)
                    {
                        foreach (SceneWireframeObject wireframe in content.wireObjects)
                        {
                            bbox.Union(ContentUtilities.GetWireframeBoundingBox(wireframe));
                        }
                    }

                    if (content.textObjects != null)
                    {
                        foreach (SceneTextObject textObj in content.textObjects)
                        {
                            bbox.Union(ContentUtilities.GetTextBoundingBox(textObj));
                        }
                    }
                }

                // Include standalone objects
                foreach (SceneGeometryObject geom in meshList)
                {
                    bbox.Union(ContentUtilities.GetMeshBoundingBox(geom));
                }

                foreach (SceneWireframeObject wireframe in wireList)
                {
                    bbox.Union(ContentUtilities.GetWireframeBoundingBox(wireframe));
                }

                foreach (SceneTextObject textObj in textList)
                {
                    bbox.Union(ContentUtilities.GetTextBoundingBox(textObj));
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
                return Vizor.Properties.Resources.PreviewContent;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a7e4b9f2-3c8d-4f1a-9e2b-6d5c8f7a1b3e"); }
        }
    }
}
