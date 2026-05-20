using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Vizor._1_System;
using VizorLibs;
using VizorLibs.MessageTypes;
using Newtonsoft.Json;

namespace Vizor._2_AR
{
    /// <summary>
    /// The SceneDynamic component manages dynamic AR content switching based on external signals.
    /// It listens for messages on the signal/{robot_name} topic and displays content matching the signal name.
    /// When a new content is requested, it removes the previously displayed content before showing the new one.
    /// 
    /// Inputs:
    /// 1. Active (Boolean): Set to true to enable listening for content switching signals.
    /// 2. AR Devices (List): List of AR devices to send content to.
    /// 3. Robot Name (String): Name of the robot sending signals (default: "ur10").
    /// 4. Content Objects (List): List of AR content objects available for dynamic display.
    /// 
    /// Outputs:
    /// 1. Status (Text): Name of the last sent content and timestamp.
    /// </summary>
    public class SceneDynamic : VizorBaseComponent
    {
        private bool active;
        private List<Device> targetDevices;
        private string inputName;
        private List<SceneContentObject> contentObjects;
        private Dictionary<string, SceneContentObject> contentMap;
        private string lastContent;
        private DateTime lastSentTime;
        private string currentTopic;

        /// <summary>
        /// Initializes a new instance of the SceneDynamic class.
        /// </summary>
        public SceneDynamic()
          : base("Scene Dynamic", "Scene Dynamic",
              "Dynamically switch AR content based on external signals",
              "2_Content")
        {
            isListener = false;
            targetDevices = new List<Device>();
            inputName = "ur10";
            contentObjects = new List<SceneContentObject>();
            contentMap = new Dictionary<string, SceneContentObject>();
            lastContent = "";
            lastSentTime = DateTime.MinValue;
            currentTopic = "";
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Active", "Active",
                "set to true to enable listening for content switching signals",
                GH_ParamAccess.item, false);
            pManager.AddGenericParameter("AR Devices", "Devices",
                "list of AR devices to send content to",
                GH_ParamAccess.list);
            pManager.AddTextParameter("Input Name", "Input",
                "name of the input device sending the signals (e.g., ur10)",
                GH_ParamAccess.item, "ur10");
            pManager.AddGenericParameter("Content Objects", "Content",
                "list of AR content objects available for dynamic display",
                GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status",
                "name of last sent content and timestamp",
                GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsDocumentActive()) return;

            // Get active state
            bool newActive = false;
            DA.GetData(0, ref newActive);

            // Get input name
            string newInputName = "";
            DA.GetData(2, ref newInputName);

            // Calculate the topic
            string signalTopic = "signal/" + newInputName;
            bool topicChanged = (signalTopic != currentTopic);

            // Update device connections
            targetDevices.Clear();
            if (!DA.GetDataList(1, targetDevices) || targetDevices.Count == 0)
            {
                // Unsubscribe before cleanup if we were listening
                if (isListener && this.wscObj != null && !string.IsNullOrEmpty(currentTopic))
                {
                    ROSMessageHandler.Unsubscribe(wscObj, currentTopic, "std_msgs/String");
                    isListener = false;
                }
                CleanupConnection();
                currentTopic = "";
                DA.SetData(0, "no device connection");
                return;
            }

            // Check if devices changed (which might change wscObj)
            bool devicesChanged = UpdateDevices(targetDevices);

            // Handle topic change - unsubscribe from old topic if needed
            if (topicChanged && isListener && this.wscObj != null && !string.IsNullOrEmpty(currentTopic))
            {
                ROSMessageHandler.Unsubscribe(wscObj, currentTopic, "std_msgs/String");
                isListener = false;
            }

            // Update current topic and input name
            currentTopic = signalTopic;
            inputName = newInputName;

