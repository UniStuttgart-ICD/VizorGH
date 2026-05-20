using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VizorLibs.MessageTypes
{
    public class ROSMessageRegisterSensor : ROSMessage
    {
        new public RegisterSensorMsg msg { get; set; }
    }

    public class RegisterSensorMsg
    {
        public const string k_RosMessageName = "vizor_package/RegisterSensor";

        public string sensor_topic;
        public string[] devices;
        public SceneContentMsg content;
        public float value_min;
        //  expected minimum value
        public float value_max;
        //  expected maximum value
        public bool update_text;
        public string text_prefix;
        //  text to display before the value
        public string text_suffix;
        //  text to display after the value
        public string color_min;
        //  empty for no change
        public string color_max;
        //  empty for no change
        public float scale_min;
        //  1.0 for no change
        public float scale_max;
        //  1.0 for no change

        public RegisterSensorMsg()
        {
            this.sensor_topic = "";
            this.devices = new string[0];
            this.content = new SceneContentMsg();
            this.value_min = 0.0f;
            this.value_max = 0.0f;
            this.update_text = false;
            this.text_prefix = "";
            this.text_suffix = "";
            this.color_min = "";
            this.color_max = "";
            this.scale_min = 1.0f;
            this.scale_max = 1.0f;
        }

        public RegisterSensorMsg(string sensor_topic, string[] devices, SceneContentMsg content, float value_min, float value_max, bool update_text, string text_prefix, string text_suffix, string color_min, string color_max, float scale_min, float scale_max)
        {
            this.sensor_topic = sensor_topic;
            this.devices = devices;
            this.content = content;
            this.value_min = value_min;
            this.value_max = value_max;
            this.update_text = update_text;
            this.text_prefix = text_prefix;
            this.text_suffix = text_suffix;
            this.color_min = color_min;
            this.color_max = color_max;
            this.scale_min = scale_min;
            this.scale_max = scale_max;
        }


    }
}
