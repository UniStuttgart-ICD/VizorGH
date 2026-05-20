// This file defines the Robot class, which represents a robot in the system.

using System;
using System.Collections.Generic;
using Vizor._1_System;
using Grasshopper.Kernel;
using ICD.VirtualRobot.Core;
using Vizor.Properties;
using VizorLibs;
using SphericalWrist6AxisRobot = ICD.VirtualRobot.Core.SphericalWrist6AxisRobot;
using NonSphericalWrist6AxisRobot = ICD.VirtualRobot.Core.NonSphericalWrist6AxisRobot;

namespace Vizor._3_Robot
{
    /// <summary>
    /// The Robot component is responsible for registering a robot in the system.  
    ///  
    /// Inputs:  
    /// - Robot Name: Identifier of the robot (e.g., PINP, GNM).  
    /// - Virtual Robot Model: The virtual robot model to be used.  
    /// - Axis Toggles: A list of booleans to constrain axes.  
    /// - Joint Names: Identifiers for the robot's joints.  
    ///  
    /// Outputs:  
    /// - Robot Object: The registered robot object.  
    /// </summary>
    
    public class Robot : DeviceComponent
    {
        private RobotObject robot;
        //private object robotModel;
        private SphericalWrist6AxisRobot robotModelS;
        private NonSphericalWrist6AxisRobot robotModelNS;
        private List<bool> axisToggles;
        private List<string> joint_names;
        
        /// <summary>
        /// Initializes a new instance of the Robot class.
        /// </summary>
        public Robot()
          : base("Robot", "Robot",
              "Register a robot to be used in the system", "1_Object")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddTextParameter("Robot Name", "Name", "identifier of the robot (e.g. PINP, GNM)", GH_ParamAccess.item, "KUKA");
            pManager.AddGenericParameter("Virtual Robot Model", "Robot", "VirtualRobot model (spherical)", GH_ParamAccess.item);
            pManager.AddGenericParameter("Virtual Robot Model NS", "RobotNS", "VirtualRobot model (non spherical)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Axis Toggles", "Toggle", "constrain axis", GH_ParamAccess.list, new List<bool>(){false,false,false});
            pManager.AddTextParameter("Link Names", "Links", "identifier of the robot links (refer to your URDF)", GH_ParamAccess.list, new List<string>(){"base_link", "link_1", "link_2", "link_3", "link_4", "link_5"});
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);
            pManager.AddGenericParameter("Robot Object", "Robot", "registered robot object", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            base.SolveInstance(DA);
            DA.SetData(1, this.disabled ? null : robot);
        }

        /// <summary>
        /// Initializes the device for the robot by the specified name.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// <returns>The initialized device.</returns>
        protected override Device InitDevice(IGH_DataAccess DA)
        {
            //object model = null;
            //DA.GetData(3, ref model);
            //if (model is SphericalWrist6AxisRobot || model is NonSphericalWrist6AxisRobot)
            //{
            //    robotModel = model;
            //}
            //else
            //{
            //    robotModel = null;
            //}
            //DA.GetData(3, ref robotModel);
            robotModelNS = null;
            robotModelS = null;
            DA.GetData(3, ref robotModelS);
            DA.GetData(4, ref robotModelNS);
            if ((robotModelNS != null) && (robotModelS != null))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "two robots provided at input! please remove one.");
            }
            if ((robotModelNS == null) && (robotModelS == null))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "no robot provided at input! please add one.");
            }

            axisToggles = new List<bool>();
            DA.GetDataList(5, axisToggles);

            joint_names = new List<string>();
            DA.GetDataList(6, joint_names);

            robot = new RobotObject()
            {
                name = this.deviceName,
                wscObj = this.wscObj,
                virtualRobotObjectS = robotModelS,
                virtualRobotObjectNS = robotModelNS,
                axisToggles = this.axisToggles,
                joint_names = this.joint_names.ToArray()
            };
            return robot;
        }

        /// <summary>
        /// Updates the device associated with the component.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void UpdateDevice(IGH_DataAccess DA)
        {
            //object model = null;
            //DA.GetData(3, ref robotModel);
            robotModelNS = null;
            robotModelS = null;
            DA.GetData(3, ref robotModelS);
            DA.GetData(4, ref robotModelNS);
            if ((robotModelNS != null) && (robotModelS != null))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "two robots provided at input! please remove one.");
            }
            if ((robotModelNS == null) && (robotModelS == null))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "no robot provided at input! please add one.");
            }

            axisToggles = new List<bool>();
            DA.GetDataList(5, axisToggles);
            joint_names = new List<string>();
            DA.GetDataList(6, joint_names);

            this.robot.joint_names = joint_names.ToArray();
            this.robot.axisToggles = axisToggles;
            this.robot.virtualRobotObjectS = robotModelS;
            this.robot.virtualRobotObjectNS = robotModelNS;
        }

        /// <summary>
        /// Clears the topics associated with the robot.
        /// </summary>
        protected override void ClearTopics()
        {
            if ((this.wscObj == null) || (!this.wscObj.isConnected()) || (deviceName == "")) return;
            ROSMessageHandler.Unsubscribe(this.wscObj, deviceName + "_Command", "std_msgs/String");
            ROSMessageHandler.Unsubscribe(this.wscObj, deviceName + "/request/planned_path", "vizor_package/PlannedTrajectory");

            registered = false;
            this.Message = "Disabled";
        }

        /// <summary>
        /// Subscribes to topics targeting the robot.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void InitTopics(IGH_DataAccess DA)
        {
            if ((this.wscObj == null) || (!this.wscObj.isConnected()) || (deviceName == "")) return;

            ROSMessageHandler.Subscribe(this.wscObj, deviceName + "_Command", "std_msgs/String");
            ROSMessageHandler.Subscribe(this.wscObj, deviceName + "/request/planned_path", "vizor_package/PlannedTrajectory");
            //ROSMessageHandler.Advertise(this.wscObj, deviceName + "_Move", "geometry_msgs/Twist");
            ROSMessageHandler.Advertise(this.wscObj, deviceName + "_Trajectory", "vizor_package/RobotPlatformTrajectory");
            ROSMessageHandler.Advertise(this.wscObj, deviceName + "/scene/update", "vizor_package/PlanningGeometry");
            
            // ad-hoc control
            ROSMessageHandler.Advertise(this.wscObj, deviceName + "/request/cartesian", "vizor_package/PlanningCartesian");
            ROSMessageHandler.Advertise(this.wscObj, deviceName + "/request/free", "vizor_package/PlanningFree");
            ROSMessageHandler.Advertise(this.wscObj, deviceName + "/request/stored", "std_msgs/String");
            ROSMessageHandler.Advertise(this.wscObj, deviceName + "/command/execute", "std_msgs/String");
            ROSMessageHandler.Advertise(this.wscObj, deviceName + "/command/simulate", "std_msgs/String");
            
            // task-based control
            ROSMessageHandler.Advertise(this.wscObj, this.robot.name + "/task/execute", "vizor_package/GeneralTask");
            ROSMessageHandler.Advertise(this.wscObj, this.robot.name + "/task/simulate", "vizor_package/GeneralTask");
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
                return Resources.Robot;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("afb86c19-713d-45b6-873c-27b22aab4f95"); }
        }
    }
}