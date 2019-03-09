/*
 * Copyright (C) 2019 Bartosz Meglicki <meglickib@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 * This program is distributed "as is" WITHOUT ANY WARRANTY of any
 * kind, whether express or implied; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ev3devMapping
{
    public enum VideoCameraStream {Color, Infrared};

    [Serializable]
    public class VideoCameraModuleProperties : ModuleProperties
    {
        public VideoCameraStream stream = VideoCameraStream.Color;
        public int width = 640;
        public int height = 360;
        public int framerate = 30;
        public string device = "/dev/dri/renderD128";
        public int bitrate = 1000000;
    }


    public class VideoCamera : RobotModule
    {
        public VideoCameraModuleProperties module;

        protected override void Awake()
        {
            base.Awake();
            print(name + " - awaiting client " + network.robotIp + " on " + network.hostIp + ":" + moduleNetwork.port);
        }
            	
        // Update is called once per frame

        #region RobotModule 

        public override string ModuleCall()
        {
            //./realsense-nhve 192.168.0.125 9766 color 640 360 30 50 /dev/dri/renderD128 500000
            // TO DO hardcoded stream time (1000)
            string stream = module.stream == VideoCameraStream.Color ? "color" : "infrared";
            return "realsense-nhve " + network.hostIp + " " + moduleNetwork.port + " " + stream + " "
                + module.width + " " + module.height + " " + module.framerate
                + " " + 1000 + " " + module.device + " " + module.bitrate; 
        }
        public override int ModulePriority()
        {
            return module.priority;
        }
        public override bool ModuleAutostart()
        {
            return module.autostart;
        }
        public override int CreationDelayMs()
        {
            return module.creationDelayMs;
        }

        #endregion

    }
           
} //namespace Ev3devMapping
