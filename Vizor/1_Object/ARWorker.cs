// This file defines the ARWorker class, which represents an AR device in the system.

using Grasshopper.Kernel;
using System;
using System.Windows.Forms;
using Vizor._1_System;
using Vizor.Properties;
using VizorLibs;
//using System.Collections.Generic;
//using Newtonsoft.Json;

namespace Vizor._2_AR
{
    /// <summary>  
    /// The ARWorker component represents an Augmented Reality (AR) device in the system.  
    /// It allows registering an AR device, such as a Hololens, and managing its configuration and communication.  
    ///  
    /// Inputs:  
    /// - Device Name: Identifier of the AR device (default: "HOLO1").  
    /// - Skill Configuration: Dictionary of skill parameters (default: "{\"monitor\": 1, \"screw\": 1, \"pick\": 1, \"place\": 1}").  
    ///  
    /// Outputs:  
    /// - AR Device: The registered AR device object.  
    /// </summary>

    public class ARWorker : DeviceComponent
    {
        private ARSystemDevice arSystemDevice;
        private string raw_config;
        //private Dictionary<string, int> skill;

        /// <summary>
        /// Initializes a new instance of the AR Worker class.
        /// </summary>
        public ARWorker()
          : base("Hololens", "Hololens",
              "Register a hololens to be used in the system.", "1_Object")
        {
        }

        /// <summary>
        /// Registers the input parameters for the component.
        /// </summary>
        /// <param name="pManager">The parameter manager.</param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddTextParameter("Device Name", "Name", "identifier of the AR device", GH_ParamAccess.item, "HOLO1");
            // TODO: this could toggle between a simple list input and a stringified dictionary with values
            pManager.AddTextParameter("Skill Configuration", "Skill", 
                "dictionary of skill parameters", GH_ParamAccess.item, "{\"monitor\": 1, \"screw\": 1, \"pick\": 1, \"place\": 1}");
        }

        /// <summary>
        /// Registers the output parameters for the component.
        /// </summary>
        /// <param name="pManager">The parameter manager.</param>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);
            pManager.AddGenericParameter("AR Device", "Device", "registered AR device", GH_ParamAccess.item);
        }

        /// <summary>
        /// Create a device object for the AR worker by the specified name
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            base.SolveInstance(DA);
            DA.SetData(1, this.disabled ? null : arSystemDevice);
        }

        /// <summary>
        /// Initializes the device for the AR worker by the specified name.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// <returns>The initialized device.</returns>
        protected override Device InitDevice(IGH_DataAccess DA)
        {
            this.arSystemDevice = new ARSystemDevice()
            {
                name = this.deviceName,
                wscObj = this.wscObj,
            };
            return arSystemDevice;
        }

        /// <summary>
        /// Updates the device associated with the component.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void UpdateDevice(IGH_DataAccess DA)
        {
            this.arSystemDevice.name = this.deviceName;
            this.arSystemDevice.wscObj = this.wscObj;
        }

        /// <summary>
        /// Clears the topics associated with the AR worker.
        /// </summary>
        protected override void ClearTopics()
        {
            if ((this.wscObj == null) || (!this.wscObj.isConnected()) || (deviceName == "")) return;

            ROSMessageHandler.Unsubscribe(this.wscObj, deviceName + "_Command", "std_msgs/String");
            ROSMessageHandler.Unsubscribe(this.wscObj, deviceName + "_Content", "vizor_package/SceneContent");
            ROSMessageHandler.Unsubscribe(this.wscObj, "/" + deviceName + "_GazePoint", "std_msgs/String");
            ROSMessageHandler.Unsubscribe(this.wscObj, "/" + deviceName + "_Model", "vizor_package/Model");
            registered = false;

            this.Message = "Disabled";
        }

        /// <summary>
        /// Subscribes to topics targeting the AR worker.
        /// </summary>
        protected override void InitTopics(IGH_DataAccess DA)
        {
            if ((this.wscObj == null) || (!this.wscObj.isConnected()) || (deviceName == "")) return;

            DA.GetData(3, ref raw_config);

            ROSMessageHandler.Subscribe(this.wscObj, deviceName + "_Command", "std_msgs/String");
            ROSMessageHandler.Subscribe(this.wscObj, "/" + deviceName + "_GazePoint", "std_msgs/String");
            ROSMessageHandler.Subscribe(this.wscObj, "/" + deviceName + "_Model", "vizor_package/Model");
            //ROSMessageHandler.Subscribe(this.wscObj, deviceName + "_Point", "std_msgs/String");

            ROSMessageHandler.Advertise(this.wscObj, deviceName + "_Config", "std_msgs/String");
            ROSMessageHandler.Advertise(this.wscObj, deviceName + "_Content", "vizor_package/SceneContent");
            //ROSMessageHandler.Advertise(this.wscObj, deviceName + "_Geometry", "vizor_package/SceneGeometry");
            //ROSMessageHandler.Advertise(this.wscObj, deviceName + "_Text", "vizor_package/SceneText");
            ROSMessageHandler.PublishSkillConfig(this.wscObj, deviceName, raw_config);
            registered = true;

            this.Message = deviceName;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.AR_Worker;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3a1af2b6-e0fa-4fe3-a08e-e41a596cb496"); }
        }
    }
}