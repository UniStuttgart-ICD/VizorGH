// This file defines the TrackedObject class, which represents a tracked object, e.g., QR code, in the system.

using Grasshopper.Kernel;
using System;
using Vizor._1_System;
using VizorLibs;

namespace Vizor._1_System
{
    /// <summary>
    /// The TrackedObject component is responsible for registering a tracked object in the system.  
    ///  
    /// Inputs:  
    /// - Device Name (string): Identifier of the tracked object.  
    ///  
    /// Outputs:  
    /// - Tracked Device (InputDevice): The registered tracked object.  
    /// </summary>
    
    public class TrackedObject : DeviceComponent
    {
        private InputDevice inputDevice;
        /// <summary>
        /// Initializes a new instance of the Input_Device class.
        /// </summary>
        public TrackedObject()
          : base("Tracked Object", "Object", 
                "Register a tracked object in the system", "1_Object")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddTextParameter("Device Name", "Name", "identifier of the tracked object", GH_ParamAccess.item, "assembly");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);
            pManager.AddGenericParameter("Tracked Device", "Device", "registered tracked object", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            base.SolveInstance(DA);
            DA.SetData(1, this.disabled ? null : inputDevice);
        }

        /// <summary>
        /// Initializes the device for the tracked object by the specified name.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// <returns>The initialized device.</returns>
        protected override Device InitDevice(IGH_DataAccess DA)
        {
            inputDevice = new InputDevice()
            {
                name = this.deviceName,
                wscObj = this.wscObj,
            };
            return inputDevice;
        }

        /// <summary>
        /// Updates the device associated with the component.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void UpdateDevice(IGH_DataAccess DA)
        {
            inputDevice.name = this.deviceName;
            inputDevice.wscObj = this.wscObj;
        }

        /// <summary>
        /// Clears the topics associated with the tracked object.
        /// </summary>
        protected override void ClearTopics()
        {
            if ((this.wscObj == null) || (!this.wscObj.isConnected()) || (deviceName == "")) return;
            registered = false;
            ROSMessageHandler.Unsubscribe(this.wscObj, deviceName + "/data", "std_msgs/String");
            this.Message = "Disabled";
        }

        /// <summary>
        /// Subscribes to topics targeting the tracked object.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void InitTopics(IGH_DataAccess DA)
        {
            if ((this.wscObj == null) || (!this.wscObj.isConnected()) || (deviceName == "")) return;
            registered = true;
            ROSMessageHandler.Subscribe(this.wscObj, deviceName + "/data", "std_msgs/String");
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
                return Vizor.Properties.Resources.Tracked_Object;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("bc1f4be7-bcf1-4d24-bcf8-d3c97a2262d5"); }
        }
    }
}