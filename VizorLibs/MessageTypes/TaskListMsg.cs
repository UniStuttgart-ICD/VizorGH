
namespace VizorLibs.MessageTypes
{
    public class ROSMessageTaskList : ROSMessage
    {
        new public TaskListMsg msg { get; set; }
    }

    public class TaskListMsg
    {
        public const string k_RosMessageName = "vizor_package/TaskList";
        public int[] ids;
        public string[] targets;
        public int[] deadlines;
        public string[] names;

        public TaskListMsg()
        {
            this.ids = new int[0];
            this.targets = new string[0];
            this.deadlines = new int[0];
            this.names = new string[0];
        }

        public TaskListMsg(int[] ids, string[] targets, int[] deadlines, string[] names)
        {
            this.ids = ids;
            this.targets = targets;
            this.deadlines = deadlines;
            this.names = names;
        }

    }
}