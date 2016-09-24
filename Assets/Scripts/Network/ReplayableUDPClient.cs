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
using System.Collections.Generic;

public abstract class ReplayableUDPClient<DATAGRAM> : MonoBehaviour, IReplayableUDPClient
	where DATAGRAM : IDatagram, new()
{
	public UDPProperties udp;

	private UDPClient<DATAGRAM> client;

	public virtual string GetUniqueName()
	{
		return "udp";
	}
		
	protected void Send(DATAGRAM packet)
	{
		client.Send (packet);
	}

	protected virtual void OnDestroy()
	{
		print(GetUniqueName() + " - stop client");
		client.Stop();
	}

	protected virtual void Awake()
	{
		print(GetUniqueName() + " - port: " + udp.port);

		if (udp.replayMode == UDPReplayMode.None) 
			client = new UDPClient<DATAGRAM>(GetComponentInParent<Network>().robotIp, udp.port);
		else if (udp.replayMode == UDPReplayMode.Record) 
		{
			print(GetUniqueName() + " - dumping packets to '" + udp.dumpFilename + "'");
			client = new UDPClient<DATAGRAM>(GetComponentInParent<Network>().robotIp, udp.port, Config.DumpPath(udp.dumpFilename), true);
		} 
		else if (udp.replayMode == UDPReplayMode.Replay) //the client reading from dump & sending
		{
			print(GetUniqueName() + " - replay from '" + udp.dumpFilename + "'");

			try
			{
				client = new UDPClient<DATAGRAM>(GetComponentInParent<Network>().robotIp, udp.port, Config.DumpPath(udp.dumpFilename), false);
			}
			catch
			{
				print(GetUniqueName() + " - replay disabled (can't initialize from '" + udp.dumpFilename + "' on port " + udp.port + ")");
				udp.replayMode = UDPReplayMode.None;
				enabled = false;
			}

		}
	}

	protected abstract void Start();

	protected void StartReplay(int time_offset)
	{
		if (udp.replayMode != UDPReplayMode.Replay)
			return;

		IReplayableUDPClient[] clients=transform.parent.GetComponentsInChildren<IReplayableUDPClient>();

		ulong min_timestamp_us = ulong.MaxValue;
		foreach (IReplayableUDPClient rep in clients)
			if (rep.GetFirstPacketTimestampUs() < min_timestamp_us)
				min_timestamp_us = rep.GetFirstPacketTimestampUs();

		ulong start_us = min_timestamp_us;
		if (time_offset >= 0)
			start_us += (ulong) time_offset;
		else
			start_us -= (ulong)(-time_offset);

		client.StartReplay(start_us); 
	}

	public ulong GetFirstPacketTimestampUs()
	{
		if (client == null || udp.replayMode != UDPReplayMode.Replay)
			return ulong.MaxValue;
		return client.GetFirstReplayTimestamp();
	}

	public bool IsPacketWaiting()
	{
		return client.IsPacketWaiting();
	}
	public bool ReceiveOne(DATAGRAM datagram)
	{
		return client.ReceiveOne(datagram);
	}
}
