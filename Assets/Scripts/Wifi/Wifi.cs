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

public enum WifiPlotType {None, Map}

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
	public int minValueDbm=-100;
	public int maxValueDbm=-45;
	public float minHeight = 0.0f;
	public float maxHeight = 1.0f;
}

class WifiThreadSharedData
{ 
	public Vector3[] readings=new Vector3[10]; //this currenlty uses only readings[0]
	public int from = -1;
	public int length = -1;
	public bool consumed = true;
	public float averagedPacketTimeMs;

	public void CopyNewDataFrom(WifiThreadSharedData other)
	{
		// Copy only the data that changed since last call
		from = other.from;
		length = other.length;
		averagedPacketTimeMs = other.averagedPacketTimeMs;

		for (int i = from; i < from + length; ++i)
		{
			int ind = i % readings.Length;
			readings[ind] = other.readings[ind];
		}
	}
}

class WifiThreadInternalData
{
	public Vector3[] readings = new Vector3[10];
	public ulong[] timestamps = new ulong[10];

	public int next=0;
	public bool pending=false;
	public int pending_from=0;
	public int pending_length=0;
	public ulong t_from=0;
	public ulong t_to=0;

	public void SetPending(int from, int length, ulong time_from, ulong time_to)
	{
		pending = true;
		pending_from = from;
		pending_length = length;
		t_from = time_from;
		t_to = time_to;
	}
}


	
[RequireComponent (typeof (WifiUI))]
[RequireComponent (typeof (Map3D))]
public class Wifi : ReplayableUDPServer<WifiPacket>, IRobotModule
{	
	public WifiModuleProperties module;
	public WifiPlotProperties plot;

	private WifiPacket packet=new WifiPacket();
	private Map3D map3D;
	private PositionHistory positionHistory;
	private float averagedPacketTimeMs;

	private Vector3 wifiPosition; //where is Wifi adapter placed in robot reference frame

	#region UDP Thread Only Data
	private WifiThreadInternalData threadInternal = new WifiThreadInternalData ();
	private WifiPacket lastPacket=new WifiPacket();
	#endregion

	#region Thread Shared Data
	private WifiThreadSharedData threadShared=new WifiThreadSharedData();
	private WifiPacket thread_shared_packet=new WifiPacket();
	#endregion

	protected override void OnDestroy()
	{
		base.OnDestroy ();
	}

	protected override void Awake()
	{
		base.Awake();
		wifiPosition = transform.localPosition;
	}

	protected override void Start ()
	{
		positionHistory = SafeGetComponentInParent<PositionHistory>();
		map3D = GetComponent<Map3D>();
		base.Start();
	}

	public void StartReplay()
	{
		base.StartReplay(0);
	}

	void Update ()
	{
		lock (threadShared)
		{
			packet = thread_shared_packet;
			averagedPacketTimeMs = threadShared.averagedPacketTimeMs;
		}
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

		// if we had unprocessed packet last time do it now
		if (plot.plotType != WifiPlotType.None && threadInternal.pending)
		{
			int i = threadInternal.pending_from;
			ulong t = threadInternal.t_from;
			if (TranslateReadingToGlobalReferenceFrame (i, t))
				PushCalculatedReadingsThreadSafe(i);
		}
			
		CalculateReadingsInLocalReferenceFrame (packet);
		if (plot.plotType != WifiPlotType.None)
			if (!TranslateReadingToGlobalReferenceFrame (threadInternal.next, threadInternal.timestamps[threadInternal.next]) )
				return; //don't use the readings yet (or at all), no position data in this timeframe
		
		lastPacket = packet.DeepCopy();

		PushCalculatedReadingsThreadSafe (threadInternal.next);
	}

	public void PushCalculatedReadingsThreadSafe(int at)
	{
		lock (threadShared)
		{
			threadShared.readings [at] = threadInternal.readings [at];

			if (threadShared.consumed)
			{
				threadShared.from = at;
				threadShared.length = 1;
			}
			else //data was not consumed yet
				threadShared.length += 1;

			threadShared.consumed = false;
			threadShared.averagedPacketTimeMs = AveragedPacketTimeMs();
			thread_shared_packet = lastPacket;
		}

	}
		
	private void CalculateReadingsInLocalReferenceFrame(WifiPacket packet)
	{
		Vector3[] readings = threadInternal.readings;
		ulong[] timestamps = threadInternal.timestamps;
		int next = threadInternal.next;

		float height01 = (packet.signal_dbm - plot.minValueDbm) / (plot.maxValueDbm-plot.minValueDbm);
		float heightm = Mathf.LerpUnclamped (plot.minHeight, plot.maxHeight, height01);			
		readings[next]= new Vector3 (wifiPosition.x, heightm , wifiPosition.z);
		timestamps [next] = packet.timestamp_us;
	}

	private bool TranslateReadingToGlobalReferenceFrame(int at, ulong timestamp)
	{
		bool not_in_history, not_yet;
		PositionHistory.PositionSnapshot snapshot= positionHistory.GetPositionSnapshotThreadSafe(timestamp, timestamp, out not_yet, out not_in_history);
		threadInternal.pending = false;

		if (not_in_history)
		{
			print("wifi - ignoring packet (position data not in history) with timestamp " + timestamp);
			return false;
		}
		if (not_yet)
		{
			threadInternal.SetPending (at, 1, timestamp, timestamp);
			return false;
		}
			
		Matrix4x4 robotToGlobal = new Matrix4x4();

		Vector3 scale = Vector3.one;
		PositionData pos = snapshot.PositionAt(timestamp);

		Vector3[] readings = threadInternal.readings;

		robotToGlobal.SetTRS(pos.position, Quaternion.Euler(0.0f, pos.heading, 0.0f), scale);
		readings[at] = robotToGlobal.MultiplyPoint3x4 (readings [at]);

		return true;
	}


	#endregion

	#region Init

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

	#endregion

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

	public override string GetUniqueName ()
	{
		return name;
	}

	public string ModuleCall()
	{
		return "ev3wifi " + network.hostIp + " " + udp.port + " " + module.wirelessDevice + " " + module.pollMs;
	}
	public int ModulePriority()
	{
		return module.priority;
	}
	public bool ModuleAutostart()
	{
		return module.autostart && !replay.ReplayInbound();
	}
	public int CreationDelayMs()
	{
		return module.creationDelayMs;
	}

	public int CompareTo(IRobotModule other)
	{
		return ModulePriority().CompareTo( other.ModulePriority() );
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

}
