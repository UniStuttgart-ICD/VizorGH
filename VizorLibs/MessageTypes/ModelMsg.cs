using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VizorLibs.MessageTypes
{
    public class ROSMessageModel : ROSMessage
    {
        new public ModelMsg msg { get; set; }
    }

    public class ModelMsg
    {
        public const string k_RosMessageName = "vizor_package/ModelMsg";
        public string[] names { get; set; }
        public BuiltInMsg.Pose[] poses { get; set; }

        public ModelMsg()
        {
            this.names = new string[0];
            this.poses = new BuiltInMsg.Pose[0];
        }
        public ModelMsg(string[] names, BuiltInMsg.Pose[] poses)
        {
            this.names = names;
            this.poses = poses;
        }
    }
}
