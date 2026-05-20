using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using Vizor._1_System;
using VizorLibs.MessageTypes;
using VizorLibs;

namespace Vizor._2_Human
{
    /// <summary>
    /// The SensorTracker component is designed to track a sensor topic and update associated content objects
    /// based on live sensor data. It allows for dynamic updates to AR devices and content objects, including
    /// text, color, and scale transformations based on sensor values.
    /// </summary>
    /// 
    /// Inputs:
    /// - Online (Boolean): Set to true to enable live sensor data reception in Grasshopper. If false, data is sent to target devices but not displayed in Grasshopper.
    /// - Register (Boolean): Set to true to register listeners and false to remove them.
    /// - Tracked Object (Generic): The target object to track, such as a sensor.
    /// - AR Devices (Generic List): A list of AR devices to update, e.g., specific AR device names.
    /// - Content Object (Generic): The content object to modify based on input data (may or may not be anchored to the tracked device).
    /// - Value Min (Number): The minimum sensor value.
    /// - Value Max (Number): The maximum sensor value.
    /// - Update Text (Boolean): Indicates whether to update the displayed text.
    /// - Prefix (Text): A prefix to prepend to the displayed text.
    /// - Suffix (Text): A suffix to append to the displayed text.
    /// - Color Min (Color): The color to display for the minimum sensor value (applied to meshes and wireframes).
    /// - Color Max (Color): The color to display for the maximum sensor value (applied to meshes and wireframes).
    /// - Scale Min (Number): The scale to apply at the minimum sensor value (applied to meshes and wireframes).
    /// - Scale Max (Number): The scale to apply at the maximum sensor value (applied to meshes and wireframes).
    /// 
    /// Outputs:
    /// - Status (Text): The status of the component, including connection and update information.
    /// - Data (Text): The live data stream from the tracked sensor.
    
    public class SensorTracker : VizorBaseComponent
    {
        private string sensorData;
        private string status;
        private bool register;
        private SceneContentObject contentObject;
        private Device trackedObject;
        private double valueMin;
        private double valueMax;
        private string suffix;
        private string prefix;
        private bool udpateText;
        private string colorMin;
        private string colorMax;
        private double scaleMin;
        private double scaleMax;
        private List<string> deviceNames;

        private bool registrationSent;

        /// <summary>
        /// Initializes a new instance of the SensorTracker class.
        /// </summary>
        public SensorTracker()
          : base("SensorTracker", "Sensor",
              "Track a sensor topic", "4_Utilities")
        {
            sensorData = "";
            isListener = false;
            registrationSent = false;
        }

        /// <summary>
        /// Create a sensor tracker and the content to be updated by the input
        /// </summary>
        /// <param name="pManager"></param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Online", "O", 
                "Set this to true if you want GH to receive the live sensor data. \n" +
                "If this is false, the data will still be sent to the target devices, but GH will not provide the sensor reading. ", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Register", "R", "Set to true to register and false to remove the listeners. ", GH_ParamAccess.item, false);
            
            pManager.AddGenericParameter("Tracked Objet", "T", "Target to track, e.g., sensor", GH_ParamAccess.item);
            pManager.AddGenericParameter("AR Devices", "D", "Devices to udpate, e.g., specific AR device names", GH_ParamAccess.list);
            pManager.AddGenericParameter("Content Object", "C", "the content object to change based on input data \n" +
                "(which may or may not be anchored on the tracked device)", GH_ParamAccess.item);
            
