
namespace VizorLibs.MessageTypes
{
    public class ROSMessageWorkAreas : ROSMessage
    {
        new public WorkAreasMsg msg { get; set; }
    }

    /// <summary>
    /// One safety zone defined as a collection of work areas
    /// </summary>
    public class SafetyZoneMsg
    {
        public const string k_RosMessageName = "vizor_package/SafetyZone";
        public string identifier;
        public int[] zone_ids;
        public float alert_distance;
        public SceneGeometryMsg[] boundaries;

        public SafetyZoneMsg()
        {
            this.identifier = "null";
            this.alert_distance = 0;
            this.zone_ids = new int[0];
            this.boundaries = new SceneGeometryMsg[] {};
        }

        public SafetyZoneMsg(string _indentifier, int[] _zone_ids, float _alert_distance, SceneGeometryMsg[] _boundaries)
        {
            this.identifier = _indentifier;
            this.alert_distance = _alert_distance;
            this.zone_ids = _zone_ids;
            this.boundaries = _boundaries;
        }
    }

    public class WorkAreasMsg
    {
        public const string k_RosMessageName = "vizor_package/WorkAreas";
        public int[] id;
        public float[] size_x;
        public float[] size_y;
        public float[] centre_x;
        public float[] centre_y;

        public WorkAreasMsg(int length)
        {
            this.id = new int[length];
            this.size_x = new float[length];
            this.size_y = new float[length];
            this.centre_x = new float[length];
            this.centre_y = new float[length];
        }

        public WorkAreasMsg(int[] id, float[] size_x, float[] size_y, float[] centre_x, float[] centre_y)
        {
            this.id = id;
            this.size_x = size_x;
            this.size_y = size_y;
            this.centre_x = centre_x;
            this.centre_y = centre_y;
        }
    }
}