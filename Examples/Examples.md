# Examples

## 01: Scene setup & AR content

**Learning targets:** [Basic scene setup](#scene-setup), [different geometry display options](#geometry-display-options), [virtual reference locations](#virtual-reference-locations), [working with external triggers](#external-triggers)

**Required hardware:** 1x Computer running vizor server + grasshopper, 1x Microsoft hololens, printed [QR codes](../docs/QR_codes/)

### Scene setup

One of the core components for each vizor session is the websocket connection from the grasshopper script to the Vizor server.

The next step is to let grasshopper know about the AR worker to display content can be sent or tasks assigned.

One additional point of interest are QR codes to which content can be anchored. You can simply print out the markers on paper from the PDF's found here.

### Geometry display options

Content to be shown in augmented reality can be prepared with the help of three different components: `Scene mesh object`, `Scene wireframe object` and `Scene text object`.

Next to their content they can be assigned names and colours for display on the headset.

Downstream, one or multiple of these components are then combined into one `Scene Content` object which can in turn be sent to a Hololens with the `Scene model` component

### Virtual reference locations

Across all display options, the same target anchor properties are shared. This sets the location reference in virtual space relative to other objects.

For instance content can be attached to a marker, which makes this content glued to the marker's QR code. Alternatively, content can be anchored to an AR headset which makes it follow the users head position.

In grasshopper, the anchor origin is assumed to be the Rhino document origin and any transform of 3D content from that origin is reflected in the hololens.

Only `Text` which does not have an inherent 3D position has an additional reference plane input which is used as the origin.

### External triggers

Content on the AR headset can also be displayed based on a event-driven trigger with the `Scene Dynamic` component. It already sends the 3D content to headset, but waits for an external signal to show it.

This signal can also be sent with grasshopper with the help of the `Publish Topic` component. When the publish input is set to _True_ it sends out the specified message to the server.

## 02: Task creation and collaboration

**Learning targets**: [Task creation](#task-creation), [Worker skills](#worker-skills), [Collaboration setup](#collaboration-setup), [Parallel tasks vs Serial tasks](#parallel-vs-serial-tasks), [AR content _persistence_](#ar-content-persistence)

**Required hardware:** 1x Computer running vizor server + grasshopper, 2-3x Microsoft hololens, printed [QR codes](../docs/QR_codes/)

### Task creation

### Worker skills

Each worker can be assigned a set of skills which this worker is able to execute and is published to the server. With every newly created task a set of required skills can be added to it which are taken into account by the server for the assignment of the task to a suitable worker.

The input should be in the form of a stringified python dictionary, e.g. `{"lifting": 1, "heavy-lifting":0}` for enabling _lifting_ but not allowing _heavy-lifting_ for this worker.

### Collaboration setup

### Parallel vs. serial tasks

Tasks can be created in two different temporal relation. The default of tasks is to be `serial` which means that this task has to be finished before a new task will be published.

The other option is to set tasks to be `parallel` which means that the task can be executed at the same time by multiple workers.

This concept becomes most important in a collaboration setup with multiple workers because parallel tasks prevent the idling of workers. A series task becomes blocking for all other workers up until it is completed. Parallel tasks will be distributed across available workers.
If more parallel tasks are created than there are workers in the pool the remaining tasks will be distributed to workers when they finish their previous tasks.
Individual parallelized tasks get treated as parallel up until the next serial tasks comes in the task list.

### AR content persistence

On AR content components, you might have spotted the display rules input. With this, you can control the behaviour of the AR content over time. It can take the following values: [`persistent`](#persistent), [`session`](#session), [`step`](#step), [`flange`](#flange)

#### `Persistent`

Content set to persistent will always stay visible in the Vizor AR application up until it is removed again

#### `Session`

Session content will stay visible up until the current list of tasks is completed.

#### `Step`

All the content set to step is only visible while the task it has been assigned to is active.

#### `Flange`

This content is attached to the robotic flange and follows the movements of the robot.

## 03: Robot integration and collaboration

**Learning targets**: Robot path AR visualization, Human robot collaboration

**Required hardware:** 1x Computer running vizor server + grasshopper, 1x Microsoft hololens, printed [QR codes](../docs/QR_codes/)

**Required grasshopper plug-ins:** Virtual robot

**Optional harware:** Universal Robot UR10 for physical testing

### Robot path AR visualization

### Human Robot Collaboration

## Notes

- If you change the Rhino document units while the grasshopper document is already open, you need to go to the group `Scale with rhino unit` and click the `Re-check` button in order to attain the correct the scaling of the AR content.