            // Manage subscription state
            if (this.wscObj != null && this.wscObj.isConnected())
            {
                bool shouldBeListening = newActive;
                bool needsSubscriptionUpdate = (shouldBeListening != isListener) || devicesChanged || topicChanged;

                if (needsSubscriptionUpdate)
                {
                    if (shouldBeListening && !isListener)
                    {
                        // Subscribe to the signal topic
                        ROSMessageHandler.Subscribe(wscObj, currentTopic, "std_msgs/String");
                        isListener = true;
                    }
                    else if (!shouldBeListening && isListener)
                    {
                        // Unsubscribe from the signal topic
                        ROSMessageHandler.Unsubscribe(wscObj, currentTopic, "std_msgs/String");
                        isListener = false;
                    }
                    else if (shouldBeListening && isListener && (devicesChanged || topicChanged))
                    {
                        // Re-subscribe with new websocket or topic
                        ROSMessageHandler.Subscribe(wscObj, currentTopic, "std_msgs/String");
                    }
                }
            }

            // Update active state
            active = newActive;

            // Update content objects
            contentObjects.Clear();
            DA.GetDataList(3, contentObjects);

            // Build content map for quick lookup
            contentMap.Clear();
            foreach (SceneContentObject content in contentObjects)
            {
                if (!contentMap.ContainsKey(content.name))
                {
                    contentMap.Add(content.name, content);
                }
            }

            this.Message = String.Format("{0} devices, {1} contents\nlast: {2}", 
                targetDevices.Count, contentObjects.Count, lastContent);

            if (!this.onMessageTriggered)
            {
                // No message received, show current status
                if (lastContent != "")
                {
                    DA.SetData(0, String.Format("Last sent: {0}\nTimestamp: {1}",
                        lastContent, lastSentTime.ToString()));
                }
                else
                {
                    if (active)
                    {
                        DA.SetData(0, String.Format("Listening on {0}", currentTopic));
                    }
                    else
                    {
                        DA.SetData(0, "Inactive - set Active to true to start listening");
                    }
                }
            }
            else
            {
                // Message received, process content switching
                this.onMessageTriggered = false;

                if (this.wscObj.message != null)
                {
                    // Parse the message from signal/{robotName} topic
                    string requestedContent = ParseSignalMessage(this.wscObj.message);
                    
                    if (requestedContent == null)
                    {
                        DA.SetData(0, "Invalid message format or wrong topic");
                        return;
                    }

                    // Check if requested content exists
                    if (contentMap.ContainsKey(requestedContent))
                    {
                        // Remove previous content if exists - send to all devices
                        if (!string.IsNullOrEmpty(lastContent) && contentMap.ContainsKey(lastContent))
                        {
                            SceneContentObject oldContent = contentMap[lastContent];
                            oldContent.operation = "remove";
                            
                            foreach (Device targetDevice in targetDevices)
                            {
                                ROSMessageHandler.PublishContent(wscObj, targetDevice.name, oldContent.GetSceneContentMsg());
                            }
                            System.Threading.Thread.Sleep(50); // Brief delay between remove and add
                        }

                        // Send new content to all devices
                        SceneContentObject newContent = contentMap[requestedContent];
                        newContent.operation = "add-scene";
                        
                        foreach (Device targetDevice in targetDevices)
                        {
                            ROSMessageHandler.PublishContent(wscObj, targetDevice.name, newContent.GetSceneContentMsg());
                        }

                        // Update tracking variables
                        lastContent = requestedContent;
                        lastSentTime = DateTime.Now;

                        DA.SetData(0, String.Format("Sent: {0}\nTo {1} device(s)\nTimestamp: {2}",
                            lastContent, targetDevices.Count, lastSentTime.ToString()));
                    }
                    else
                    {
                        DA.SetData(0, String.Format("Content '{0}' not found in available contents", requestedContent));
                    }
                }
            }
        }

        /// <summary>
        /// Parses the signal message from signal/{robotName} topic
        /// </summary>
        /// <param name="message">The ROS message JSON string</param>
        /// <returns>The content name from the message payload, or null if invalid</returns>
        private string ParseSignalMessage(string message)
        {
            try
            {
                // Parse as a ROS string message
                ROSMessageString _msg = JsonConvert.DeserializeObject<ROSMessageString>(message);
                
                // Verify this is from the correct topic
                if (_msg != null && _msg.topic == currentTopic)
                {
                    return _msg.msg.data;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Vizor.Properties.Resources.SceneDynamic;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("33430B46-AC8F-4A5D-98F8-1453B22C57A2"); }
        }
    }
}