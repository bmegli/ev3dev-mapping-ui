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

public abstract class ReplayableUDPServer<DATAGRAM> : ReplayableServer
	where DATAGRAM : IDatagram, new()
{
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

		string robotName = transform.parent.name;

		if (replay.RecordInbound())
		{
			print(name + " - awaiting client " + network.robotIp + " on " + network.hostIp + ":" + moduleNetwork.port);
			print(name + " - dumping packets to '" + Config.DumpPath(robot.sessionDirectory, robotName, name) + "'");
			Directory.CreateDirectory(Config.DumpPath(robot.sessionDirectory, robotName));
			server = new UDPServer<DATAGRAM>(network.hostIp, network.robotIp, moduleNetwork.port, Config.DumpPath(robot.sessionDirectory, robotName, name));
		}
		else if (replay.ReplayInbound()) //the server and client reading from dump & sending
		{
			print(name + " - awaiting client 127.0.0.1 on 127.0.0.1:" + moduleNetwork.port);
			print(name + " - replay from '" + Config.DumpPath(robot.sessionDirectory, robotName, name) + "'");
			server = new UDPServer<DATAGRAM>("127.0.0.1","127.0.0.1", moduleNetwork.port);

			try
			{
				client = new UDPClient<DATAGRAM>("127.0.0.1", moduleNetwork.port, Config.DumpPath(robot.sessionDirectory, robotName, name), false);
			}
			catch
			{
				print(name + " - replay disabled (can't initialize from '" + Config.DumpPath(robot.sessionDirectory, robotName, name) + "' on port " + moduleNetwork.port + ")");
				replay.mode = ReplayMode.None;
			}
		}
		else
		{
			print(name + " - awaiting client " + network.robotIp + " on " +  network.hostIp + ":" + moduleNetwork.port);
			server = new UDPServer<DATAGRAM>(network.hostIp, network.robotIp, moduleNetwork.port);
		}
	}

	protected virtual void Start()
	{
		print(name + " - starting server thread");
		server.Start(ProcessPacket);
	}

	protected override void StartReplay(int time_offset)
	{
		if (!replay.ReplayInbound())
			return;

		ReplayableServer[] servers=transform.parent.GetComponentsInChildren<ReplayableServer>();
			
		ulong min_timestamp_us = ulong.MaxValue;

		foreach (ReplayableServer rep in servers)
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

	public override ulong GetFirstPacketTimestampUs()
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
