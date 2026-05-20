
# FAQ

## Grasshopper

- In the case that a tasklist has been properly created, but is not starting to run, make sure that all AR workers which are active in the **server worker pool** have sent an initial acknowledge signal. When switching from a script with multiple AR-workers to a script with only one, adjusting the vizor server configuration and restarting it might be required.
- If the server assigns tasks to other workers than set in grasshopper make sure that the set of skills of the worker matches the skills required by the tasks. If there is a mismatch the server assigns the tasks to another worker with suitable skillset.
- If you are working on MacOS and the vizor plug-in is not showing in the bar, make sure that Rhino is run with `Rosetta` enabled.

## Vizor server / infrastructure

- The following ports need to be opened on the computer hosting the server and reachable from other devices in the network:
  - **9090** : server-grasshopper bridge
  - **10000** : `HOLO1`
  - **10001** : `HOLO2`
  - **10002** : `HOLO3`
  - **10003** : `HOLO4`

## HoloLens

- The WiFi the HoloLens is connected to needs to be set to a **private network** rather than  a public one. When joining a new network, it defaults to _public_ and needs to be changed in the Network settings in order to connect to the server. This can be done in the network settings page.
- Make sure to set the device ID in the network tab of the vizor network settings. It takes an integer from 1 to 4 and is the respective identifier for the HoloLens, e.g. _2_ translates to a device identifier of `HOLO2`
- All QR codes need to be confirmed with a tap in the HoloLens for them to be enabled. Whenever they are re-positioned, their new location needs to be confirmed again with another tap for the new position to take effect.
- The provided QR codes need to have a white border left around them for reliable identification.
- The recognition of the QR codes on the HoloLens has the scale of them baked in and only allows deviation to a certain extent. The provided print files assume an A4 paper size and might need to be resized when changing format.
