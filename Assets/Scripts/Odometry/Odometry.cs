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
using System.Collections;
using System;

// we want to keep binary compatiblity between DeadReconning and Odometry
// so that we can rebuild recorded DeadReconning as Odometry (skipping information about heading)
using OdometryPacket = DeadReconningPacket;

[Serializable]
public class OdometryModuleProperties : ModuleProperties
{
	public int pollMs=10;
}


[RequireComponent (typeof (OdometryUI))]
public class Odometry : ReplayableUDPServer<OdometryPacket>
{	
	public OdometryModuleProperties module;

	private PositionData actualPosition;
	private float averagedPacketTimeMs;

	#region UDP Thread Only Data
	private OdometryPacket lastPacket=new OdometryPacket();
	private PositionData lastPosition=new PositionData();
	#endregion

	#region Thread Shared Data
	private object odometryLock=new object();
	private PositionData thread_shared_position=new PositionData();
	private float thread_shared_averaged_packet_time_ms;
	#endregion

	protected override void OnDestroy()
	{
		base.OnDestroy ();
	}

	protected override void Awake()
	{
		base.Awake();
	}

	protected override void Start ()
	{
		base.Start();
	}

	public void StartReplay()
	{
		base.StartReplay(20000);
	}

	void Update ()
	{
		lock (odometryLock)
		{
			actualPosition = thread_shared_position;
			averagedPacketTimeMs = thread_shared_averaged_packet_time_ms;
		}

		transform.parent.transform.position=actualPosition.position;
		transform.parent.transform.rotation=Quaternion.Euler(0.0f, actualPosition.heading, 0.0f);
	}

	#region UDP Thread Only Functions

	protected override void ProcessPacket(OdometryPacket packet)
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
			print("odometry - ignoring out of time packet (previous, now):" + Environment.NewLine + lastPacket.ToString() + Environment.NewLine + packet.ToString());
			return;
		}

		//this is the actual work
		lastPosition=EstimatePosition(lastPosition, lastPacket, packet);
		lastPacket.CloneFrom(packet);

		// Share the new calculated position estimate with Unity thread
		lock (odometryLock)
		{
			thread_shared_position=lastPosition;
			thread_shared_averaged_packet_time_ms = AveragedPacketTimeMs();
		}

		positionHistory.PutThreadSafe(lastPosition);

	}

	private PositionData EstimatePosition(PositionData lastPosition, OdometryPacket lastPacket, OdometryPacket packet)
	{
		// Calculate the linear displacement since last packet
		float distance_per_encoder_count_mm = Mathf.PI * physics.wheelDiameterMm / physics.encoderCountsPerRotation;
		float ldiff = packet.position_left - lastPacket.position_left;
		float rdiff = packet.position_right - lastPacket.position_right;
		float displacement_m = (ldiff + rdiff) * distance_per_encoder_count_mm / 2.0f / Constants.MM_IN_M;

		if (physics.reverseMotorPolarity)
			displacement_m = -displacement_m;


		float angle_start_deg = lastPosition.heading;
		float angle_difference_deg = (ldiff - rdiff) * distance_per_encoder_count_mm / physics.wheelbaseMm * Constants.RAD2DEG;
		if (physics.reverseMotorPolarity)
			angle_difference_deg = -angle_difference_deg;

		// The tricky case when we cross 0 or -180/180 in packets has to be handled separately
		if (Mathf.Abs(angle_difference_deg) > 180.0f)
			angle_difference_deg = angle_difference_deg -  Mathf.Sign(angle_difference_deg) * 360.0f;

		float angle_end_deg = angle_start_deg + angle_difference_deg;

		float average_heading_rad = (angle_start_deg + angle_difference_deg / 2.0f) * Constants.DEG2RAD;

		// Finally update the position and heading
		lastPosition.timestamp = packet.timestamp_us;
		lastPosition.position = new Vector3(lastPosition.position.x + displacement_m * Mathf.Sin(average_heading_rad), lastPosition.position.y, lastPosition.position.z + displacement_m * Mathf.Cos(average_heading_rad));
		lastPosition.heading = angle_end_deg;

		return lastPosition;
	}
		
	#endregion

	public Vector3 GetPosition()
	{
		return actualPosition.position;
	}
	public float GetHeading()
	{
		return actualPosition.heading;
	}

	public float GetAveragedPacketTimeMs()
	{
		return averagedPacketTimeMs;
	}

	#region RobotModule 

	public override string ModuleCall()
	{
		return "ev3odometry " + network.hostIp + " " + udp.port + " " + module.pollMs;
	}
	public override int ModulePriority()
	{
		return module.priority;
	}
	public override bool ModuleAutostart()
	{
		return module.autostart && !replay.ReplayInbound();
	}
	public override int CreationDelayMs()
	{
		return module.creationDelayMs;
	}

	#endregion
}
