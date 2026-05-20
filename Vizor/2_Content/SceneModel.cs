using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Vizor._1_System;
using VizorLibs;
using VizorLibs.MessageTypes;

namespace Vizor._2_AR
{
    /// <summary>  
    /// The SceneModel component facilitates the construction of AR scene content for networked or offline use.  
    /// This is an alternative using task-based management of AR construction steps. 
    /// It operates in two modes:  
    /// 1. LISTENER MODE: Responds to external requests for AR content updates.  
    /// 2. MANUAL MODE: Allows manual control for sending or removing AR content using send/remove triggers.
    ///  
    /// Inputs:  
    /// 1. GH Control (Boolean): Set to true for Grasshopper-based control (default), or false for offline content upload.  
    /// 2. AR Devices (Generic List): List of registered AR devices.  
    /// 3. Content Objects (Generic List): List of AR content objects in the model.  
    /// 4. Trigger Send (Boolean): Manual input to trigger sending the geometry.  
    /// 5. Trigger Remove (Boolean): Manual input to trigger removing the geometry.  
    /// 6. Time Delay (Integer): Time delay in milliseconds (default: 100ms).  
    ///  
    /// Outputs:  
    /// 1. Output (Text): Status message indicating the result of the operation.  
    ///  
    /// This component is designed to integrate seamlessly with Grasshopper and Vizor's AR ecosystem,  
    /// enabling efficient AR scene management and interaction.  
    /// </summary>
    
    public class SceneModel : VizorBaseComponent
    {
        private bool onlineMode;
        private bool manualSend;
        private bool manualRemove;
        private List<SceneContentObject> contents;
        private List<string> deviceNames;
        private int delay;

        /// <summary>
        /// Initializes a new instance of the ModelSender class.
        /// </summary>
        public SceneModel()
          : base("XR Scene Model", "Scene Model",
              "Provide model to be accessed on the network", "2_Content")
        {
            isListener = false;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("GH Control", "GH",
                "set to true for using GH as the scene control (default), set to false for uploading contents to operate offline", 
                GH_ParamAccess.item, true);
            pManager.AddGenericParameter("AR Devices", "Devices", "list of registered AR devices", GH_ParamAccess.list);
            pManager.AddGenericParameter("Content Objects", "Content", "list of AR content objects in the model", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Trigger Send", "Send", 
                "manual input to trigger sending the geometry", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Trigger Remove", "Remove", 
                "manual input to trigger removing the geometry", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Time Delay", "Delay", 
                "time in milliseconds, default 100ms", GH_ParamAccess.item, 100);
            //pManager.AddTextParameter("Operation Type", "Type", "session-local, session, persistent etc.", GH_ParamAccess.item, "add-persistent");
        }

        /// <summary>
        /// Outputs the status message.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Out", "status output", GH_ParamAccess.item);
        }

        private void updateParamName(bool isOnline)
        {

            if (isOnline)
            {
                Params.Input[3].Name = "Trigger Send";
                Params.Input[3].NickName = "Send";
                Params.Input[3].Description = "manual input to trigger sending the geometry";
                Params.Input[4].Name = "Trigger Remove";
                Params.Input[4].NickName = "Remove";
                Params.Input[4].Description = "manual input to trigger removing the geometry";
            }
            else
            {
                Params.Input[3].Name = "---";
                Params.Input[3].NickName = "---";
                Params.Input[3].Description = "invalid input (set 'Online Control' to true to use triggers)";
                Params.Input[4].Name = "---";
                Params.Input[4].NickName = "---";
                Params.Input[4].Description = "invalid input (set 'Online Control' to true to use triggers)";
            }
            Params.OnParametersChanged();
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsDocumentActive()) return;

            DA.GetData(0, ref this.onlineMode);
            updateParamName(this.onlineMode);
            

            if (!this.onMessageTriggered)
            {
                // Respond to manual triggers in Grasshopper
                List<Device> _devices = new List<Device>();
                if (DA.GetDataList(1, _devices))
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

                

                contents = new List<SceneContentObject>();
                DA.GetDataList(2, contents);
                this.Message = String.Format("{0} contents", contents.Count);

                DA.GetData(3, ref manualSend);
                DA.GetData(4, ref manualRemove);
                DA.GetData(5, ref this.delay);

                if (onlineMode)
                {
                    //isListener = true;

                    // manual sending to all devices
                    if (manualSend && this.wscObj.isConnected())
                    {
                        string status = "sent to: ";
                        string items = "";
                        foreach (string name in deviceNames)
                        {
                            items = SendModel(name, false);
                            if (items != "") status += name + ", ";
                        }
                        DA.SetData(0, items + "sent to " + status + "on " + DateTime.Now.ToString());
                        return;
                    }

                    // manual removing to all devices
                    if (manualRemove && this.wscObj.isConnected())
                    {
                        string status = "removed from: ";
                        string items = "";
                        foreach (string name in deviceNames)
                        {
                            items = ClearModel(name);
                            if (items != "") status += name + ", ";
                        }
                        DA.SetData(0, "removed from " + status + "on " + DateTime.Now.ToString());
                        return;
                    }
                }
                else
                {
                    //isListener = false;

                    // upload to data store
                    string items = SendModel("data store", true);
                    DA.SetData(0, items + "uploaded on " + DateTime.Now.ToString());
                    return;
                }
            }

