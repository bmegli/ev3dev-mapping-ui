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

/*
*  For functional requirements see:
*  https://github.com/bmegli/ev3dev-mapping-ui/issues/26
* 
*/


using UnityEngine;
using System;
using System.Collections.Generic;

//this class has temp implementation!
public class Control : ReplayableTCPClient<ControlMessage>
{
	private List<RobotModule> modules=new List<RobotModule>();
	private ControlMessage msg=new ControlMessage();

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
		modules.Remove(this);
		modules.Sort();
	}
		
	public void StartReplay()
	{
		//base.StartReplay(0);
	}
		
	void Update ()
	{
		if (replay.mode == UDPReplayMode.Replay)
			return; //do nothing, we replay previous communication

		switch (GetState ())
		{
		case ModuleState.Offline:
			StartConnectingIfAutostartAndDisconnected ();
			break;
		case ModuleState.Initializing:
			StartConnectingIfAutostartAndDisconnected ();
			if (TCPClientState == TCPClientState.Idle)
				SetState (ModuleState.Failed);
			if (TCPClientState == TCPClientState.Connected)
			{
				SetState (ModuleState.Online);
				EnableModules ();
			}
			break;
		case ModuleState.Online:
			if (TCPClientState == TCPClientState.Idle)
			{
				SetState (ModuleState.Failed);
				return;
			}
			while (ReceiveOne (msg))
				ProcessMessage (msg);
			//LastSeen + Keepalive
			break;
		case ModuleState.Shutdown:
			while (ReceiveOne (msg))
				ProcessMessage (msg);
			//all all disabled go offline, disconnect
			break;
		case ModuleState.Failed:
			break;
		}

	}

	private void StartConnectingIfAutostartAndDisconnected()
	{		
		if (TCPClientState == TCPClientState.Disconnected && ModuleAutostart ())
		{
			StartConnecting ();
			SetState (ModuleState.Initializing);
		}
	}


	private ModuleState ModuleStateFromControlCommand(ControlCommands cmd)
	{
		if (cmd == ControlCommands.ENABLED)
			return ModuleState.Online;
		if (cmd == ControlCommands.DISABLED)
			return ModuleState.Offline;
		if (cmd == ControlCommands.FAILED)
			return ModuleState.Failed;
		throw new ArgumentException("Unable to convert command " + cmd.ToString() + " to ModuleState");	
	}

	void ProcessMessage(ControlMessage message)
	{
		RobotModule module;
		ControlCommands cmd = message.GetCommand();

		switch (cmd)
		{
			case ControlCommands.KEEPALIVE:
				break;
			case ControlCommands.ENABLED:				
			case ControlCommands.DISABLED:
			case ControlCommands.FAILED: //failed has additional information on error code
				module = modules.Find(m => (m.name == message.Attribute<string>(0)));
				print(name + " - " + module.name + " state changed to " + cmd.ToString());
				module.SetState(ModuleStateFromControlCommand(cmd));
				break;
			default:
				print(name + " - ignoring unknown command " + message.GetType());
				break;
		}
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

		ControlMessage msg = ControlMessage.EnableMessage (m.name, m.ModuleCall (), (ushort)m.CreationDelayMs ());

		print(name + " - enable module " + m.name);
		Send (msg);

	}
	public void DisableModule(RobotModule m)
	{
		if (replay.mode == UDPReplayMode.Replay)
			return; //we just replay
		
		m.SetState(ModuleState.Shutdown);

		ControlMessage msg = ControlMessage.DisableMessage(m.name);

		Send (msg);
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

	private void DisableModules()
	{ 
		// maybe check some connected state etc

		foreach (RobotModule module in modules)
			if(module.GetState() == ModuleState.Initializing || module.GetState() == ModuleState.Online)
				module.SetState (ModuleState.Shutdown);

		ControlMessage msg = ControlMessage.DisableAllMessage();
		Send (msg);
		print(name + " - disable all modules");
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
		return true;
	}
	public override int CreationDelayMs()
	{
		throw new NotImplementedException (name + " this should never happen");
	}

	#endregion
}