using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Vizor._1_System;
using Vizor.Properties;
using VizorLibs;
using VizorLibs.MessageTypes;

namespace Vizor._4_Utilities
{
    /// <summary>
    /// The GazeTracker class is a Grasshopper component designed to track the Area of Interest (AOI) based on gaze data from an AR device.
    /// 
    /// Inputs:
    /// - Online (Boolean): Set this to true to enable live gaze data reception in Grasshopper. 
    /// - AR Device (Generic): The AR device (e.g., HoloLens) where the gaze data comes from. 
    ///
    /// Outputs:
    /// - Area of Interest (Text): The output AOI (Area of Interest) data based on the gaze tracking information.
    /// 
    /// This component listens to live gaze data from the specified AR device and outputs the AOI. 
    /// </summary>
    
    public class GazeTracker : VizorBaseComponent
    {
        Device currentDevice;
        //private string gazeTarget;

        /// <summary>
        /// Initializes a new instance of the GazeTracker class.
        /// </summary>
        public GazeTracker()
          : base("GazeTracker", "GazeTracker",
              "Listen to AOI by the specified AR device",
              "4_Utilities")
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
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Area of Interest", "AOI", "output AOI", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsDocumentActive()) return;
            DA.GetData(0, ref this.isListener);
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
                    string data = ROSMessageHandler.ParseGazePointMessage(currentDevice.name, wscObj.message);
                    if (data != null)
                    {
                        DA.SetData(0, data);
                    }
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
                return Resources.GazeTracker;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5237AE0A-31E4-4DD2-8BD4-B85EBF333B53"); }
        }
    }
}