            else
            {
                // Respond to requests made from a specific device

                this.onMessageTriggered = false;

                if (this.wscObj.message != null)
                {
                    string[] payload = ROSMessageHandler.ParseCommand(this.wscObj.message);
                    if (payload == null) return;
                    string items = "";
                    
                    // send the entire model
                    if (payload[1] == "load_model")
                    {
                        foreach (string name in deviceNames)
                        {
                            if (name == payload[0])
                            {
                                items = SendModel(device.name, false);
                                break;
                            }
                        }
                    }
                    // send content matching a specific name
                    else if (payload[1].StartsWith("load_model_"))
                    {
                        string content_name = payload[1].Substring(11);
                        foreach (string name in deviceNames)
                        {
                            if (name == payload[0])
                            {
                                items = SendContentToDevice(device.name, content_name);
                                break;
                            }
                        }
                    }
                    else
                    {
                        return;
                    }

                    if (items != "")
                    {
                        DA.SetData(0, items + "sent to " + payload[0] + " \nlast updated on " + DateTime.Now.ToString());
                    }
                    else
                    {
                        DA.SetData(0, "failed to send model to " + payload[0] + " \nlast updated on " + DateTime.Now.ToString());
                    }
                }
            }
        }

        private string SendModel(string targetDeviceName, bool upload)
        {
            if (this.wscObj != null)
            {
                DateTime start = DateTime.Now;
                string names = "";

                // send scene contents
                for (int i = 0; i < contents.Count; i++)
                {
                    contents[i].operation = "add-scene"; // harmless to overwrite this field so no need to copy the object
                    if (upload)
                    {
                        ROSMessageHandler.Advertise(this.wscObj, "DataStore/scene/add", "vizor_package/SceneContent");
                        ROSMessageHandler.UploadContent(wscObj, contents[i].GetSceneContentMsg());
                    }
                    else
                    {
                        ROSMessageHandler.PublishContent(wscObj, targetDeviceName, contents[i].GetSceneContentMsg());
                    }
                    names += contents[i].name + ", ";
                    System.Threading.Thread.Sleep(delay);
                }

                TimeSpan duration = DateTime.Now - start;
                this.Message = "sent in " + duration.TotalMilliseconds.ToString() + "ms";

                return names;
            }
            else return "";
        }

        private string SendContentToDevice(string targetDeviceName, string contentName)
        {
            if (this.wscObj != null && !onlineMode)
            {
                DateTime start = DateTime.Now;
                string names = "";

                // send scene content matching a specific name
                for (int i = 0; i < contents.Count; i++)
                {
                    if (contentName == contents[i].name)
                    {
                        contents[i].operation = "add-scene";
                        ROSMessageHandler.PublishContent(wscObj, targetDeviceName, contents[i].GetSceneContentMsg());
                        names += contents[i].name + ", ";
                        //System.Threading.Thread.Sleep(delay);
                        TimeSpan duration = DateTime.Now - start;
                        this.Message = "sent in " + duration.TotalMilliseconds.ToString() + "ms";
                        return names;
                    }
                }
                return "";
            }
            else return "";
        }

        // sending only data that has changed (no mesh payload, only colors / thickness etc.)
        // private string UpdateModel(string targetDeviceName)

        private string ClearModel(string targetDeviceName)
        {
            if (this.wscObj != null)
            {
                DateTime start = DateTime.Now;
                string names = "";

                // send scene contents
                for (int i = 0; i < contents.Count; i++)
                {
                    contents[i].operation = "remove";
                    ROSMessageHandler.PublishContent(wscObj, targetDeviceName, contents[i].GetSceneContentMsg());
                    names += contents[i].name + ", ";
                    System.Threading.Thread.Sleep(delay);
                }

                TimeSpan duration = DateTime.Now - start;
                this.Message = "removed in " + duration.TotalMilliseconds.ToString() + "ms";
                
                return names;
            }
            else return "";
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Vizor.Properties.Resources.Scene_Model;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("028cda0a-93ed-4954-90aa-de13be27da43"); }
        }
    }
}