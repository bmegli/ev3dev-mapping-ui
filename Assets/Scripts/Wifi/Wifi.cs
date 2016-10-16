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

[Serializable]
public class WifiModuleProperties : ModuleProperties
{
	public string wirelessDevice = "wlan0";
	public uint pollMs = 100;
}

[RequireComponent (typeof (WifiUI))]
public class Wifi : ReplayableUDPServer<WifiPacket>, IRobotModule
{	
	public WifiModuleProperties module;

	private WifiPacket packet=new WifiPacket();
	private PositionHistory positionHistory;
	private float averagedPacketTimeMs;


	#region UDP Thread Only Data
	private WifiPacket lastPacket=new WifiPacket();
	#endregion

	#region Thread Shared Data
	private object wifiLock=new object();
	private WifiPacket thread_shared_packet=new WifiPacket();
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
		positionHistory = SafeGetComponentInParent<PositionHistory>();
		base.Start();
	}

	public void StartReplay()
	{
		base.StartReplay(0);
	}

	void Update ()
	{
		lock (wifiLock)
		{
			packet = thread_shared_packet;
			averagedPacketTimeMs = thread_shared_averaged_packet_time_ms;
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
			
		lastPacket = packet.DeepCopy();

		lock (wifiLock)
		{
			thread_shared_packet = lastPacket; //just pass the reference!
			thread_shared_averaged_packet_time_ms = AveragedPacketTimeMs();
		}
		//we no longer can use last packet here, reference passed to other thread
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
