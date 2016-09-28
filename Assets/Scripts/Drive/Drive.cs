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

[Serializable]
public class DriveModuleProperties : ModuleProperties
{
	public int timeoutMs=500;
}

enum DriveMode {Manual, Auto};

[RequireComponent (typeof (LaserUI))]
public class Drive : ReplayableUDPClient<DrivePacket>, IRobotModule
{
	public int packetDelayMs=50;
	public DriveUI driveUI;
	public DifferentialDrive driveModel;
	public DriveModuleProperties module;

	private DifferentialDrive drive;

	private DrivePacket packet = new DrivePacket();
	private float timeSinceLastPacketMs;

	private DriveMode mode=DriveMode.Manual;

	public override string GetUniqueName()
	{
		return name;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy ();
	}

	protected override void Awake()
	{		
		base.Awake ();
	}

	protected override void Start ()
	{
		if (driveModel == null)
		{
			Debug.LogWarning("Drive Model not set for Drive Component!");
			enabled = false;
			return;
		}
		drive = Instantiate<DifferentialDrive>(driveModel);
	}

	public void StartReplay()
	{
		base.StartReplay(0);
	}
		
	void Update ()
	{
		if (replay.mode == UDPReplayMode.Replay)
			return; //do nothing, we replay previous communication or replay inbound traffic

		timeSinceLastPacketMs += Time.deltaTime*1000.0f;

		if (timeSinceLastPacketMs < packetDelayMs)
			return;

		packet.timestamp_us = Timestamp.TimestampUs();

		if (mode == DriveMode.Auto)
		{
			if (IsManualInput ())
			{
				mode = DriveMode.Manual;
				return;
			}
			packet.command = (short)DrivePacket.Commands.KEEPALIVE;
		}

		if (mode == DriveMode.Manual)
		{
			packet.command = (short)DrivePacket.Commands.SET_SPEED;
			drive.InputToEngineSpeeds (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.2f + 0.8f*Input.GetAxis("Acceleration"), out packet.param1,out packet.param2);
		}

		Send(packet);	
		timeSinceLastPacketMs = 0.0f;
	}

	public bool IsManualInput()
	{
		return (Input.GetAxis ("Horizontal") != 0.0f || Input.GetAxis ("Vertical") != 0);
	}

	public void DriveAhead(float distance_cm, float speed_cm_per_sec)
	{
		if (replay.mode == UDPReplayMode.Replay) 
			return; //do nothing, we replay previous communication or inbound traffic
		
		mode = DriveMode.Auto;
		packet.timestamp_us = Timestamp.TimestampUs();
		packet.command = (short)DrivePacket.Commands.TO_POSITION_WITH_SPEED;
		drive.DistanceAndSpeedToEngineCountsAndSpeed (distance_cm, speed_cm_per_sec, out packet.param1, out packet.param2, out packet.param3, out packet.param4);
		Send(packet);	
		timeSinceLastPacketMs = 0.0f;
	}

	#region IRobotModule 

	private ModuleState moduleState=ModuleState.Offline;

	public ModuleState GetState()
	{
		return moduleState;
	}
	public void SetState(ModuleState state)
	{
		moduleState = state;
	}

	public string ModuleCall()
	{
		return "ev3drive " + udp.port + " " + module.timeoutMs;
	}
	public int ModulePriority()
	{
		return module.priority;
	}
	public bool ModuleAutostart()
	{
		return module.autostart;
	}
	public int CreationDelayMs()
	{
		return module.creationDelayMs;
	}

	public int CompareTo(IRobotModule other)
	{
		return ModulePriority().CompareTo( other.ModulePriority() );
	}

	public Control GetControl()
	{
		return GetComponent<Control>();
	}
		
	#endregion

	private T SafeInstantiate<T>(T original) where T : MonoBehaviour
	{
		if (original == null)
		{
			Debug.LogError ("Expected to find prefab of type " + typeof(T) + " but it was not set");
			return default(T);
		}
		return Instantiate<T>(original);
	}


}
