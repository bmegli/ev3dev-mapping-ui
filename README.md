# ev3dev-mapping-ui
A cross-platform real-time 3D spatial data visualization working with [ev3dev-mapping-modules](https://github.com/bmegli/ev3dev-mapping-modules)

For high-level project overview visit [ev3dev-mapping web page](http://www.ev3dev.org/projects/2016/08/07/Mapping/)

For project meta-repository visit [ev3dev-mapping](https://github.com/bmegli/ev3dev-mapping)

## Supported Platforms

This is Unity project. In theory it should work on platforms ranging from desktops, through consoles to mobile devices and TVs.
See [Unity platforms](https://unity3d.com/unity/multiplatform) for the full list. Note that currently only some desktops were tested.
Other platforms may not work, need some tweaking or disabling some functionality.

## Installation Instructions

Required Unity Version: 5.6.2f1 or newer

1. Download [Unity](https://unity3d.com/)
2. Clone the project with submodules

    `git clone --recursive https://github.com/bmegli/ev3dev-mapping-ui`
3. Open the project in Unity

## Getting Started

The easiest way to start is working with recorded UDP communication:

1. Open the project in Unity
2. Open the Base scene
3. Hit `Play` button in Unity
4. Click `Replay` button in UI

If all went well you should see a moving yellow brick (yes, this is the robot for now) and the readings as they are collected. 

## Hardware

On EV3 follow Building Instructions for [ev3dev-mapping-modules](https://github.com/bmegli/ev3dev-mapping-modules)

### Summary

| Hardware                    | Connection | Unity Component     | EV3 module                 | Test Scene                        | First EV3 Step
| ----------------------------|------------|---------------------|----------------------------|-----------------------------------|------------------------
| 2 x EV3 Large Servo Motor   | outA, outD | Drive, Odometry     | ev3drive, ev3odometry      | TestingTheDrive WithOdometry      | `./ev3control 8004 500`
| Above + CruizCore gyroscope | in3        | Drive, DeadReconning| ev3drive, ev3dead-reconning| TestingTheDrive WithDeadReconning | `sudo ./TestingTheDriveWithDeadReconning.sh`                      
| WiFi dongle                 | wlan0      | WiFi                | ev3wifi                    | TestingTheWiFi                    | `./ev3control 8004 500`
| Neato XV11 Lidar            | in1, outC  | Laser               | ev3laser                   | TestingTheLidar                   | [Using the XV11 LIDAR ](http://www.ev3dev.org/docs/tutorials/using-xv11-lidar/#ev3dev-mapping)
| All above                   | as above   | all above*          | all above*                 | Base**                            | `sudo ./ev3init.sh`

*To test without gyroscope use Odometry component in place of DeadReconning. 

**Change `Replay` `Mode` to `None` for hardware testing

After First Step on EV3:
- follow printed instructions on EV3 (if any)
- on PC open ev3dev-mapping-ui in Unity 
- open corresponding Test Scene and select `Robot` game object 
- in `Network` component set `Host Ip` to your PC ip and 'Robot Ip' to your EV3 ip
- hit 'Play' button

### Mapping/Scanning

This section summarizes how to get result like in [3D mapping/scanning project with ev3dev OS and Unity UI](https://www.youtube.com/watch?v=9o_Fi8bHdvs).

#### Hardware

| Hardware                      | Connection               |
| ------------------------------|--------------------|
| WiFi dongle                   | USB hub            |
| EV3 Large Servo Motor (left)  | outA               |
| EV3 Large Servo Motor (right) | outD               | 
| Neato XV11 Lidar (horizontal) | outC, in1, USB hub | 
| Neato XV11 Lidar (vertical)   | outB, in2, USB hub |
| CruizCore gyroscope           | in3                |

#### Instructions

1. On PC follow Installation Instructions and Gettings Started for [ev3dev-mapping-ui](https://github.com/bmegli/ev3dev-mapping-ui)
2. On EV3 follow Building Instructions for [ev3dev-mapping-modules](https://github.com/bmegli/ev3dev-mapping-modules)
3. On PC open ev3dev-mapping-ui `Base` scene in Unity
    - select `Robot` game object
	- in `Robot Required` component change `Session Directory` (e.g. "session1")
	- in `Replay` component change `Mode` to `None`
    - in `Network` component set `Host Ip` to your PC ip
	- in `Network` component set `Robot Ip` to your EV3 ip
	- tweak other components and `Robot` children if your geometry differs 
4. On EV3 (through ssh/putty) run `ev3init` script and `ev3control`
``` bash
cd ev3dev-mapping-modules/bin
sudo ./ev3init.sh
./ev3control 8004 500

```
5. On PC hit play button in Unity

## Troubleshooting

1. If you have a laptop with both integrated and dedicated GPU make sure Unity is using the dedicated one. Unity likes to use the first if not forced.
2. If you want to build the project as standalone copy UDP folder to you build location (for replays)
3. If you have some problems read the Unity console output
4. If in Base scene floor and background are white instead of black make sure you are using Unity 5.4 or newer
5. If laser (hardware) fails just after starting let it spin for 15 seconds and warmup (from ssh) and only later hit "play".
