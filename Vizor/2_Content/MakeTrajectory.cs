using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Vizor._3_Robot;
using VizorLibs;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Vizor._1_System
{
    /// <summary>
    /// The MakeTrajectory component is responsible for creating a robot trajectory object.  
    /// It supports both Cartesian and joint space trajectory definitions, allowing users to define  
    /// the trajectory either as a list of tool center point (TCP) frames or as a tree of joint values.  
    ///  
    /// Inputs:  
    /// - Target Robot: The robot client to associate the trajectory with.  
    /// - ParamSpace (IsCartesian): Boolean to toggle between Cartesian and joint space inputs.  
    /// - TCP Frames: List of planes defining the TCPs (used in Cartesian space).  
    /// - Joint Values: Tree of joint values for trajectory points (used in joint space).  
    /// - Trajectory Width: Width of the trajectory visualization mesh.  
    ///  
    /// Outputs:  
    /// - Output: Status message describing the trajectory creation process.  
    /// - Trajectory Object: The generated RobotTrajectoryObject.  
    /// - Trajectory Mesh: A mesh representation of the trajectory for visualization.  
    ///  
    /// This component dynamically updates its input parameter names and descriptions based on  
    /// the selected input space (Cartesian or joint). It also validates the input data and provides  
    /// error messages if the inputs are invalid.  
    /// </summary>
    
    public class MakeTrajectory : GH_Component
    {
        private RobotObject target;
        private double width;
        private string output;
        private bool isCartesian = true; //default to cartesian inputs (virtual robot)
        private List<float[]> joint_trajectory;
        private List<Plane> gTrajectoryFrames;
        private Mesh gMesh;
        private RobotTrajectoryObject trajObject;

        /// <summary>
        /// Initializes a new instance of the MakeTrajectory class.
        /// </summary>
        public MakeTrajectory()
          : base("Robot Trajectory Object", "Trajectory",
              "Create a trajectory object for a robot or machine. ",
              "VizorGH", "2_Content")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Target Robot", "R", "robot client", GH_ParamAccess.item);
            pManager.AddBooleanParameter("ParamSpace", "IsCartesian", "true if trajectory points are provided in cartesian space, false if in joint space", GH_ParamAccess.item, true);
            pManager.AddPlaneParameter("TCP Frames", "F", "list of planes defining the target tool centre points (TCPs)", GH_ParamAccess.list); //cartesian space
            pManager.AddNumberParameter("Joint Values", "V", "tree of joint values for each trajectory point", GH_ParamAccess.tree); //joint space
            pManager.AddNumberParameter("Trajectory Width", "W", "width of the trajectory visualisation [m]", GH_ParamAccess.item, 0.01);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Outputs a status message, the trajectory object, and a mesh pipe for visualising the trajectory. 
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Out", "status output", GH_ParamAccess.item);
            pManager.AddGenericParameter("Trajectory Object", "Trajectory", "robot trajectory", GH_ParamAccess.item);
            pManager.AddMeshParameter("Trajectory Mesh", "Path Mesh", "mesh of the trajectory", GH_ParamAccess.item);
        }

        private void updateParamName(bool isCartesian)
        {
            
            if (!isCartesian)
            {
                Params.Input[2].Name = "---";
                Params.Input[2].NickName = "---";
                Params.Input[2].Description = "invalid input (set 'IsCartesian' to true to use TCP inputs)";
                Params.Input[3].Name = "Joint Values";
                Params.Input[3].NickName = "Joints";
                Params.Input[3].Description = "tree of joint values for each trajectory point";
            }
            else
            {
                Params.Input[3].Name = "---";
                Params.Input[3].NickName = "---";
                Params.Input[3].Description = "invalid input (set 'IsCartesian' to false to use joint trajectory inputs)";
                Params.Input[2].Name = "TCP Frames";
                Params.Input[2].NickName = "TCPs";
                Params.Input[2].Description = "list of planes defining the target tool centre points (TCPs)";
            }
            Params.OnParametersChanged();
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref target);
            if (target is RobotObject){
                output = target.name + " trajectory";
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "You did not add a correct robot client");
                this.Message = "no robot";
                return;
            }

            //List<IGH_Goo> points = new List<IGH_Goo>();
            //DA.GetDataList(1, points);

            // parse trajectory data
            DA.GetData(1, ref isCartesian);
            updateParamName(isCartesian);
            if (isCartesian)
            {
                // TCP input as GH frames
                gTrajectoryFrames = new List<Plane>();
                this.Message = "cartesian";
                DA.GetDataList(2, gTrajectoryFrames);
                //the cartesian planes should be transformed according to the robot base
                joint_trajectory = RobotLibraryConverter.ghFramesToJointTrajectory(gTrajectoryFrames, target);

                if (joint_trajectory.Count == 0)
                {
                    this.Message = "No Path";
                    return;
                }
            }
            else
            {
                // Pre-computed joint trajectory points
                joint_trajectory = new List<float[]>();
                this.Message = "joint";
                GH_Structure<GH_Number> inputDataTree = new GH_Structure<GH_Number>();
                if (!DA.GetDataTree(3, out inputDataTree)) return;
                foreach (GH_Path p in inputDataTree.Paths)
                {
                    List<GH_Number> branch = inputDataTree.get_Branch(p) as List<GH_Number>;
                    if (branch == null) continue;

                    float[] array = new float[branch.Count];
                    for (int i = 0; i < branch.Count; i++)
                    {
                        array[i] = (float)branch[i].Value;
                    }
                    joint_trajectory.Add(array);
                }

                gTrajectoryFrames = RobotLibraryConverter.jointTrajectoryToGhFrames(joint_trajectory, target);
            }

            output += " is created with " + joint_trajectory.Count.ToString();
            output += " points in " + (isCartesian ? "cartesian" : "joint") + " space";

            // create trajectory mesh
            DA.GetData(4, ref width);
            Curve path = new PolylineCurve(this.gTrajectoryFrames.Select(f => f.Origin).ToArray());
            gMesh = Mesh.CreateFromCurvePipe(path, width, 6, 1, MeshPipeCapStyle.Dome, true);
            DA.SetData("Trajectory Mesh", gMesh);

            // create RobotTrajectoryObject
            trajObject = new RobotTrajectoryObject
            {
                robot = this.target,
                gTrajectoryFrames = this.gTrajectoryFrames,
                joint_trajectory = this.joint_trajectory,
                gMesh = this.gMesh,
            };
            DA.SetData("Trajectory Object", trajObject);
            DA.SetData("Output", output);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Vizor.Properties.Resources.Trajectory;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9a03197b-3b8b-492e-93f1-32baf9fd7a12"); }
        }
    }
}