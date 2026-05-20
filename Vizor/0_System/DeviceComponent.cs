// This file defines the base class for device components in the Vizor system.
// It provides a framework for components that interact with a websocket object.

using Grasshopper.Kernel;
using VizorLibs;

namespace Vizor._1_System
{
    /// <summary>
    /// Base class of a device component, it does not have listeners.
    /// </summary>
    abstract public class DeviceComponent : GH_Component
    {
        // Holds the websocket object used for communication.
        protected WsObject wscObj;
        // Flag to indicate if the device is disabled.
        protected bool disabled;
        // Name of the device.
        protected string deviceName = "";
        // Flag to indicate if the device is registered.
        protected bool registered = false;

        /// <summary>
        /// Initializes a new instance of the DeviceComponent class.
        /// </summary>
        public DeviceComponent(string name, string nickname, string description, string subCategory)
          : base(name, nickname,
              description,
              "VizorGH", subCategory)
        {
        }

        /// <summary>
        /// Registers the input parameters for the component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Websocket Object", "WSC", "websocket object", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Disable", "Disable", "set to true to stop the device from publishing/subscribing to updates", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers the output parameters for the component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Out", "error log", GH_ParamAccess.item);
        }

        /// <summary>
        /// Solves the instance of the component, handling the logic for device initialization and updates.
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(2, ref deviceName);

            WsObject wscObj = new WsObject();
            if (DA.GetData(0, ref wscObj))
            {
                if (this.wscObj != wscObj)
                {
                    ClearTopics();
                    this.wscObj = wscObj;
                    DA.SetData(1, InitDevice(DA));
                    DA.SetData(0, "device is created");
                }
                else
                {
                    UpdateDevice(DA);
                }
            }
            else
            {
                DA.SetData(0, "please provide a ws object at input.");
                DA.SetData(1, null);
                this.Message = "Inactive";
                return;
            }

            DA.GetData(1, ref disabled);
            if (!disabled)
            {
                InitTopics(DA);
                DA.SetData(0, registered ? (deviceName + " registered") : (deviceName + " cannot be registered"));
            }
            else
            {
                ClearTopics();
                DA.SetData(0, deviceName + " is removed");
            }
        }

        /// <summary>
        /// Initializes the device. This method can be overridden in derived classes.
        /// </summary>
        protected virtual Device InitDevice(IGH_DataAccess DA)
        {
            return new Device();
        }

        /// <summary>
        /// Updates the device. This method can be overridden in derived classes.
        /// </summary>
        protected virtual void UpdateDevice(IGH_DataAccess DA)
        {
            return;
        }

        /// <summary>
        /// Clears the topics associated with the device. This method can be overridden in derived classes.
        /// </summary>
        protected virtual void ClearTopics() { }

        /// <summary>
        /// Initializes the topics for the device. This method can be overridden in derived classes.
        /// </summary>
        protected virtual void InitTopics(IGH_DataAccess DA) { }
    }
}