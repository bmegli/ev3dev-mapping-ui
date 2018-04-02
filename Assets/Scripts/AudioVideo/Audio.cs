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
* - tx (part of trx) (audio transmission) on robot 
* - rx (part of trx) (audio client) on host
 *
* As a prerequisity trx should be installed in system path (only rx is needed)
*/

using System;
using System.Diagnostics;
using UnityEngine;

[Serializable]
public class AudioModuleProperties : ModuleProperties
{
	public string device="plughw:1";
	public int bitrateKhz=96;
}

//to be renamed to camera or something
public class Audio : RobotModule
{
	public AudioModuleProperties module;

	private Process m_audio_process=null;
	// Use this for initialization
	void Start ()
	{
		print (name + " - starting audio client (rx)");
		m_audio_process = CreateAudioClientProcess ();
	}
	
	void Update ()
	{
	}

	void OnDestroy()
	{
		print (name + " - stopping audio client (rx)");

		if(m_audio_process != null)
			m_audio_process.Kill();
	}
		
	private Process CreateAudioClientProcess()
	{
		ProcessStartInfo info=new ProcessStartInfo{
			FileName = "rx",
			Arguments = "-p " + moduleNetwork.port + " -c 1", 
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
		return "audio.sh" +  " " + module.device + " " + module.bitrateKhz  + " " + network.hostIp + " " + moduleNetwork.port;

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
