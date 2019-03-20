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

using UnityEngine;
using System.Collections;
using System;

namespace Ev3devMapping
{

/*
[Serializable]
public class DeadReconningModuleProperties : ModuleProperties
{
	public int pollMs=10;
}
*/

[RequireComponent (typeof (DeadReconning3DUI))]
public class DeadReconning3D : ReplayableUDPServer<DeadReconning3DPacket>
{	
	public DeadReconningModuleProperties module; //consider

	private PositionData actualPosition;
	private float averagedPacketTimeMs;
	private float batteryVoltage; //temp in Volts 

	#region UDP Thread Only Data
	private DeadReconning3DPacket lastPacket=new DeadReconning3DPacket();
	private PositionData lastPosition=new PositionData();
	//private Vector3 initialRotation; //consider
	#endregion

	#region Thread Shared Data
	private object deadReconningLock=new object();
	private PositionData thread_shared_position=new PositionData();
	private float thread_shared_averaged_packet_time_ms;
	private float thread_shared_battery_voltage; //temp in volts
	#endregion

	protected override void OnDestroy()
	{
		base.OnDestroy ();
	}

	protected override void Awake()
	{
		base.Awake();
		lastPosition = new PositionData{position=transform.parent.position, rotation=transform.parent.eulerAngles, quaternion=transform.parent.rotation};
		//initialRotation = lastPosition.rotation;
		thread_shared_position = lastPosition;
	}

	protected override void Start ()
	{
		base.Start();
	}

	public override void StartReplay()
	{
		StartReplay(20000);
	}

	void Update ()
	{
		lock (deadReconningLock)
		{
			actualPosition = thread_shared_position;
			averagedPacketTimeMs = thread_shared_averaged_packet_time_ms;
			batteryVoltage = thread_shared_battery_voltage;
		}
			
		transform.parent.transform.position=actualPosition.position;
		transform.parent.transform.rotation=actualPosition.quaternion;
	}

	#region UDP Thread Only Functions

	protected override void ProcessPacket(DeadReconning3DPacket packet)
	{
		//First call - set first udp packet with reference encoder positions
		if (lastPacket.timestamp_us == 0)
		{ 
			lastPacket.CloneFrom(packet);
			return;
		}

		// UDP doesn't guarantee ordering of packets, if previous odometry is newer ignore the received
		if (packet.timestamp_us <= lastPacket.timestamp_us)
		{
			print(name + " - ignoring out of time packet (previous, now):" + Environment.NewLine + lastPacket.ToString() + Environment.NewLine + packet.ToString());
			return;
		}

		//this is the actual work	
		lastPosition=EstimatePosition(lastPosition, lastPacket, packet);
		lastPacket.CloneFrom(packet);

		// Share the new calculated position estimate with Unity thread
		lock (deadReconningLock)
		{
			thread_shared_position=lastPosition;
			thread_shared_averaged_packet_time_ms = AveragedPacketTimeMs();
		}

		positionHistory.PutThreadSafe(lastPosition);

	}
		
	private PositionData EstimatePosition(PositionData lastPosition, DeadReconning3DPacket lastPacket, DeadReconning3DPacket packet)
	{		
		// Calculate the linear displacement since last packet
		float distance_per_encoder_count_mm = Mathf.PI * physics.wheelDiameterMm / physics.encoderCountsPerRotation;
		float ldiff = packet.position_left - lastPacket.position_left;
		float rdiff = packet.position_right - lastPacket.position_right;
		float displacement_m = (ldiff + rdiff) * distance_per_encoder_count_mm / 2.0f / Constants.MM_IN_M;

		if (physics.reverseMotorPolarity)
			displacement_m = -displacement_m;
		
		Quaternion quat_act = new Quaternion (packet.quat_x, packet.quat_y, packet.quat_z, packet.quat_w);
		Quaternion quat_avg = Quaternion.Lerp(lastPosition.quaternion, quat_act, 0.5f); 

		Vector3 forward = quat_avg * Vector3.forward;
		Vector3 displacement = displacement_m * forward;

		// Finally update the position and heading
		lastPosition.timestamp = packet.timestamp_us;
		lastPosition.position = lastPosition.position + displacement;
		lastPosition.quaternion.Set(packet.quat_x, packet.quat_y, packet.quat_z, packet.quat_w);

		return lastPosition;
	}
		
	#endregion

	public Vector3 GetPosition()
	{
		return actualPosition.position;
	}
	public Vector3 GetRotation()
	{
		return actualPosition.quaternion.eulerAngles;
	}
		
	public float GetHeading()
	{
		return actualPosition.heading;
	}

	public float GetAveragedPacketTimeMs()
	{
		return averagedPacketTimeMs;
	}

	public float GetBatteryVoltage()
	{
		return batteryVoltage;
	}

	#region RobotModule 

	public override string ModuleCall()
	{
		return "ev3dead-reconning " + network.hostIp + " " + moduleNetwork.port + " " + module.pollMs;
	}
	public override int ModulePriority()
	{
		return module.priority;
	}
	public override bool ModuleAutostart()
	{
		return false; //consider
	}
	public override int CreationDelayMs()
	{
		return module.creationDelayMs;
	}

	#endregion

}

} //namespace