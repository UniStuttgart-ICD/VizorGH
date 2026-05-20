// This file contains utility methods for managing tasks and devices in the Vizor system.

using System;
using System.Collections.Generic;
using Vizor._1_System;
using VizorLibs;
using VizorLibs.MessageTypes;

namespace Vizor._4_Task
{
    public static class TaskUtilities
    {
        /// <summary>
        /// Generates a task list message from a list of general task objects.
        /// </summary>
        /// <param name="tasks">The list of general task objects.</param>
        /// <returns>A task list message containing IDs, targets, deadlines, and names.</returns>
        public static TaskListMsg GenerateTaskList(List<GeneralTaskObject> tasks)
        {
            int num = tasks.Count;
            int[] ids = new int[num];
            string[] names = new string[num];
            string[] targets = new string[num];
            int[] deadlines = new int[num];
            for (int i = 0; i < tasks.Count; i++)
            {
                ids[i] = tasks[i].id;
                targets[i] = tasks[i].gTarget.name;
                names[i] = tasks[i].name;
                deadlines[i] = tasks[i].deadline;
            }
            return new TaskListMsg(ids, targets, deadlines, names);
        }

        /// <summary>
        /// Retrieves a list of devices associated with the given tasks.
        /// </summary>
        /// <param name="tasks">The list of general task objects.</param>
        /// <returns>A list of devices.</returns>
        public static List<Device> GetDevicesForTasks(List<GeneralTaskObject> tasks)
        {
            List<Device> targetDevices = new List<Device>();

            foreach (GeneralTaskObject t in tasks)
            {
                if (!targetDevices.Contains(t.gTarget))
                    targetDevices.Add(t.gTarget);
            }

            return targetDevices;
        }

        /// <summary>
        /// Retrieves a list of device names associated with the given tasks.
        /// </summary>
        /// <param name="tasks">The list of general task objects.</param>
        /// <returns>A list of device names.</returns>
        public static List<string> GetDeviceNamesForTasks(List<GeneralTaskObject> tasks)
        {
            List<string> targetDevices = new List<string>();

            foreach (GeneralTaskObject t in tasks)
            {
                if (!targetDevices.Contains(t.gTarget.name))
                    targetDevices.Add(t.gTarget.name);
            }

            return targetDevices;
        }
    }
}
