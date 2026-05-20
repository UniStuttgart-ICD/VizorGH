using Grasshopper.Kernel;
using System;
using VizorLibs;

namespace Vizor._1_System
{

    /// <summary>  
    /// The SimulateDevice component is used to simulate the behavior of a network device,  
    /// allowing manual triggering of various device responses such as acknowledge, reject,  
    /// ping, and help messages. It supports both human-operated devices and robots.  
    ///  
    /// Inputs:  
    /// - Network Device (Device): The network device to simulate.  
    /// - Trigger Acknowledge (bool): Triggers an acknowledge message.  
    /// - Trigger Reject (bool): Triggers a reject or error message.  
    /// - Trigger Ping (bool): Triggers a ping message (e.g., a reminder).  
    /// - Trigger Help (bool): Triggers a help message (e.g., a request for assistance).  
    /// - TaskID (int): The task ID associated with the triggered message.  
    ///  
    /// Outputs:  
    /// - Output (string): Provides status messages about the simulation, such as  
    ///   connection status, message sent confirmations, or errors.  
    ///  
    /// This component also manages the connection state of the device and ensures  
    /// that messages are only sent when the device is connected and the state has changed.  
    /// </summary>
    
    public class SimulateDevice : VizorBaseComponent
    {
        Device mock_device;
        bool isHuman;
        bool acknowledge;
        bool rejectOrError;
        bool hurryOrPause;
        bool helpOrStop;
        bool initiated = false;
        int taskId = 0;
        private bool archiveAck = false;
        private bool archiveHelp = false;
        private bool archiveHurry = false;
        private bool archiveReject = false;

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SimulateDevice()
          : base("Simulate Device", "Simulate",
              "manually trigger the acknowledge response of a device", "4_Utilities")
        {
            isListener = false;
            this.initiated = false;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Network Device", "Device", "Network device to mock up", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Trigger Acknowledge", "Acknowledge", "trigger the acknowledge message", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Trigger Reject", "Reject", "trigger the reject message", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Trigger Ping", "Ping", "trigger the ping message (i.e. this device is reminding others in the team)", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Trigger Help", "Help", "trigger the help message (i.e. this device requires help)", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("TaskID", "ID", "task ID to request help on", GH_ParamAccess.item, 0);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Out", "status output", GH_ParamAccess.item);
        }

        private void updateParamName(bool isRobot)
        {

            if (isRobot)
            {
                Params.Input[1].Name = "Trigger Success";
                Params.Input[1].NickName = "Success";
                Params.Input[1].Description = "trigger a robot success message for this task";
                Params.Input[2].Name = "Trigger Error";
                Params.Input[2].NickName = "Error";
                Params.Input[2].Description = "trigger a robot error for this task";
                Params.Input[3].Name = "Trigger Pause";
                Params.Input[3].NickName = "Pause";
                Params.Input[3].Description = "trigger a pause command";
                Params.Input[4].Name = "Trigger Stop";
                Params.Input[4].NickName = "Stop";
                Params.Input[4].Description = "trigger a stop command";
            }
            else
            {
                Params.Input[1].Name = "Trigger Acknowledge";
                Params.Input[1].NickName = "Acknowledge";
                Params.Input[1].Description = "trigger the acknowledge message";
                Params.Input[2].Name = "Trigger Reject";
                Params.Input[2].NickName = "Reject";
                Params.Input[2].Description = "trigger the reject message";
                Params.Input[3].Name = "Trigger Ping";
                Params.Input[3].NickName = "Ping";
                Params.Input[3].Description = "trigger the ping message (i.e. this device is reminding others in the team)";
                Params.Input[4].Name = "Trigger Help";
                Params.Input[4].NickName = "Help";
                Params.Input[4].Description = "trigger the help message (i.e. this device requires help)";
            }
            Params.OnParametersChanged();
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref mock_device);
            if ( mock_device != null )
            {
                if (mock_device.name.Contains(","))
                {
                    this.Message = "error";
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Cannot simulate multiple devices. Create individual devices first and simualte them separately. ");
                    return;
                }
                if (UpdateDevice(mock_device))
                {
                    this.initiated = false;
                    DA.SetData(0, "device last updated on " + DateTime.Now.ToString());
                }
            }
            else
            {
                this.device = null;
               
                CleanupConnection();
                DA.SetData(0, "no device");
                return;
            }

            // if the device is not connected, return and ignore
            if (!this.device.wscObj.isConnected())
            {
                this.initiated = false;
                DA.SetData(0, "no connection");
                return;
            }

            this.Message = mock_device.name;

            isHuman = (mock_device is RobotObject) ? false : true;

            updateParamName(!isHuman);

