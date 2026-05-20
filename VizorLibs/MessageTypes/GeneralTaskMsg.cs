
namespace VizorLibs.MessageTypes
{
    public class ROSMessageTask : ROSMessage
    {
        new public GeneralTaskMsg msg { get; set; }
    }

    public class GeneralTaskMsg
    {
        public const string k_RosMessageName = "vizor_package/GeneralTask";
        public int id; // ID number
        public string target; // name of the preferred actor
        public string type; // parallel/normal(sequential) + shared/individual
        public string skill; // comma-separated values for skill description
        public int deadline; // in seconds
        public string name; // task name displayed
        public string instruction; // instructions for humans
        public RobotTrajectoryMsg trajectory; // robot motion data, empty if none
        public SafetyZoneMsg zone; // safety zone data, empty if none
        public SceneContentMsg content; // AR content for visualisation, empty if none

        public GeneralTaskMsg(GeneralTaskObject task)
        {
            this.id = task.id;
            this.target = task.gTarget.name;
            this.type = task.type;
            this.skill = task.skill;
            this.deadline = task.deadline;
            this.name = task.name;
            this.instruction = task.instruction;
            this.trajectory = task.GetTrajectoryMessages();
            this.zone = task.GetSafetyZoneMessage();
            this.content = task.GetSceneContentMsg();
        }

        public GeneralTaskMsg(int id, string target, string type, string skill, int deadline, string name, string instruction, 
            RobotTrajectoryMsg trajectory, SafetyZoneMsg zone, SceneContentMsg content)
        {
            this.id = id;
            this.target = target;
            this.type = type;
            this.skill = skill;
            this.deadline = deadline;
            this.name = name;
            this.instruction = instruction;
            this.trajectory = trajectory;
            this.zone = zone;
            this.content = content;
        }
    }
}
