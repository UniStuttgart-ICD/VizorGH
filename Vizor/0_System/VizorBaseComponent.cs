// This file defines the base class for Vizor components with optional listeners attached.

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Newtonsoft.Json;
using VizorLibs;

namespace Vizor._1_System
{
    /// <summary>
    /// Base class of a Vizor component with optional listeners attached.
    /// </summary>
    abstract public class VizorBaseComponent : GH_Component
    {
        // Flag to indicate if a message has been triggered.
        protected bool onMessageTriggered = false;
        // Reference to the Grasshopper document.
        protected GH_Document ghDocument;

        // Flag to indicate if the component is a listener.
        protected bool isListener;
        // Flag to track the previous listener state.
        protected bool isListenerPast;

        // The online flag is only used by the PlanTrajectory component.
        // When online flag is true, the solution will not re-compute.
        // It prevents the component update unless PLAN or REQUEST is pressed.
        protected bool online;

        // Reference to the device.
        protected Device device;
        // List of devices.
        protected List<Device> devices;
        // Reference to the websocket object.
        protected WsObject wscObj;
        // Flag to indicate if a new solution is being requested.
        protected bool isAskingNewSolution;

        /// <summary>
        /// Initializes a new instance of the VizorBaseComponent class.
        /// </summary>
        public VizorBaseComponent(string name, string nickname, string description, string subCategory)
          : base(name, nickname,
              description,
              "VizorGH", subCategory)
        {
        }

        /// <summary>
        /// Checks if the document is active.
        /// </summary>
        /// <returns>True if the document is active, otherwise false.</returns>
        protected bool IsDocumentActive()
        {
            if (this.ghDocument == null)
            {
                this.ghDocument = OnPingDocument();
                if (this.ghDocument == null) return false;
            }
            return true;
        }

        /// <summary>
        /// Updates the devices associated with the component.
        /// </summary>
        /// <param name="_devices">The list of devices to update.</param>
        /// <returns>True if the devices were updated, otherwise false.</returns>
        protected bool UpdateDevices(List<Device> _devices)
        {
            List<Device> devices = _devices.Where(x => x != null).ToList();
            if ((this.devices == null) || !devices.All(this.devices.Contains)) //if not all items in the new list are already included in the old one
            {
                this.devices = devices;
                //if (devices[0] == null) return false;
                // vertify if all objects need shared socket
                WsObject _wscObj = devices[0].wscObj;
                isListenerPast = isListener;

                if (this.wscObj != _wscObj)
                {
                    if (isListener) this.UnsubscribeEventHandlers();
                    this.wscObj = _wscObj;
                    if (isListener) this.SubscribeEventHandlers();
                }
                return true;
            }
            else
            {
                if (isListener != isListenerPast)
                {
                    isListenerPast = isListener;
                    if (isListener)
                    {
                        this.SubscribeEventHandlers();
                    }
                    else
                    {
                        this.UnsubscribeEventHandlers();
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Updates the device associated with the component.
        /// </summary>
        /// <param name="_device">The device to update.</param>
        /// <returns>True if the device was updated, otherwise false.</returns>
        protected bool UpdateDevice(Device _device)
        {
            if ((this.device == null) || (!device.IsSameAs(_device)))
            {
                this.device = _device;
                if (device == null) return false;
                WsObject _wscObj = device.wscObj;
                isListenerPast = isListener;

                if (this.wscObj != _wscObj)
                {
                    if (isListener) this.UnsubscribeEventHandlers();
                    this.wscObj = _wscObj;
                    if (isListener) this.SubscribeEventHandlers();
                }
                isListenerPast = isListener;
                return true;
            }
            else
            {
                if (isListener != isListenerPast)
                {
                    isListenerPast = isListener;
                    if (isListener)
                    {
                        this.SubscribeEventHandlers();
                    }
                    else
                    {
                        this.UnsubscribeEventHandlers();
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Cleans up the connection by unsubscribing event handlers and resetting device references.
        /// </summary>
        protected void CleanupConnection()
        {
            if (isListener) this.UnsubscribeEventHandlers();
            this.devices = new List<Device>();
            this.device = null;
            this.wscObj = null;
            this.onMessageTriggered = false;
        }

        /// <summary>
        /// Asserts that the input count matches the target count.
        /// </summary>
        /// <param name="inputName">The name of the input.</param>
        /// <param name="inputCount">The count of the input.</param>
        /// <param name="targetCount">The target count.</param>
        /// <returns>True if the assertion passes, otherwise false.</returns>
        protected bool AssertInput(string inputName, int inputCount, int targetCount)
        {
            if (inputCount != targetCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please check the size of the list: " + inputName);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Unsubscribes event handlers from the websocket object.
        /// </summary>
        protected void UnsubscribeEventHandlers()
        {
            if (this.wscObj != null)
            {
                try { this.wscObj.changed -= this.WscObjOnChanged; }
                catch { }
            }
        }

        /// <summary>
        /// Subscribes event handlers to the websocket object.
        /// </summary>
        protected void SubscribeEventHandlers()
        {
            this.wscObj.changed += this.WscObjOnChanged;
        }

        /// <summary>
        /// Event handler for when the websocket object changes.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        public void WscObjOnChanged(object sender, EventArgs e)
        {
            if (!this.online && ghDocument.SolutionState != GH_ProcessStep.Process && wscObj != null && !isAskingNewSolution)
            {
                Grasshopper.Instances.DocumentEditor.BeginInvoke((Action)delegate ()
                {
                    if (ghDocument.SolutionState != GH_ProcessStep.Process)
                    {
                        isAskingNewSolution = true;
                        this.onMessageTriggered = true;
                        this.ExpireSolution(true);
                        isAskingNewSolution = false;
                    }
                });
            }
        }
    }
}