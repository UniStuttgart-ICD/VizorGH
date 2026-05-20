using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Vizor._1_System;
using VizorLibs.MessageTypes;
using VizorLibs;
using System.Linq;
using Transform = Rhino.Geometry.Transform;
using Mesh = Rhino.Geometry.Mesh;
using System.ComponentModel;
using System.Xml.Linq;
using System.Security.Cryptography;
using Vizor.Properties;

namespace Vizor._2_Content
{
    
    ///<summary>  
    /// The ModelTracker component is responsible for tracking and managing mesh objects in an AR space.  
    /// It listens to pose changes sent from AR devices (e.g., HoloLens) and updates the poses of model elements accordingly.  
    /// The component provides transformed meshes as output, which can be used to reflect changes in the AR space.  
    ///  
    /// How it Works:  
    /// - Tracks and updates the orientation and translation of meshes based on live AR device data.  
    /// - Supports dynamic naming of geometry elements for better identification.  
    /// - Outputs debug information, including orientations and translations of tracked models.  
    /// - Handles live updates and resets when input data changes.  
    /// - Integrates with Grasshopper for real-time visualization and manipulation.  
    ///  
    /// Inputs:  
    /// - Online (Boolean): Enables or disables live sensor data reception.  
    /// - AR Device (Generic): The AR device manipulating the model.  
    /// - Meshes (Mesh List): List of meshes to track and update.  
    /// - Names (Text List): Names of the geometry elements (applied dynamically if only one name is provided).  
    ///  
    /// Outputs:  
    /// - Status (Text): Status of the component, including connection and update information.  
    /// - Names (Text List): Names of the updated model elements.  
    /// - DebugTransforms X (Transform List): Orientations of tracked models.  
    /// - DebugTransforms T (Transform List): Translations of tracked models.  
    /// - Transformed Mesh (Mesh List): Updated and transformed meshes.  
    ///  
    /// Usage:  
    /// Use this component to update AR scene geometry on the GH canvas.  
    /// DO NOT directly plug the output mesh into scene model until the changes are finalisd. 
    /// </summary>
    public class ModelTracker : VizorBaseComponent
    {
        Device currentDevice;
        private List<string> modelNames = new List<string>();
        private List<Transform> modelOrient = new List<Transform>();
        private List<Transform> modelTranslate = new List<Transform>();
        //private List<Transform> modelTransforms = new List<Transform>();

        private List<Mesh> _meshes;
        private List<string> _names;
        private List<Mesh> meshOutputs; // created once during reset, and updated subsequently based on transforms
        private Dictionary<string, Mesh> store;

