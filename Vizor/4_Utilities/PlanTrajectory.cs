using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Vizor._1_System;
using Vizor._3_Robot;
using VizorLibs;
using VizorLibs.MessageTypes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Vizor._3_Machine
{
    /// <summary>  
    /// The PlanTrajectory component is responsible for generating and executing motions for a robot.  
    /// It interacts with a robot object, processes target poses, and communicates with a server to plan, request,  
    /// and execute motion trajectories.  
    ///  
    /// Inputs:  
    /// - Robot (Generic): The registered robot object to be used for planning and execution.  
    /// - Target Pose (Plane List): A list of target planes. A single target assumes free motion, while multiple targets plan a Cartesian motion.  
    /// - Path Name (Text): Identifier for the motion path.  
    /// - Plan (Boolean): Trigger to initiate the planning process.  
    /// - Execute (Boolean): Trigger to execute the planned trajectory.  
    /// - Request (Boolean): Trigger to request the planned path from the server.  
    ///  
    /// Outputs:  
    /// - Output (Text): Status messages indicating the current state or errors.  
    /// - Trajectory Object (Generic): The generated robot trajectory object.  
    /// - Trajectory Mesh (Mesh): A mesh representation of the trajectory for visualization.  
    /// - Trajectory Values (Number Tree): Joint values of the trajectory in a tree structure.  
    ///  
    /// Note: 
    /// This component wraps the MoveIt plan motion interface. 
    /// For programmatically generating motion plans, the Compas-Fab interface is recommended.  
    /// 
    /// This component is designed to work with the Vizor system and integrates with ROS for motion planning.  
    /// </summary>
    
    public class PlanTrajectory : VizorBaseComponent
    {
        private RobotObject robot;
        private string path_name;
        private List<Plane> targetPlanes;
        private bool plan;
        private bool execute;
        private bool request;
        //private SceneGeometryObject geom;
        private bool planned = false;
        private RobotTrajectoryObject trajObj;

        /// <summary>
        /// Initializes a new instance of the PlanMotion class.
        /// </summary>
        public PlanTrajectory()
          : base("Generate Motion", "Plan",
              "Description", "4_Utilities")
        {
            isListener = true;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Robot", "Robot", "registed robot object", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Target Pose", "Target", 
                "when a single target is supplied a free motion is assumed, otherwise a cartesian motion will be planned", GH_ParamAccess.list);
            pManager.AddTextParameter("Path Name", "Name", "identifier for the motion", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Attached Geometry", "Attachment", "mesh showing what is attached at the ee", GH_ParamAccess.item);
            
            pManager.AddBooleanParameter("Plan", "Plan", "press to plan", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Execute", "Execute", "press to execute", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Request", "Request", "press to request this path from the server", GH_ParamAccess.item, false);
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Out", "status output", GH_ParamAccess.item);
            pManager.AddGenericParameter("Trajectory Object", "Trajectory", "robot trajectory", GH_ParamAccess.item);
            pManager.AddMeshParameter("Trajectory Mesh", "Path Mesh", "mesh of the trajectory", GH_ParamAccess.item);
            pManager.AddNumberParameter("Trajectory Values", "Joint Values", "joint values of the trajectory", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsDocumentActive()) return;

            if (!this.onMessageTriggered)
            {
                // Set robot object
                RobotObject _robot = new RobotObject();
                if (DA.GetData("Robot", ref _robot))
                {
                    if (_robot != this.robot)
                    {
                        this.online = true;
                        this.robot = _robot;
                        if (UpdateDevice((Device)_robot))
                            DA.SetData(0, "device updated\nlast updated on " + DateTime.Now.ToString());
                    }
                }
                else
                {
                    CleanupConnection();
                    DA.SetData(0, "no connection");
                    return;
                }

                targetPlanes = new List<Plane>();
                DA.GetDataList("Target Pose", targetPlanes);
                DA.GetData("Plan", ref plan);
                DA.GetData("Execute", ref execute);
                DA.GetData("Request", ref request);
                DA.GetData("Path Name", ref path_name);
                //DA.GetData(1, ref geom);

                if (targetPlanes.Count == 1)
                {
                    this.Message = "free";
                    if (plan)
                    {
                        PlanningFreeMsg planningMessage = new PlanningFreeMsg
                        {
                            name = path_name,
                            target_pose = RobotLibraryConverter.PlaneToPose(targetPlanes[0])
                        };
                        this.online = false;
                        ROSMessageHandler.PublishPlanRequest(this.wscObj, robot.name, planningMessage);
                    }
                }
                else
                {
                    this.Message = "cartesian";
                    if (plan)
                    {
                        BuiltInMsg.Pose[] targetPoses = new BuiltInMsg.Pose[targetPlanes.Count];
                        for (int i = 0; i < targetPlanes.Count; i++)
                        {
                            targetPoses[i] = RobotLibraryConverter.PlaneToPose(targetPlanes[i]);
                        }
                        PlanningCartesianMsg planningMessage = new PlanningCartesianMsg
                        {
                            name = path_name,
                            poses = targetPoses,
                        };
                        this.online = false;
                        ROSMessageHandler.PublishPlanRequest(this.wscObj, robot.name, planningMessage);
                    }
                }

                if (request)
                {
                    this.wscObj = robot.wscObj;
                    this.online = false;
                    ROSMessageHandler.PublishPathRquestCommand(this.wscObj, robot.name, path_name);
                }

                if (planned && execute)
                {
                    this.wscObj = robot.wscObj;
                    ROSMessageHandler.PublishExecutionCommand(this.wscObj, robot.name, path_name);
                }
            }

            else
            {
                this.onMessageTriggered = false;

                if (this.wscObj.message != null)
                {
                    RobotTrajectoryObject _trajObj = ROSMessageHandler.ParseTrajectory(robot, path_name, this.wscObj.message);
                    if (_trajObj != null)
                    {
                        trajObj = _trajObj;
                        planned = true;
                        this.online = true;
                    }
                }
            }

            // process trajObj and output the frames and mesh visualisation
            if (trajObj != null)
            {
                DA.SetData(1, trajObj);

                GH_Structure<GH_Number> traj_data = new GH_Structure<GH_Number>();
                int i = 0;
                foreach (float[] list in trajObj.joint_trajectory)
                {
                    GH_Path path = new GH_Path(i);
                    i += 1;
                    List<GH_Number> branch = new List<GH_Number>();
                    foreach (float v in list)
                    {
                        branch.Add(new GH_Number(v));
                    }
                    traj_data.AppendRange(branch, path);
                }
                DA.SetDataTree(3, traj_data);

                DA.SetData("Trajectory Mesh", trajObj.gMesh);
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
                return Vizor.Properties.Resources.Plan_Trajectory;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("54a527c3-8a74-4c1b-aed0-00427a87adbd"); }
        }
    }
}