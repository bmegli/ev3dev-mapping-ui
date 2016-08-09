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

public abstract class ReplayableUDPServer<DATAGRAM> : MonoBehaviour, IReplayableUDPServer
	where DATAGRAM : IDatagram, new()
{
	public UDPProperties udp;

	private UDPServer<DATAGRAM> server;
	private UDPClient<DATAGRAM> client;

	public virtual string GetUniqueName()
	{
		return "udp";
	}

	protected abstract void ProcessPacket (DATAGRAM packet);

	protected virtual void OnDestroy()
	{
		print(GetUniqueName() + " - stop server");
		server.Stop();
		if (client!=null)
			client.Stop();
	}
		
	protected virtual void Awake()
	{
		print(GetUniqueName() + " port: " + udp.port);

		if (udp.replayMode == UDPReplayMode.None) 
			server = new UDPServer<DATAGRAM>(udp.port);
		else if (udp.replayMode == UDPReplayMode.Record) 
		{
			print(GetUniqueName() + " - dumping packets to '" + udp.dumpFilename + "'");
			server = new UDPServer<DATAGRAM>(udp.port, Config.DumpPath(udp.dumpFilename) );
		} 
		else if (udp.replayMode == UDPReplayMode.Replay) //the server and client reading from dump & sending
		{
			print(GetUniqueName() + " - replay from '" + udp.dumpFilename + "'");
			server = new UDPServer<DATAGRAM>(udp.port);
			try
			{
				client = new UDPClient<DATAGRAM>("localhost", udp.port, Config.DumpPath(udp.dumpFilename), false );
			}
			catch
			{
				print(GetUniqueName() + " - replay disabled (can't initialize from '" + udp.dumpFilename + "' on port " + udp.port + ")");
				udp.replayMode = UDPReplayMode.None;
			}
		}
	}

	protected virtual void Start()
	{
		print(GetUniqueName() + " - starting server thread");
		server.Start(ProcessPacket);
	}

	protected void StartReplay(int time_offset)
	{
		if (udp.replayMode != UDPReplayMode.Replay)
			return;

		IReplayableUDPServer[] reps = GetComponentsInParent<IReplayableUDPServer>();
		ulong min_timestamp_us = ulong.MaxValue;
		foreach (IReplayableUDPServer rep in reps)
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

	public float AveragedPacketTimeMs()
	{
		return server.AveragedPacketTimeMs();
	}

}