        /// <summary>
        /// Initializes a new instance of the ModelTracker class.
        /// </summary>
        public ModelTracker()
          : base("ModelTracker", "ModelTracker",
              "Listen to changes to changed poses for model elements",
              "2_Content")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Online", "O",
                "Set this to true if you want GH to receive the live sensor data. \n" +
                "If this is false, the data will still be sent to the target devices, but GH will not provide the sensor reading. ", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("AR Device", "HoloLens", "AR device which is manipulating the model", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Scene Geometry Objects", "SGs", "list of scene geometry objects to update", GH_ParamAccess.list);
            pManager.AddMeshParameter("Meshes", "M", "list of meshes to track (same as your input to the MakeMesh component)", GH_ParamAccess.list);
            pManager.AddTextParameter("Names", "N", "list of scene mesh names (same as your input to the MakeMesh component)", GH_ParamAccess.list, "dynamic_mesh");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status", "status output", GH_ParamAccess.item);
            pManager.AddTextParameter("Names", "N", "names of the model elements that are updated", GH_ParamAccess.list);
            pManager.AddTransformParameter("DebugTransforms X", "orient", "Orientations of tracked models", GH_ParamAccess.list);
            pManager.AddTransformParameter("DebugTransforms T", "transl", "Translations of tracked models", GH_ParamAccess.list);
            //pManager.AddTransformParameter("Transforms", "transform", "Transforms of tracked models", GH_ParamAccess.list);
            pManager.AddMeshParameter("Transformed Mesh", "Mesh Out", "Transformed meshes", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsDocumentActive()) return;

            DA.GetData(0, ref this.isListener);
            _meshes = new List<Mesh>();
            _names = new List<string>();
            DA.GetDataList(2, _meshes);
            DA.GetDataList(3, _names);

            //modelTransforms.Clear();

            store = new Dictionary<string, Mesh>();
            for (int i = 0; i < _meshes.Count; i++)
            {
                if (_meshes[i] != null)
                {
                    string name = _names[0];
                    if (_names.Count == 1)
                    {
                        if (_meshes.Count == 1)
                            name = _names[0];
                        else
                            name = _names[0] + i.ToString();
                    }
                    else
                    {
                        name = _names[i];
                    } // use same name format as the scene geometry mesh object
                    store[name] = _meshes[i];
                }
            }

            if (!isListener)
            {
                meshOutputs = new List<Mesh>();
                meshOutputs = _meshes;
                modelNames.Clear();
                modelOrient.Clear();
                modelTranslate.Clear();
                this.Message = String.Format("new objects: {0}", store.Count);
            }
            else
            {
                if ((meshOutputs == null) || (_meshes.Count != meshOutputs.Count))
                {
                    this.Message = "Input changed - reset!";
                    return;
                }
                else
                {
                    this.Message = String.Format("{0} objects", store.Count);
                }
            }
            

            if (!this.onMessageTriggered)
            {
                if (DA.GetData(1, ref currentDevice)) // && currentDevice != null)
                {
                    if (UpdateDevice(currentDevice))
                        DA.SetData(0, "device updated\nlast updated on " + DateTime.Now.ToString());
                }
                else
                {
                    CleanupConnection();
                    DA.SetData(0, "no connection");
                    return;
                }
            }
            else
            {
                this.onMessageTriggered = false;

                if (this.wscObj.message != null)
                {
                    ModelMsg data = ROSMessageHandler.ParseModelMessage(currentDevice.name, wscObj.message);

                    if (data != null) // test if this will result in an empty data output? 
                    {
                        modelNames.Clear();
                        modelOrient.Clear();
                        modelTranslate.Clear();

                        for (int i = 0; i < data.names.Length; i++)
                        {
                            string name = data.names[i];
                            BuiltInMsg.Pose pose = data.poses[i];

                            //if (pose.position.x == 0 && pose.orientation.x == 0) continue;
                            /* 
                             * Convert quaternion w, y, z, x
                             * Convert position -y, x, z
                               ROS  FLU  (X = forward, Y = left, Z = up)
                               Rhino ENU (X = east / right, Y = north / forward, Z = up)
                            */
                            Point3d pos = new Point3d(-pose.position.y, pose.position.x, pose.position.z);
                            Rhino.Geometry.Quaternion q = new Rhino.Geometry.Quaternion
                                (pose.orientation.w, -pose.orientation.y, pose.orientation.x, pose.orientation.z);
                            
                            //q.Unitize();
                            //Transform xform; // = q.MatrixForm(); // incorrect
                            //q.GetRotation(out xform);
                            q.GetRotation(out Plane plane);
                            Transform xform = Transform.PlaneToPlane(Plane.WorldXY, plane);

                            Transform tform = Transform.Translation(pos - Point3d.Origin);
                            //Transform combined = xform * tform;

                            modelNames.Add(name);
                            modelOrient.Add(xform);
                            modelTranslate.Add(tform);
                            //modelTransforms.Add(combined);
                        }
                    }

                    DA.SetDataList(1, modelNames);
                    DA.SetDataList(2, modelOrient);
                    DA.SetDataList(3, modelTranslate);
                    //DA.SetDataList(4, modelTransforms);
                    DA.SetData(0, modelNames.ToString() + "updated. \n model transforms received for " + currentDevice.name + "\nlast updated on " + DateTime.Now.ToString());
                }
            }


            // apply the update to the meshes (if any)
            string output = "";
            if (modelNames.Count == 0)
            {
                meshOutputs = store.Values.ToList();
            }
            else
            {
                foreach (string name in modelNames)
                {
                    if (store.ContainsKey(name))
                    {
                        Mesh mesh = store[name].DuplicateMesh();
                        int index = modelNames.IndexOf(name);
                        int meshIndex = store.Keys.ToList().IndexOf(name);
                        mesh.Transform(modelOrient[index]);
                        mesh.Transform(modelTranslate[index]);
                        meshOutputs[meshIndex] = mesh;
                        output += name + " ";
                    }
                }
            }
            DA.SetDataList(4, meshOutputs);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.ModelTracker;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("CE1AF613-6D17-4DD3-8D2D-626E55884D7D"); }
        }
    }
}