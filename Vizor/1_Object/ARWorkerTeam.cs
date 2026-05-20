// This file defines the ARWorkerTeam class, which represents a group of AR devices in the system.

using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vizor._1_System;
using Vizor.Properties;
using VizorLibs;
//using System.Collections.Generic;
//using Newtonsoft.Json;

namespace Vizor._1_Object
{
    /// <summary>  
    /// The ARWorkerTeam component is responsible for registering and managing a group of AR devices in the system.  
    ///  
    /// Inputs:  
    /// - Device Names: A comma-separated list of identifiers for the AR workers in the team (e.g., "HOLO1,HOLO2").  
    /// - Skill Configurations: A JSON-like dictionary defining skill parameters for each worker (e.g., "{\"HOLO1\": {\"monitor\": 1, \"screw\": 1, \"pick\": 1, \"place\": 1}, \"HOLO2\": {\"monitor\": 1}}").  
    ///  
    /// Outputs:  
    /// - AR Worker Team: The registered AR worker team object.  
    /// </summary>

    public class ARWorkerTeam : DeviceComponent
    {
        //private static List<string> defaultTeam = new List<string> { "HOLO1", "HOLO2"};
        private ARSystemDevice arWorkerTeamDevice;
        private string raw_config;
        //private Dictionary<string, Dictionary<string, int>> skills;

        /// <summary>
        /// Initializes a new instance of the ARWorkerTeam class.
        /// </summary>
        public ARWorkerTeam()
          : base("AR Worker Team", "Team",
              "Register a group of AR devices to be used in the system.", "1_Object")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddTextParameter("Device Names", "Names", 
                "identifier of the AR workers in the team, separated by commas", GH_ParamAccess.item, "HOLO1,HOLO2");
            pManager.AddTextParameter("Skill Configurations", "Skills", 
                "dictionary of skill parameters for each worker", GH_ParamAccess.item,
                "{\"HOLO1\": {\"monitor\": 1, \"screw\": 1, \"pick\": 1, \"place\": 1}, \"HOLO2\": {\"monitor\": 1}}");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);
            pManager.AddGenericParameter("AR Worker Team", "Team", "registered AR worker team", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //skills = (Dictionary<string, Dictionary<string, int>>) JsonConvert.DeserializeObject(raw_config);

            base.SolveInstance(DA);
            DA.SetData(1, this.disabled ? null : arWorkerTeamDevice);
        }

        /// <summary>
        /// Initializes the device for the AR worker team by the specified name.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// <returns>The initialized device.</returns>
        protected override Device InitDevice(IGH_DataAccess DA)
        {
            this.arWorkerTeamDevice = new ARSystemDevice()
            {
                name = this.deviceName,
                wscObj = this.wscObj,
            };
            return arWorkerTeamDevice;
        }

        /// <summary>
        /// Updates the device associated with the component.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void UpdateDevice(IGH_DataAccess DA)
        {
            this.arWorkerTeamDevice.name = this.deviceName;
            this.arWorkerTeamDevice.wscObj = this.wscObj;
        }

        /// <summary>
        /// Clears the topics associated with the AR worker team.
        /// </summary>
        protected override void ClearTopics()
        {
            if ((this.wscObj == null) || (!this.wscObj.isConnected()) || (deviceName == "")) return;

            string[] names = this.deviceName.Split(',');
            foreach (string name in names)
            {
                ROSMessageHandler.Unsubscribe(this.wscObj, name + "_Command", "std_msgs/String");
                //ROSMessageHandler.Unsubscribe(this.wscObj, name + "_Point", "std_msgs/String");
            }
            registered = false;
            this.Message = "Disabled";
        }

        /// <summary>
        /// Subscribes to topics targeting the AR worker team.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void InitTopics(IGH_DataAccess DA)
        {
            if ((this.wscObj == null) || (!this.wscObj.isConnected()) || (deviceName == "")) return;

            DA.GetData(3, ref raw_config);
            Dictionary<string, Dictionary<string, int>> skill_config =
                Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(raw_config);

            string[] names = this.deviceName.Split(',');
            foreach (string name in names)
            {
                ROSMessageHandler.Subscribe(this.wscObj, name + "_Command", "std_msgs/String");
                //ROSMessageHandler.Subscribe(this.wscObj, name + "_Point", "std_msgs/String");

                ROSMessageHandler.Advertise(this.wscObj, name + "_Config", "std_msgs/String");
                ROSMessageHandler.Advertise(this.wscObj, name + "_Content", "vizor_package/SceneContent");
                //ROSMessageHandler.Advertise(this.wscObj, name + "_Geometry", "vizor_package/SceneGeometry");
                //ROSMessageHandler.Advertise(this.wscObj, name + "_Text", "vizor_package/SceneText");

                ROSMessageHandler.PublishSkillConfig(this.wscObj, name, Newtonsoft.Json.JsonConvert.SerializeObject(skill_config[name]));
            }
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
                return Resources.AR_Worker_Team;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("593c1864-a7a6-481a-916f-5ec75855e540"); }
        }
    }
}