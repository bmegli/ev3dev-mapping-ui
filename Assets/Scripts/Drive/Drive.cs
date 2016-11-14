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
using System.IO;
using System;


[Serializable]
public class DriveModuleProperties : ModuleProperties
{
	public int timeoutMs=500;
}

enum DriveMode {Manual, Auto, Backtrack};

[RequireComponent (typeof (DriveUI))]
public class Drive : ReplayableUDPClient<DrivePacket>
{
	public int packetDelayMs=50;
	public DriveModuleProperties module;

	private DrivePacket packet = new DrivePacket();
	private float timeSinceLastPacketMs;

	private DriveMode mode=DriveMode.Manual;

	protected override void OnDestroy()
	{
		StopBacktrack();
		base.OnDestroy ();
		File.Delete(GetBacktrackFilename());
		if (!replay.ReplayAny() && !replay.RecordOutbound())
			File.Delete(GetRecordFilename());
	}

	protected override void Awake()
	{		
		base.Awake ();

		if (!replay.ReplayAny () && !replay.RecordOutbound ())
			InitRecordTo (GetRecordFilename());
		
		CheckLimits();
	}

	protected override void Start ()
	{
	}
				
	void Update ()
	{
		if (replay.ReplayAny())
			return; 

		timeSinceLastPacketMs += Time.deltaTime*1000.0f;

		if (timeSinceLastPacketMs < packetDelayMs)
			return;

		if (IsManualInput ())
		{
			if (mode == DriveMode.Auto)
			{
				mode = DriveMode.Manual;
				return;
			}
			if (mode == DriveMode.Backtrack)
				StopReplay ();			
		}
	
		if (mode == DriveMode.Backtrack)
		{
			if (!ReplayRunning)
				mode = DriveMode.Manual;
			else
				return;
		}
						
		packet.timestamp_us = Timestamp.TimestampUs();

		if(mode == DriveMode.Auto)
			packet.command = DrivePacket.Commands.KEEPALIVE;
		else if (mode == DriveMode.Manual)
		{
			packet.command = DrivePacket.Commands.SET_SPEED;
			InputToEngineSpeeds (Input.GetAxis(input.horizontal), Input.GetAxis(input.vertical), (1.0f-input.accelerationPower) + input.accelerationPower *Input.GetAxis(input.acceleration), out packet.param1,out packet.param2);
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
		if (replay.ReplayAny())
		{
			print(name + " - ignoring drive command (replay)");
			return; 
		}
		StopBacktrack();
					
		mode = DriveMode.Auto;
		packet.timestamp_us = Timestamp.TimestampUs();
		packet.command = DrivePacket.Commands.TO_POSITION_WITH_SPEED;
		DistanceAndSpeedToEngineCountsAndSpeed (distance_cm, speed_cm_per_sec, out packet.param1, out packet.param2, out packet.param3, out packet.param4);
		Send(packet);	
		timeSinceLastPacketMs = 0.0f;
	}

	public void Backtrack()
	{
		if (replay.ReplayAny())
		{
			print(name + " - ignoring backtrack command (replay)");
			return; 
		}

		StopBacktrack();

		PrepareBacktrackDump(GetBacktrackFilename());
		InitReplayFrom (GetBacktrackFilename());
		mode = DriveMode.Backtrack;
		StartExclusiveReplay();
	}

		
	#region Logic

	//move this to design time later
	private void CheckLimits()
	{	
		short left, right;
		InputToEngineSpeeds (1.0f, 1.0f, 1.0f, out left, out right);
		InputToEngineSpeeds (0.0f, 1.0f, 1.0f, out left, out right);	
	}

	private void Clamp(ref short value, short min, short max)
	{
		if (value < min)
		{
			Debug.LogWarning ("Limits of differential drive exceed physical capabilities: leads to " + value + " speed where theorethical limit is " + min);	
			value = min;
		}
		else if (value > max)
		{
			Debug.LogWarning ("Limits of differential drive exceed physical capabilities: leads to " + value + " speed where theorethical limit is " + max);	
			value = max;
		}
	}

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

		Clamp(ref left_counts_s, (short)-physics.maxEncoderCountsPerSecond, (short)physics.maxEncoderCountsPerSecond);
		Clamp(ref right_counts_s, (short)-physics.maxEncoderCountsPerSecond, (short)physics.maxEncoderCountsPerSecond);
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

	#region Backtracking

	private void StopBacktrack()
	{
		if (mode == DriveMode.Backtrack)
		{
			StopReplay();
			print(name + " - stopping backtracking");
			return;
		}
	}
		
	private void PrepareBacktrackDump(string backtrackFilename)
	{		
		FlushDump ();

		FileStream filestream=File.Open(GetRecordFilename(), FileMode.Open, FileAccess.Read, FileShare.Write);
		long packets = filestream.Length / packet.BinarySize ();

		BinaryReader reader = new BinaryReader (filestream);
		DrivePacket[] datagrams=new DrivePacket[packets];

		for (int i = datagrams.Length - 1; i >= 0; --i)
		{
			datagrams [i] = new DrivePacket ();
			datagrams [i].FromBinary (reader);
		}

		reader.Close ();
		filestream.Close ();

		BinaryWriter rewriter=new BinaryWriter(File.Open (backtrackFilename, FileMode.Create, FileAccess.Write));

		ulong now = Timestamp.TimestampUs ();
		ulong base_timestamp = datagrams[0].timestamp_us;

		foreach (DrivePacket dp in datagrams)
		{
			dp.timestamp_us = now + (base_timestamp - dp.timestamp_us);
			switch (dp.command)
			{
			case DrivePacket.Commands.SET_SPEED:
				dp.param1 = (short) -dp.param1;
				dp.param2 = (short) -dp.param2;
				break;
			case DrivePacket.Commands.TO_POSITION_WITH_SPEED:
				dp.param3 = (short) -dp.param3;
				dp.param4 = (short) -dp.param4;
				break;
			default:
				break;
			}

			dp.ToBinary (rewriter);
		}
			
		rewriter.Close ();
	}
		
	private string GetRecordFilename()
	{
		if (replay.RecordOutbound ())
			return base.GetReplayFilename();
		if (!replay.ReplayAny ())
			return base.GetReplayFilename ("Track");

		throw new InvalidOperationException ("There should be no drive record file in replay mode");
	}
	private string GetBacktrackFilename()
	{
		return GetReplayFilename("BackTrack");
	}
		
	#endregion

	#region RobotModule 

	public override string ModuleCall()
	{
		return "ev3drive " + moduleNetwork.port + " " + module.timeoutMs;
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
