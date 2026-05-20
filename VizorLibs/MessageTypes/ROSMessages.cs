
namespace VizorLibs.MessageTypes
{
    // TODO: build ros service interface

    //public class ROSServiceRequest
    //{
    //    public string op = "call_service";
    //    public string service { get; set; }
    //    public string args { get; set; }
    //}

    //public class ROSPlanFreeRequest : ROSServiceRequest
    //{
    //    new public PlanningFreeMsg args { get; set; }
    //}
    //public class ROSPlanCartesianRequest : ROSServiceRequest
    //{
    //    new public PlanningCartesianMsg args { get; set; }
    //}

    //public class ROSServiceResponse
    //{
    //    public string op { get; set; }
    //    public string service { get; set; }
    //    public string values { get; set; }
    //    public bool result { get; set; }
    //}

    //public class ROSPlanFreeResponse : ROSServiceResponse
    //{
    //    new public RobotTrajectoryMsg values { get; set; }
    //}

    public class ROSMessage
    {
        public string op { get; set; }
        public string topic { get; set; }
        public string type { get; set; }
        public string msg { get; set; }
    }
}