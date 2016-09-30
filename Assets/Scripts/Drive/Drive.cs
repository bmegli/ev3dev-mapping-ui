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
	public DriveModuleProperties module;

	private Physics physics;
	private Limits limits;

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
		physics = SafeGetComponentInParent<Physics>().DeepCopy();
		limits = SafeGetComponentInParent<Limits>().DeepCopy();
	}

	protected override void Start ()
	{
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
			InputToEngineSpeeds (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.2f + 0.8f*Input.GetAxis("Acceleration"), out packet.param1,out packet.param2);
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
		DistanceAndSpeedToEngineCountsAndSpeed (distance_cm, speed_cm_per_sec, out packet.param1, out packet.param2, out packet.param3, out packet.param4);
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
	private T SafeGetComponentInParent<T>() where T : MonoBehaviour
	{
		T component = GetComponentInParent<T> ();

		if (component == null)
			Debug.LogError ("Expected to find component of type " + typeof(T) + " but found none");

		return component;
	}

	#region Logic

	public void InputToEngineSpeeds(float in_hor, float in_ver, float in_scale,out short left_counts_s,out short right_counts_s)
	{
		float maxAngularSpeedContributionMmPerS = limits.MaxAngularSpeedRadPerS() * physics.wheelbaseMm / 2.0f;
		float countsPerMM = physics.CountsPerMM ();

		float V_mm_s = in_ver * limits.MaxLinearSpeedMmPerSec;
		float angular_speed_contrib_mm_s = maxAngularSpeedContributionMmPerS * in_hor;

		float VL_mm_s = V_mm_s + angular_speed_contrib_mm_s;
		float VR_mm_s = V_mm_s - angular_speed_contrib_mm_s;

		float VL_counts_s = VL_mm_s * countsPerMM;
		float VR_counts_s = VR_mm_s * countsPerMM;

		float scale = in_scale * ( (physics.reverseMotorPolarity) ? -1.0f : 1.0f );

		left_counts_s = (short)(VL_counts_s * scale);
		right_counts_s = (short)(VR_counts_s * scale);
	}
	public void DistanceAndSpeedToEngineCountsAndSpeed(float distance_cm, float speed_cm_per_s, out short l_counts_s, out short r_counts_s, out short l_counts, out short r_counts)
	{		
		float counts = distance_cm * 10.0f * physics.CountsPerMM();
		float counts_per_s = speed_cm_per_s * 10.0f * physics.CountsPerMM();

		float scale = ((physics.reverseMotorPolarity) ? -1.0f : 1.0f);
		l_counts = r_counts = (short)(scale * counts);
		l_counts_s = r_counts_s =(short)(counts_per_s * scale);
		//TO DO - check limits!
	}


	#endregion

}