            DA.GetData(1, ref acknowledge);
            DA.GetData(2, ref rejectOrError);
            DA.GetData(3, ref hurryOrPause);
            DA.GetData(4, ref helpOrStop);
            DA.GetData(5, ref taskId);

            // send message only if the stored state has changed
            if ((hurryOrPause && hurryOrPause == archiveHurry) || (acknowledge && acknowledge == archiveAck) ||
                (rejectOrError && rejectOrError == archiveReject) || (helpOrStop && helpOrStop == archiveHelp))
            {
                return;
            }
            archiveAck = acknowledge;
            archiveHurry = hurryOrPause;
            archiveHelp = helpOrStop;
            archiveReject = rejectOrError;

            if (this.device.wscObj!=null && this.device.wscObj.isConnected())
            {
                if (!this.initiated)
                {
                    if (isHuman)
                    {
                        ROSMessageHandler.Advertise(device.wscObj, device.name + "_Command", "std_msgs/String");
                        ROSMessageHandler.Advertise(device.wscObj, "WorkerPool_ShortMsg", "std_msgs/String");
                    }
                    else
                    {
                        ROSMessageHandler.Advertise(device.wscObj, "Robot/status", "std_msgs/String");
                        ROSMessageHandler.Advertise(device.wscObj, "Robot/control", "std_msgs/String");
                    }
                    this.initiated = true;
                }
                if (initiated && device.wscObj.isConnected())
                {
                    // task completion messages

                    if (acknowledge)
                    {
                        if (isHuman)
                        {
                            ROSMessageHandler.PublishMockAcknowledge(mock_device.wscObj, mock_device.name + "_Command");
                            DA.SetData(0, String.Format("AR Device {0} sent acknowledge message at {1}.", mock_device.name, DateTime.Now.ToString()));
                        }
                        else
                        {
                            ROSMessageHandler.PublishRobotStatus(mock_device.wscObj, "Robot/status", taskId, true);
                            DA.SetData(0, String.Format("Robot {0} sent acknowledge message at {1}.", mock_device.name, DateTime.Now.ToString()));
                        }
                    }

                    // task rejection messages

                    if (rejectOrError)
                    {
                        if (isHuman)
                        {
                            ROSMessageHandler.PublishMockReject(mock_device.wscObj, mock_device.name + "_Command", mock_device.name, taskId);
                            DA.SetData(0, String.Format("AR Device {0} sent reject message at {1}.", mock_device.name, DateTime.Now.ToString()));
                        }
                        else
                        {
                            ROSMessageHandler.PublishRobotStatus(mock_device.wscObj, "Robot/status", taskId, false);
                            DA.SetData(0, String.Format("Robot {0} sent error message at {1}.", mock_device.name, DateTime.Now.ToString()));
                        }
                    }

                    // emote messages

                    if (hurryOrPause)
                    {
                        if (isHuman)
                        {
                            ROSMessageHandler.PublishMockHurry(mock_device.wscObj, "WorkerPool_ShortMsg", mock_device.name);
                            DA.SetData(0, String.Format("AR Device {0} sent hurry message at {1}.", mock_device.name, DateTime.Now.ToString()));
                        }
                        else
                        {
                            ROSMessageHandler.PublishRobotControl(mock_device.wscObj, "Robot/control", "pause");
                            DA.SetData(0, String.Format("Robot {0} sent pause command at {1}.", mock_device.name, DateTime.Now.ToString()));
                        }
                    }

                    //if (!hurryOrPause && !isHuman)
                    //{
                    //    ROSMessageHandler.PublishRobotControl(mock_device.wscObj, "Robot/control", "resume");
                    //    DA.SetData(0, String.Format("Robot {0} sent resume command at {1}.", mock_device.name, DateTime.Now.ToString()));
                    //}

                    if (helpOrStop)
                    {
                        if (isHuman)
                        {
                            ROSMessageHandler.PublishMockHelp(mock_device.wscObj, "WorkerPool_ShortMsg", mock_device.name, taskId);
                            DA.SetData(0, String.Format("AR Device {0} asked for help at {1}.", mock_device.name, DateTime.Now.ToString()));
                        }
                        else
                        {
                            ROSMessageHandler.PublishRobotControl(mock_device.wscObj, "Robot/control", "stop");
                            DA.SetData(0, String.Format("Robot {0} sent stop command at {1}.", mock_device.name, DateTime.Now.ToString()));
                        }
                    }
                    return;
                }
                DA.SetData(0, String.Format("{0} ready.", mock_device.name));
            }
            else
            {
                DA.SetData(0, String.Format("{0} has no valid connection at {1}.", mock_device.name, DateTime.Now.ToString()));
            }

            //if (reject)
            //{
            //    // send reject
            //}
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Vizor.Properties.Resources.Simulate_Device;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1d5fcb9f-8af4-4cdc-b166-c15f958f4bde"); }
        }
    }
}