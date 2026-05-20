using System;
using System.ComponentModel;
using System.Xml.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Vizor._1_System;
using VizorLibs;
using VizorLibs.MessageTypes;

namespace Vizor._2_AR
{
    /// <summary>  
    /// The DeviceTracker component is responsible for tracking the transform (position and orientation)  
    /// of a specified AR device in real-time. It subscribes to device transform updates and outputs  
    /// the current position and orientation of the device.  
    ///  
    /// Inputs:  
    /// - "AR Device" (Device): The AR device to track.  
    /// - "Disable" (Boolean): A flag to enable or disable event listeners for the device.  
    ///  
    /// Outputs:  
    /// - "Output" (Text): Status messages indicating the connection and update status of the device.  
    /// - "Position" (Point): The current position of the device in 3D space.  
    /// - "Orientation Plane" (Plane): The current orientation of the device represented as a plane.  
    ///  
    /// This component listens to ROS messages for device transform updates and processes them to  
    /// provide real-time tracking information.  
    /// </summary>
    
    public class DeviceTracker : VizorBaseComponent
    {
        private Point3d position;
        private Quaternion quaternion;
        
        public DeviceTracker()
          : base("DeviceTracker", "DeviceTracker",
              "track the transform of a given device", "4_Utilities")
        {
            isListener = true;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("AR Device", "Device", "AR device to track", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Disable", "Disable", "disable event listeners", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Out", "status output", GH_ParamAccess.item);
            pManager.AddPointParameter("Position", "Pos", "position of the device", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Orientation Plane", "Ori", "orientation of the device", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsDocumentActive()) return;
            DA.GetData(1, ref this.online);

            if (!this.onMessageTriggered)
            {
                ARSystemDevice _device = new ARSystemDevice();
                if (DA.GetData(0, ref _device))
                {
                    if (UpdateDevice(_device))
                        DA.SetData(0, "device updated\nlast updated on " + DateTime.Now.ToString());
                }
                else
                {
                    if (this.device != null)
                    {
                        ROSMessageHandler.Unsubscribe(this.wscObj, this.device.name + "_DeviceTransform", "geometry_msgs/Pose");
                        CleanupConnection();
                    }
                    DA.SetData(0, "no connection");
                    return;
                }

                if (this.device != null)
                {
                    if (online)
                    {
                        ROSMessageHandler.Unsubscribe(this.wscObj, this.device.name + "_DeviceTransform", 
                            "geometry_msgs/Pose");
                    }
                    else
                    {
                        ROSMessageHandler.Subscribe(this.wscObj, this.device.name + "_DeviceTransform", 
                            "geometry_msgs/Pose");
                    }
                }
            }
            else
            {
                this.onMessageTriggered = false;

                if (this.wscObj.message != null)
                {
                    BuiltInMsg.Pose data = ROSMessageHandler.ParseDeviceTransform(device.name, wscObj.message);
                    if (data == null) return;

                    position = new Point3d(data.position.x * 1000, data.position.z * 1000, data.position.y * 1000);
                    DA.SetData(1, position);

                    quaternion = new Quaternion(data.orientation.x, data.orientation.y, data.orientation.z, data.orientation.w);
                    quaternion.GetRotation(out Plane plane);
                    DA.SetData(2, plane);
                    DA.SetData(0, "device transform received for " +device.name+ "\nlast updated on " + DateTime.Now.ToString());

                }
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
                return Vizor.Properties.Resources.DeviceTracker;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d4d5cd1d-a9f7-438a-bce6-c83ebce3f3a2"); }
        }
    }
}