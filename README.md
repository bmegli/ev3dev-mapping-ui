# ev3dev-mapping-ui
A cross-platform real-time 3D spatial data visualization working with [ev3dev-mapping-modules](https://github.com/bmegli/ev3dev-mapping-modules)

## Supported Platforms

This is Unity project. In theory it should work on platforms ranging from desktops, through consoles to mobile devices and TVs.
See [Unity platforms](https://unity3d.com/unity/multiplatform) for the full list. Note that currently only some desktops were tested.
Other platforms may not work, need some tweaking or disabling some functionality.

## State of the Project

This is preliminary version. It's functional and stable but all programming interfaces are subject to change.
Some architectural changes are also on the way and Control component/ev3control module are currently rewritten for TCP/IP.

## Current Functionality 

1. Real time 3D mapping 
2. Robot engines control 
3. Recording/replaying UDP communication
4. Exporting point clouds to [ply file format](https://en.wikipedia.org/wiki/PLY_(file_format)) for further processing

## Hardware Requirements

The project can be tested without hardware (robot) with recorded UDP communication. See details below.

To test with robot hardware:
- XV11 Lidar works with Laser component and ev3lidar module (plotting only current readings)
- 2 engines and CruizCore XG 1300L work with Odometry component and ev3odometry module (plotting only robot movement)
- if you have all of the above mapping (2D or 3D) is possible.
- 2 engines work with Drive component and ev3drive module (control of robot)

If you want to use it with different hardware (for now) you have to modify existing [ev3dev-mapping-module](https://github.com/bmegli/ev3dev-mapping-modules)
or write your own and its counterpart in UI.

## Getting Started

Only instructions for working with recorded UDP communication for now.

1. Download [Unity](https://unity3d.com/)
2. Clone the project with submodules

    `git clone --recursive https://github.com/bmegli/ev3dev-mapping-ui`
3. Open the project in Unity
4. Open the Base scene
5. Hit `Play` button in Unity
6. Click `Replay` button in UI

If all went well you should see a moving yellow brick (yes, this is the robot for now) and the readings as they are collected. 

## Further Steps

Select the `Robot` Game Object in the hierarchy window. See the components (Odometry, Laser, Drive, Control).
See the components configuration (e.g. drive models, udp parameters, replay mode, laser geometry, laser plot, etc.)
Try to change the replay files to some other from UDP folder.
Wait for some documentation on building modules in [ev3dev-mapping-modules](https://github.com/bmegli/ev3dev-mapping-modules).
Build the ev3drive module, configure Drive component accordingly and try to control robot with 2 engines from within Unity.

## Troubleshooting

1. If you have a laptop with both integrated and dedicated GPU make sure Unity is using the dedicated one. Unity likes to use the first if not forced.
2. If you want to build the project as standalone choose the Unity project folder as destination or copy UDP folder to you build location (for replays)
3. If you have some problems read the Unity console output
