/*
 * Copyright (C) 2018 Bartosz Meglicki <meglickib@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 * This program is distributed "as is" WITHOUT ANY WARRANTY of any
 * kind, whether express or implied; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

/* This module starts:
 * - gstreamer (video transmission) on robot 
 * - gstreamer (video client) on host
 *
 * As a prerequisity gstreamer has to be installed on both machines
*/

using System;
using System.Diagnostics;
using UnityEngine;

[Serializable]
public class VideoModuleProperties : ModuleProperties
{
	public string device="/dev/video0";
	public int width=640;
	public int height=360;
	public int bitrate=3000000;
}

//to be renamed to camera or something
public class Video : RobotModule
{
	public VideoModuleProperties module;


	private Process m_video_process=null;
	// Use this for initialization
	void Start ()
	{
		print (name + " - starting video client (gstreamer)");
		m_video_process = CreateVideoClientProcess ();
	}
	
	void Update ()
	{
	}

	void OnDestroy()
	{
		print (name + " - stopping video client (gstreamer)");

		if(m_video_process != null)
			m_video_process.Kill();
	}
		
	private Process CreateVideoClientProcess()
	{
		ProcessStartInfo info=new ProcessStartInfo{
			FileName = "gst-launch-1.0",
			Arguments = "udpsrc port=" + moduleNetwork.port + " ! application/x-rtp, media=video, clock-rate=90000, encoding-name=H264 ! rtpjitterbuffer latency=50 drop-on-latency=1 ! rtph264depay ! avdec_h264 ! fpsdisplaysink sync=false text-overlay=false",
			UseShellExecute = true,
			RedirectStandardOutput = false,
			CreateNoWindow = false
		};

		Process process = new Process { StartInfo = info };
		process.Start ();
		return process;
	}

	#region RobotModule 

	public override string ModuleCall()
	{
		return "video.sh" + " " +  module.device + " " + module.width + " " + module.height + " " + module.bitrate + " " + network.hostIp + " " + moduleNetwork.port;
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
