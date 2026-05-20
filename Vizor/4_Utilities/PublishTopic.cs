using Grasshopper.Kernel;
using System;
using System.Runtime.Versioning;
using VizorLibs;

namespace Vizor._1_System
{
    /// <summary>
    /// The PublishTopic component allows you to publish a string message to a specified topic
    /// using a given message type and a WebSocket connection.
    /// 
    /// Note: This component only accepts a string message as input for now. 
    /// 
    /// Inputs:
    /// - WSC (WsObject): The WebSocket connection object.
    /// - Topic Name (string): The name of the topic to publish to.
    /// - Message Type (string): The type of the message (e.g., "std_msgs/String").
    /// - Message (string): The message content to publish.
    /// - Publish (bool): Set to true to publish the message.
    /// 
    /// Outputs:
    /// - Output (string): Status message about the publish operation.
    /// </summary>
    public class PublishTopic : VizorBaseComponent
    {
        private string topicName;
        private string messageType;
        private string message;
        private bool publish;

        public PublishTopic()
          : base("Publish Topic", "PublishTopic",
              "Publish a string message to a specified topic via WebSocket.", "4_Utilities")
        {
            isListener = false;
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("WSC", "WSC", "WebSocket connection object", GH_ParamAccess.item);
            pManager.AddTextParameter("Topic Name", "Topic", "Topic to publish to", GH_ParamAccess.item, "/Robot/reset");
            pManager.AddTextParameter("Message Type", "Type", "Message type", GH_ParamAccess.item, "std_msgs/String");
            pManager.AddTextParameter("Message", "Msg", "Message to publish", GH_ParamAccess.item, "UR10");
            pManager.AddBooleanParameter("Publish", "P", "Set to true to publish the message", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Out", "Status output", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref wscObj);
            DA.GetData(1, ref topicName);
            DA.GetData(2, ref messageType);
            DA.GetData(3, ref message);
            DA.GetData(4, ref publish);

            if (wscObj == null)
            {
                DA.SetData(0, "No WebSocket connection object provided.");
                return;
            }

            if (string.IsNullOrWhiteSpace(topicName) || string.IsNullOrWhiteSpace(messageType))
            {
                DA.SetData(0, "Topic name and message type are required.");
                return;
            }

            if (!wscObj.isConnected())
            {
                DA.SetData(0, "WebSocket is not connected.");
                return;
            }

            if (publish)
            {
                ROSMessageHandler.Advertise(wscObj, topicName, messageType);
                ROSMessageHandler.PublishStringMessage(wscObj, topicName, message);
                DA.SetData(0, $"Published message '{message}' to '{topicName}' at {DateTime.Now}.");
            }
            else
            {
                DA.SetData(0, "Set 'Publish' to true to send the message.");
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Vizor.Properties.Resources.PublishMsg;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("e2b7c7e2-1b2a-4e7a-9b2e-2c7e2b7c7e2a"); }
        }
    }
}
