
namespace VizorLibs.MessageTypes
{
    public class ROSMessagePlanningGeometry : ROSMessage
    {
        new public PlanningGeometryMsg msg { get; set; }
    }
    public class ROSMessagePlanningCartesian : ROSMessage
    {
        new public PlanningCartesianMsg msg { get; set; }
    }
    public class ROSMessagePlanningFree : ROSMessage
    {
        new public PlanningFreeMsg msg { get; set; }
    }

    public class PlanningGeometryMsg
    {
        public const string k_RosMessageName = "vizor_package/PlanningGeometry";
        public string operation { get; set; }
        public string name { get; set; }
        public BuiltInMsg.MeshMsg mesh { get; set; }

        public PlanningGeometryMsg()
        {
            this.operation = "";
            this.name = "";
            this.mesh = new BuiltInMsg.MeshMsg();
        }

        public PlanningGeometryMsg(string operation, string name, BuiltInMsg.MeshMsg mesh)
        {
            this.operation = operation;
            this.name = name;
            this.mesh = mesh;
        }
    }


    public class PlanningCartesianMsg
    {
        public const string k_RosMessageName = "vizor_package/PlanningCartesian";
        public string name { get; set; }
        public BuiltInMsg.Pose[] poses { get; set; }

        public PlanningCartesianMsg()
        {
            this.name = "";
            this.poses = new BuiltInMsg.Pose[0];
        }

        public PlanningCartesianMsg(string name, BuiltInMsg.Pose[] poses)
        {
            this.name = name;
            this.poses = poses;
        }
    }


    public class PlanningFreeMsg
    {
        public const string k_RosMessageName = "vizor_package/PlanningFree";
        public string name { get; set; }
        public BuiltInMsg.Pose target_pose { get; set; }

        public PlanningFreeMsg()
        {
            //this.name = "";
            //this.target_pose = new BuiltInMsg.Pose();
        }

        public PlanningFreeMsg(string name, BuiltInMsg.Pose target_pose)
        {
            this.name = name;
            this.target_pose = target_pose;
        }
    }
}
