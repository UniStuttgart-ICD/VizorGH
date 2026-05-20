using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Vizor._1_System;
using VizorLibs;
using VizorLibs.MessageTypes;

namespace Vizor._3_Machine
{
    /// <summary>
    /// <summary>  
    /// The PlanningSceneModel component is responsible for managing collision objects in a planning scene.  
    /// It allows adding or removing geometries to/from the scene and communicates with a robot device.  
    ///  
    /// Inputs:  
    /// - Robot (Generic): A registered robot object to interact with.  
    /// - Scene Geometries (Generic List): A list of geometries to be added or removed from the planning scene.  
    /// - Trigger Send (Boolean): A manual trigger to send the geometries to the planning scene.  
    /// - Trigger Remove (Boolean): A manual trigger to remove the geometries from the planning scene.  
    ///  
    /// Outputs:  
    /// - Output (Text): A status message indicating the result of the operation (e.g., device updated, geometries sent/removed).  
    ///  
    /// This component interacts with WebSocket connections to send or remove collision meshes in the planning scene.  
    /// It ensures that the robot device is updated and connected before performing operations.  
    /// </summary>
    
    public class PlanningSceneModel : VizorBaseComponent
    {
        private RobotObject robot;
        private bool manualSend;
        private bool manualRemove;
        private List<SceneGeometryObject> geoms;

        /// <summary>
        /// Initializes a new instance of the UpdateScene class.
        /// </summary>
        public PlanningSceneModel()
          : base("Planning Scene Model", "PlanningScene",
              "Add or remove collision objects in the planning scene", "4_Utilities")
        {
            isListener = false;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Robot", "Robot", "registed robot object", GH_ParamAccess.item);
            pManager.AddGenericParameter("Scene Geometries", "Geometries", "list of scene geometries", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Trigger Send", "Send", "optional manual input to trigger sending the geometry", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Trigger Remove", "Remove", "optional manual input to trigger removing the geometry", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Out", "status output", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsDocumentActive()) return;
            RobotObject _robot = new RobotObject();
            if (DA.GetData("Robot", ref _robot))
            {
                if ((_robot != this.robot) || (this.wscObj==null))
                {
                    this.robot = _robot;
                    if (UpdateDevice((Device)_robot))
                        DA.SetData(0, "device updated\nlast updated on " + DateTime.Now.ToString());
                }
            }
            else
            {
                CleanupConnection();
                DA.SetData(0, "no connection");
                return;
            }

            geoms = new List<SceneGeometryObject>();
            if (!DA.GetDataList(1, geoms)) return;
            this.Message = String.Format("{0} elements", geoms.Count);

            DA.GetData(2, ref manualSend);
            if (manualSend && this.wscObj.isConnected())
            {
                this.wscObj = this.robot.wscObj;
                // manual trigger will broadcast to all devices
                string status = "sent to: ";
                AddCollisionMeshInScene(this.robot.name);
                DA.SetData(0, status + "\nlast updated on " + DateTime.Now.ToString());
            }

            DA.GetData(3, ref manualRemove);
            if (manualRemove && this.wscObj.isConnected())
            {
                // manual trigger will broadcast to all devices
                string status = "removed from: ";
                RemoveCollisionMeshInScene(this.robot.name);
                DA.SetData(0, status + "\nlast updated on " + DateTime.Now.ToString());
            }
        }

        private bool AddCollisionMeshInScene(string targetDeviceName)
        {
            if (this.wscObj != null && !online)
            {
                for (int i = 0; i < geoms.Count; i++)
                {
                    PlanningGeometryMsg geomMsg = new PlanningGeometryMsg
                    {
                        operation = "add",
                        name = geoms[i].name,
                        mesh = MsgDataConverter.ghMeshToMsg(geoms[i].gMesh),
                    };
                    ROSMessageHandler.PublishPlanningGeometry(this.wscObj, targetDeviceName, geomMsg);
                    System.Threading.Thread.Sleep(100);
                }
                return true;
            }
            else return false;
        }

        private bool RemoveCollisionMeshInScene(string targetDeviceName)
        {
            if (this.wscObj != null && !online)
            {
                for (int i = 0; i < geoms.Count; i++)
                {
                    PlanningGeometryMsg geomMsg = new PlanningGeometryMsg
                    {
                        operation = "remove",
                        name = geoms[i].name,
                        mesh = MsgDataConverter.ghMeshToMsg(new Mesh()),
                    };
                    ROSMessageHandler.PublishPlanningGeometry(this.wscObj, targetDeviceName, geomMsg);
                    System.Threading.Thread.Sleep(100);
                }
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Vizor.Properties.Resources.PlanningScene;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("431b9c35-d484-4846-aa12-520d3fe64ae0"); }
        }
    }
}