﻿using UnityEngine;
using System;
using System.Collections.Generic;

//this class has temp implementation!
public class Control : ReplayableUDPClient<ControlPacket>
{
	private ControlPacket packet = new ControlPacket();

	private List<IRobotModule> modules=new List<IRobotModule>();

	public override string GetUniqueName()
	{
		return "control";
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
		GetComponents<IRobotModule>(modules);
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

		print("Control: " + packet.unique_name);
		print("control: " + mod.GetUniqueName() + " state changed to " + ((ControlPacket.Commands)packet.command).ToString());
	
		if (packet.command == (int)ControlPacket.Commands.Enabled)
			mod.SetState(ModuleState.Online);
		else if (packet.command == (int)ControlPacket.Commands.Disabled)
			mod.SetState(ModuleState.Offline);
		else if (packet.command == (int)ControlPacket.Commands.Failed)
			mod.SetState(ModuleState.Failed);
		else
			print("Unknown control packet command received");

	//	if (packet.command == (int)ControlPacket.Commands.Failed)
	//		print("failed fuck");
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
		print("control - enable module " + m.GetUniqueName());
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
		print("control - disable module " + m.GetUniqueName());
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
