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

	private List<IRobotModule> modules=new List<IRobotModule>();

	public override string GetUniqueName()
	{
		return name;
	}

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
		modules.AddRange(transform.parent.GetComponentsInChildren<IRobotModule>());
		modules.Sort();
		EnableModules();
	}



	public void StartReplay()
	{
		base.StartReplay(0);
	}
		
	void Update ()
	{
		
		if (udp.replayMode == UDPReplayMode.Replay)
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
		IRobotModule mod = modules.Find(m => (m.GetUniqueName() == packet.unique_name));

		print(name + ": " + packet.unique_name);
		print(name + ": " + mod.GetUniqueName() + " state changed to " + ((ControlPacket.Commands)packet.command).ToString());
	
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
		IRobotModule mod = modules.Find(m => (m.GetUniqueName() == unique_module_name));

		if(enable)
			EnableModule(mod);
		else
			DisableModule(mod);


	}

	public void EnableModule(IRobotModule m)
	{
		m.SetState(ModuleState.Initializing);

		packet.timestamp_us = GetTimestampUs();
		packet.command = (short)ControlPacket.Commands.Enable;
		packet.creation_delay_ms = (short)m.CreationDelayMs();
		packet.unique_name = m.GetUniqueName();
		packet.call = m.ModuleCall();
		Send(packet);
		print(name + " - enable module " + m.GetUniqueName());
	}
	public void DisableModule(IRobotModule m)
	{
		m.SetState(ModuleState.Shutdown);

		packet.timestamp_us = GetTimestampUs();
		packet.command = (short)ControlPacket.Commands.Disable;
		packet.creation_delay_ms = (short)m.CreationDelayMs();
		packet.unique_name = m.GetUniqueName();
		packet.call = m.ModuleCall();
		Send(packet);
		print(name + " - disable module " + m.GetUniqueName());
	}


	public void EnableModules()
	{
		foreach (IRobotModule module in modules)
		{
			if (!module.ModuleAutostart())
				continue;
		
			EnableModule(module);

		}
	}

	public void DisableModules()
	{
		foreach (IRobotModule module in modules)
			DisableModule(module);
		
	}
}