            pManager.AddNumberParameter("Value Min", "Vmin", "the minimum sensor value", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Value Max", "Vmax", "the maximum sensor value", GH_ParamAccess.item, 1);
            pManager.AddBooleanParameter("Update Text", "T", "whether or not to update the displayed text", 
                GH_ParamAccess.item, true);
            pManager.AddTextParameter("Prefix", "P", "prefix", GH_ParamAccess.item, "Value: ");
            pManager.AddTextParameter("Suffix", "S", "suffix", GH_ParamAccess.item, " N");
            pManager.AddColourParameter("Color Min", "Cmin", "colour to display with the minimum value (applied to meshes and wireframes)", 
                GH_ParamAccess.item, System.Drawing.Color.FromArgb(255, 9, 169, 237));
            pManager.AddColourParameter("Color Max", "Cmax", "colour to display with the maximum value (applied to meshes and wireframes)", 
                GH_ParamAccess.item, System.Drawing.Color.FromArgb(255, 9, 169, 237));
            pManager.AddNumberParameter("Scale Min", "Smin", "scale at the minimum value (applied to meshes and wireframes)", 
                GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Scale Max", "Smax", "scale at the minimum value (applied to meshes and wireframes)", 
                GH_ParamAccess.item, 1.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status", "status output", GH_ParamAccess.item);
            pManager.AddTextParameter("Data", "Data", "data stream", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsDocumentActive()) return;

            DA.GetData(0, ref this.isListener); // when disabled, incoming message will not trigger update
            DA.GetData(1, ref this.register);
            DA.GetData(2, ref this.trackedObject);

            if (!this.onMessageTriggered)
            {
                this.Message = "";
                // Set AR device object
                List<Device> _devices = new List<Device>();
                if (DA.GetDataList(3, _devices))
                {
                    if (UpdateDevices(_devices))
                    {
                        DA.SetData(0, "devices updated\nlast updated on " + DateTime.Now.ToString());

                        // add both individual and team devices
                        deviceNames = new List<string>();
                        foreach (Device device in devices)
                        {
                            if (device.name.Contains(","))
                            {
                                foreach (string chunk in device.name.Split(','))
                                {
                                    deviceNames.Add(chunk);
                                }
                            }
                            else
                            {
                                deviceNames.Add(device.name);
                            }
                        }
                    }
                }
                else
                {
                    deviceNames = new List<string>();
                    CleanupConnection();
                    DA.SetData(0, "no connection");
                    return;
                }

                System.Drawing.Color colorMIN = new System.Drawing.Color();
                System.Drawing.Color colorMAX = new System.Drawing.Color();

                DA.GetData(4, ref this.contentObject);
                DA.GetData(5, ref valueMin);
                DA.GetData(6, ref valueMax);
                DA.GetData(7, ref udpateText);
                DA.GetData(8, ref prefix);
                DA.GetData(9, ref suffix);
                DA.GetData(10, ref colorMIN);
                DA.GetData(11, ref colorMAX);
                DA.GetData(12, ref scaleMin);
                DA.GetData(13, ref scaleMax);

                colorMin = String.Format("{0},{1},{2},{3}", colorMIN.R, colorMIN.G, colorMIN.B, colorMIN.A);
                colorMax = String.Format("{0},{1},{2},{3}", colorMAX.R, colorMAX.G, colorMAX.B, colorMAX.A);
                this.Message = "Tracking " + trackedObject.name;


                if (register)
                {
                    if (registrationSent) return;
                    ROSMessageHandler.Advertise(trackedObject.wscObj, "WorkerPool_Sensor", "vizor_package/RegisterSensor");
                    RegisterSensorMsg msg = new RegisterSensorMsg
                    {
                        sensor_topic = trackedObject.name + "/data",
                        devices = deviceNames.ToArray(),
                        content = contentObject.GetSceneContentMsg(),
                        value_min = (float) valueMin,
                        value_max = (float) valueMax,
                        update_text = udpateText,
                        text_prefix = prefix,
                        text_suffix = suffix,
                        color_min = colorMin,
                        color_max = colorMax,
                        scale_min = (float) scaleMin,
                        scale_max = (float) scaleMax
                    };
                    msg.content.operation = "add-scene";
                    ROSMessageHandler.PublishSensorRegistration(trackedObject.wscObj, msg);
                    registrationSent = true;
                }
                else
                {
                    if (!registrationSent) return;
                    RegisterSensorMsg msg = new RegisterSensorMsg
                    {
                        sensor_topic = trackedObject.name + "/data",
                        devices = deviceNames.ToArray(),
                        content = contentObject.GetSceneContentMsg(),
                        value_min = (float) valueMin,
                        value_max = (float) valueMax,
                        update_text = udpateText,
                        text_prefix = prefix,
                        text_suffix = suffix,
                        color_min = colorMin,
                        color_max = colorMax,
                        scale_min = (float) scaleMin,
                        scale_max = (float) scaleMax
                    };
                    msg.content.operation = "remove";
                    ROSMessageHandler.PublishSensorRegistration(trackedObject.wscObj, msg);
                    registrationSent = false;
                }

            }
            else
            {
                // this block will not be run if isListener is set to false
                
                this.onMessageTriggered = false;
                if (this.wscObj.message != null)
                {
                    string data = ROSMessageHandler.ParseTrackedData(device.name, wscObj.message);
                    if (data == null)
                    {
                        this.Message = "no data";
                        return;
                    }

                    sensorData = data;
                    DA.SetData(1, sensorData);
                    this.Message = ">>"+ device.name;

                    status = "last updated on " + DateTime.Now.ToString();
                    DA.SetData(0, status);
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
                return Vizor.Properties.Resources.Sensor_Tracker;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("58118ff7-fa58-4cc1-b080-ca617fb73ece"); }
        }
    }
}