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

public abstract class ReplayableUDPServer<DATAGRAM> : RobotModule, IReplayableUDPServer
	where DATAGRAM : IDatagram, new()
{
	public NetworkProperties udp;

	private UDPServer<DATAGRAM> server;
	private UDPClient<DATAGRAM> client;

	protected abstract void ProcessPacket (DATAGRAM packet);

	protected virtual void OnDestroy()
	{
		print(name + " - stop server");
		server.Stop();
		if (client!=null)
			client.Stop();
	}
		
	protected override void Awake()
	{
		base.Awake ();

		print(name + " port: " + udp.port);

		if(replay.RecordInbound()) 
		{
			print(name + " - dumping packets to '" + Config.DumpPath(robot.sessionDirectory, name) + "'");
			Directory.CreateDirectory(Config.DUMPS_DIRECTORY);
			Directory.CreateDirectory(Config.DumpPath(robot.sessionDirectory));
			server = new UDPServer<DATAGRAM>(udp.port, Config.DumpPath(robot.sessionDirectory, name) );
		} 
		else if (replay.ReplayInbound()) //the server and client reading from dump & sending
		{
			print(name + " - replay from '" + Config.DumpPath(robot.sessionDirectory, name) + "'");
			server = new UDPServer<DATAGRAM>(udp.port);
			try
			{
				client = new UDPClient<DATAGRAM>("localhost", udp.port, Config.DumpPath(robot.sessionDirectory, name), false );
			}
			catch
			{
				print(name + " - replay disabled (can't initialize from '" + Config.DumpPath(robot.sessionDirectory, name) + "' on port " + udp.port + ")");
				replay.mode = UDPReplayMode.None;
			}
		}
		else
			server = new UDPServer<DATAGRAM>(udp.port);
	}

	protected virtual void Start()
	{
		print(name + " - starting server thread");
		server.Start(ProcessPacket);
	}

	protected void StartReplay(int time_offset)
	{
		if (!replay.ReplayInbound())
			return;

		IReplayableUDPServer[] servers=transform.parent.GetComponentsInChildren<IReplayableUDPServer>();
			
		ulong min_timestamp_us = ulong.MaxValue;

		foreach (IReplayableUDPServer rep in servers)
		{
			if (rep.GetFirstPacketTimestampUs() < min_timestamp_us)
				min_timestamp_us = rep.GetFirstPacketTimestampUs();
		}

		ulong start_us = min_timestamp_us;

		if (time_offset >= 0)
			start_us += (ulong) time_offset;
		else
			start_us -= (ulong)(-time_offset);

		client.StartReplay(start_us); 
	}

	public ulong GetFirstPacketTimestampUs()
	{
		if (client == null || !replay.ReplayInbound())
			return ulong.MaxValue;
		return client.GetFirstReplayTimestamp();
	}

	public float AveragedPacketTimeMs()
	{
		return server.AveragedPacketTimeMs();
	}

}
