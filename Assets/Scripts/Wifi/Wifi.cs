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
using System.Collections.Generic;
using CircularBuffer;
using System;


namespace Ev3devMapping
{

public enum WifiPlotType {None, Map}

[Serializable]
public class WifiSignalProperties
{
	public int minValueDbm=-80;
	public int maxValueDbm=-45;
	public int warningLevelDbm=-67;
	public int criticalLevelDbm=-70;
}


[Serializable]
public class WifiModuleProperties : ModuleProperties
{
	public string wirelessDevice = "wlan0";
	public uint pollMs = 100;
}

[Serializable]
public class WifiPlotProperties
{
	public WifiPlotType plotType = WifiPlotType.None;
	public float minHeight = 0.0f;
	public float maxHeight = 1.0f;
}

[Serializable]
public class WifiProperties
{
	public int maxPendingReadings=10;
}

public struct WifiReading
{
	public ulong timestamp_us;
	public sbyte signal_dbm;
}


[RequireComponent (typeof (WifiUI))]
[RequireComponent (typeof (Map3D))]
public class Wifi : ReplayableUDPServer<WifiPacket>
{	
	public WifiModuleProperties module;
	public WifiSignalProperties signal;
	public WifiPlotProperties plot;
	public WifiProperties properties;

	private WifiPacket packet=new WifiPacket();
	private Map3D map3D;
	private float averagedPacketTimeMs;
	private Vector3[] readings;
	private int readings_count=0;

	private Vector3 wifiPosition; //where is Wifi adapter placed in robot reference frame

	#region UDP Thread Only Data
	private WifiPacket lastPacket=new WifiPacket();
	private Queue<WifiReading> readingsToProcess = new Queue<WifiReading> ();
	private CircularBuffer<Vector3> wifiReadingsCB;
	#endregion

	#region Thread Shared Data
	private object wifiLock=new object();
	private WifiPacket thread_shared_packet=new WifiPacket();
	private float thread_shared_averaged_packet_time_ms;
	private CircularBuffer<Vector3> thread_shared_readings_CB;
	#endregion

	protected override void OnDestroy()
	{
		base.OnDestroy ();
	}

	protected override void Awake()
	{
		base.Awake();
		readings=new Vector3[properties.maxPendingReadings];
		wifiPosition = transform.localPosition;
		wifiReadingsCB = new CircularBuffer<Vector3> (properties.maxPendingReadings);
		thread_shared_readings_CB = new CircularBuffer<Vector3> (properties.maxPendingReadings);
	}

	protected override void Start ()
	{
		map3D = GetComponent<Map3D>();
		base.Start();
	}
		
	void Update()
	{
		lock (wifiLock)
		{
			packet = thread_shared_packet;
			averagedPacketTimeMs = thread_shared_averaged_packet_time_ms;
			if (thread_shared_readings_CB.Size > 0)
				readings_count = thread_shared_readings_CB.Get (readings, 0, thread_shared_readings_CB.Size);		
		}

		if (readings_count > 0 && map3D != null)
			map3D.AssignVertices(readings, readings_count);

		readings_count = 0;
	}

	#region UDP Thread Only Functions

	protected override void ProcessPacket(WifiPacket packet)
	{
		// UDP doesn't guarantee ordering of packets, if previous packet is newer ignore the received
		if (packet.timestamp_us <= lastPacket.timestamp_us)
		{
			print(name + " ignoring out of time packet (previous, now):" + Environment.NewLine + lastPacket.ToString() + Environment.NewLine + packet.ToString());
			return;
		}			
		lastPacket.CloneFrom(packet);

		if (plot.plotType == WifiPlotType.Map)
			ProcessReadings (packet);

		lock (wifiLock)
		{
			thread_shared_packet = lastPacket;
			thread_shared_averaged_packet_time_ms = AveragedPacketTimeMs();
			if (wifiReadingsCB.Size > 0) //unoptimal!
				thread_shared_readings_CB.Put(wifiReadingsCB.Get (wifiReadingsCB.Size));
		}
	}

	private void ProcessReadings(WifiPacket packet)
	{
		readingsToProcess.Enqueue (new WifiReading{ timestamp_us = packet.timestamp_us, signal_dbm = packet.signal_dbm });

		bool not_in_history, not_yet;
		Matrix4x4 robotToGlobal = new Matrix4x4();

		while (readingsToProcess.Count > 0)
		{
			WifiReading reading = readingsToProcess.Peek ();
			PositionHistory.PositionSnapshot snapshot= positionHistory.GetPositionSnapshotThreadSafe(reading.timestamp_us, reading.timestamp_us, out not_yet, out not_in_history);
			if (not_in_history)
			{
				print("wifi - ignoring packet (position data not in history) with timestamp " + reading.timestamp_us);
				readingsToProcess.Dequeue ();
				continue;
			}
			if (not_yet) //wait for position data to arrive				
				return;

			readingsToProcess.Dequeue();

			float height01 = ((float)reading.signal_dbm - (float)signal.minValueDbm) / ((float)signal.maxValueDbm-(float)signal.minValueDbm);
			float heightm = Mathf.LerpUnclamped(plot.minHeight, plot.maxHeight, height01);	
			Vector3 position = new Vector3(wifiPosition.x, heightm, wifiPosition.z);
			PositionData pos = snapshot.PositionAt (reading.timestamp_us); 

			robotToGlobal.SetTRS(pos.position,  Quaternion.Euler(0.0f, pos.heading, 0.0f), Vector3.one);
			position = robotToGlobal.MultiplyPoint3x4(position);
			wifiReadingsCB.Put (position);
		}			
	}
		
	#endregion

	public float GetAveragedPacketTimeMs()
	{
		return averagedPacketTimeMs;
	}

	public string GetBSSID()
	{
		return packet.bssid_string;
	}
	public string GetSSID()
	{
		return packet.ssid;
	}
	public sbyte GetSignalDbm()
	{
		return packet.signal_dbm;
	}
	public uint GetRxPackets()
	{
		return packet.rx_packets;
	}
	public uint GetTxPackets()
	{
		return packet.tx_packets;
	}

	#region RobotModule 

	public override string ModuleCall()
	{
		return "ev3wifi " + network.hostIp + " " + moduleNetwork.port + " " + module.wirelessDevice + " " + module.pollMs;
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

} //namespace