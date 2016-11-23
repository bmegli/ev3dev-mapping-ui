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

public abstract class ReplayableUDPClient<DATAGRAM> : ReplayableClient
	where DATAGRAM : IDatagram, new()
{
	private UDPClient<DATAGRAM> client;
			
	protected void Send(DATAGRAM packet)
	{
		client.Send (packet);
	}

	protected virtual void OnDestroy()
	{
		print(name + " - stop client");
		if(client != null)
			client.Stop();
	}

	protected override void Awake()
	{
		base.Awake ();

		string robotName = transform.parent.name;

		if (replay.RecordOutbound())
		{
			print(name + " - preparing for host: " + network.robotIp + " on port: " + moduleNetwork.port);
			print(name + " - dumping packets to '" + Config.DumpPath(robot.sessionDirectory, robotName, name) + "'");
			Directory.CreateDirectory(Config.DUMPS_DIRECTORY);
			Directory.CreateDirectory(Config.DumpPath(robot.sessionDirectory, robotName));
			client = new UDPClient<DATAGRAM>(network.robotIp, moduleNetwork.port, Config.DumpPath(robot.sessionDirectory, robotName, name), true);
		}
		else if (replay.ReplayOutbound()) //the client reading from dump & sending
		{
			print(name + " - preparing for host: " + network.robotIp + " port: " + moduleNetwork.port);
			print(name + " - replay from '" + Config.DumpPath(robot.sessionDirectory, robotName, name) + "'");

			try
			{
				client = new UDPClient<DATAGRAM>(network.robotIp, moduleNetwork.port, Config.DumpPath(robot.sessionDirectory, robotName, name), false);
			}
			catch
			{
				print(name + " - replay disabled (can't initialize from '" + Config.DumpPath(robot.sessionDirectory, robotName, name) + "' on port " + moduleNetwork.port + ")");
				replay.mode = ReplayMode.None;
				enabled = false;
			}
		}
		else if(replay.ReplayInbound())
			print(name + " - client not started (inbound replay)");
		else
		{
			print(name + " - preparing for host: " + network.robotIp + " port: " + moduleNetwork.port);
			client = new UDPClient<DATAGRAM>(network.robotIp, moduleNetwork.port);
		}
	}

	protected abstract void Start();

	protected bool ReplayRunning
	{
		get {return client.ReplayRunning; }
	}

	protected override void StartReplay(int time_offset)
	{
		if (!replay.ReplayOutbound())
			return;		

		ReplayableClient[] clients=transform.parent.GetComponentsInChildren<ReplayableClient>();

		ulong min_timestamp_us = ulong.MaxValue;
		foreach (ReplayableClient rep in clients)
			if (rep.GetFirstPacketTimestampUs() < min_timestamp_us)
				min_timestamp_us = rep.GetFirstPacketTimestampUs();

		ulong start_us = min_timestamp_us;
		if (time_offset >= 0)
			start_us += (ulong) time_offset;
		else
			start_us -= (ulong)(-time_offset);

		client.StartReplay(start_us); 
	}
	protected void StopReplay()
	{
		print(name + " - stopping replay");
		client.StopReplay ();
	}


	protected void InitReplayFrom(string filename)
	{
		print(name + " - replay from '" + filename + "'");
		client.InitReplayFrom(filename);
	}
	protected void InitRecordTo(string filename)
	{
		print(name + " - dumping packets to '" + filename + "'");
		Directory.CreateDirectory(Config.DUMPS_DIRECTORY);
		Directory.CreateDirectory(Config.DumpPath(robot.sessionDirectory, transform.parent.name));
		client.InitRecordTo(filename);
	}

		
	protected void StartExclusiveReplay()
	{
		client.StartReplay(client.GetFirstReplayTimestamp());
	}

	protected string GetReplayFilename()
	{
		return Config.DumpPath (robot.sessionDirectory, transform.parent.name, name);
	}
	protected string GetReplayFilename(string postfix)
	{
		return Config.DumpPath (robot.sessionDirectory, transform.parent.name ,name + postfix);
	}
		
	protected void FlushDump()
	{
		client.FlushDump ();
	}
		
	public override ulong GetFirstPacketTimestampUs()
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
