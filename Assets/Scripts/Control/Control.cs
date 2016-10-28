/*
 * Copyright (C) 2016 Bartosz Meglicki <meglickib@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 * This program is distributed "as is" WITHOUT ANY WARRANTY of any
 * kind, whether express or implied; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using UnityEngine;
using System;
using System.Collections.Generic;

//this class has temp implementation!
public class Control : ReplayableUDPClient<ControlPacket>
{
	private ControlPacket packet = new ControlPacket();

	private List<RobotModule> modules=new List<RobotModule>();

	protected override void OnDestroy()
	{
		DisableModules();
		base.OnDestroy ();
	}

	protected override void Awake()
	{		
		base.Awake();
	}

	protected override void Start ()
	{
		modules.AddRange(transform.parent.GetComponentsInChildren<RobotModule>());
		modules.Sort();
		modules.Remove (this);
		EnableModules();
	}
		
	public void StartReplay()
	{
		base.StartReplay(0);
	}
		
	void Update ()
	{
		if (replay.mode == UDPReplayMode.Replay)
			return; //do nothing, we replay previous communication


		while (IsPacketWaiting())
		{
			print("Packet waiting, processing");
			ReceiveOne(packet);
			ProcessPacket(packet);
		}

	}

	void ProcessPacket(ControlPacket packet)
	{
		RobotModule mod = modules.Find(m => (m.name == packet.unique_name));

		print(name + ": " + packet.unique_name);
		print(name + ": " + mod.name + " state changed to " + ((ControlPacket.Commands)packet.command).ToString());
	
		if (packet.command == (int)ControlPacket.Commands.Enabled)
			mod.SetState(ModuleState.Online);
		else if (packet.command == (int)ControlPacket.Commands.Disabled)
			mod.SetState(ModuleState.Offline);
		else if (packet.command == (int)ControlPacket.Commands.Failed)
			mod.SetState(ModuleState.Failed);
		else
			print("Unknown control packet command received");
	}

	private ulong GetTimestampUs()
	{
		return (ulong)(DateTime.Now.Ticks / 10);
	}

	public void EnableDisableModule(string unique_module_name, bool enable)
	{
		RobotModule mod = modules.Find(m => (m.name == unique_module_name));

		if(enable)
			EnableModule(mod);
		else
			DisableModule(mod);


	}

	public void EnableModule(RobotModule m)
	{
		if (replay.mode == UDPReplayMode.Replay)
			return; //we just replay
		
		m.SetState(ModuleState.Initializing);

		packet.timestamp_us = GetTimestampUs();
		packet.command = (short)ControlPacket.Commands.Enable;
		packet.creation_delay_ms = (short)m.CreationDelayMs();
		packet.unique_name = m.name;
		packet.call = m.ModuleCall();
		Send(packet);
		print(name + " - enable module " + m.name);
	}
	public void DisableModule(RobotModule m)
	{
		if (replay.mode == UDPReplayMode.Replay)
			return; //we just replay
		
		m.SetState(ModuleState.Shutdown);

		packet.timestamp_us = GetTimestampUs();
		packet.command = (short)ControlPacket.Commands.Disable;
		packet.creation_delay_ms = (short)m.CreationDelayMs();
		packet.unique_name = m.name;
		packet.call = m.ModuleCall();
		Send(packet);
		print(name + " - disable module " + m.name);
	}


	public void EnableModules()
	{		
		foreach (RobotModule module in modules)
		{
			if (!module.ModuleAutostart())
				continue;
		
			EnableModule(module);
		}
	}

	public void DisableModules()
	{
		foreach (RobotModule module in modules)
			DisableModule(module);
		
	}

	#region RobotModule 

	//This module is exception, it controls other modules and has to be started on robot by other means (e.g. manually)
	//For now it's derived from ReplayableUDPClient and RobotModule but it's going to be rewritten for TCP/IP at some point

	public override string ModuleCall()
	{
		throw new NotImplementedException (name + " this should never happen");
	}
	public override int ModulePriority()
	{
		throw new NotImplementedException (name + " this should never happen");
	}
	public override bool ModuleAutostart()
	{
		throw new NotImplementedException (name + " this should never happen");
	}
	public override int CreationDelayMs()
	{
		throw new NotImplementedException (name + " this should never happen");
	}

	#endregion
}