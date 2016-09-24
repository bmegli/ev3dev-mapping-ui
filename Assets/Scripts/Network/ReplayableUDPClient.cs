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
using System.IO;

public abstract class ReplayableUDPClient<DATAGRAM> : MonoBehaviour, IReplayableUDPClient
	where DATAGRAM : IDatagram, new()
{
	public UDPProperties udp;

	protected Network network;
	protected Replay replay;

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
		if(client != null)
			client.Stop();
	}

	protected virtual void Awake()
	{
		network = GetComponentInParent<Network>().DeepCopy();
		replay = GetComponentInParent<Replay>().DeepCopy();

		print(GetUniqueName() + " - port: " + udp.port);

		if (replay.RecordOutbound()) 
		{
			print(GetUniqueName() + " - dumping packets to '" + Config.DumpPath(replay.directory, GetUniqueName()) + "'");
			Directory.CreateDirectory(Config.DumpPath(replay.directory));
			client = new UDPClient<DATAGRAM>(network.robotIp, udp.port, Config.DumpPath(replay.directory, GetUniqueName()), true);
		} 
		else if (replay.ReplayOutbound()) //the client reading from dump & sending
		{
			print(GetUniqueName() + " - replay from '" + Config.DumpPath(replay.directory, GetUniqueName()) + "'");

			try
			{
				client = new UDPClient<DATAGRAM>(network.robotIp, udp.port, Config.DumpPath(replay.directory, GetUniqueName()), false);
			}
			catch
			{
				print(GetUniqueName() + " - replay disabled (can't initialize from '" + Config.DumpPath(replay.directory, GetUniqueName()) + "' on port " + udp.port + ")");
				replay.mode = UDPReplayMode.None;
				enabled = false;
			}
		}
		else
			client = new UDPClient<DATAGRAM>(network.robotIp, udp.port);
	}

	protected abstract void Start();

	protected void StartReplay(int time_offset)
	{
		if (!replay.ReplayOutbound())
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
		if (client == null || !replay.ReplayOutbound())
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
