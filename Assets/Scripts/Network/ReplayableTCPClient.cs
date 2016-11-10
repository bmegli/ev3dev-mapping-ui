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
using System.IO;

public abstract class ReplayableTCPClient<MESSAGE> : ReplayableClient
	where MESSAGE : IMessage, new()
{
	private TCPClient<MESSAGE> client;

	protected TCPClientState TCPClientState
	{
		get {return client.State;}
	}

	protected string LastErrorMessage
	{
		get{return client.LastError.Message; }
	}
	protected long LastSeen
	{
		get{return client.LastSeen; }
	}

	protected override void Awake()
	{
		base.Awake ();

		print(name + " - address " + network.robotIp  + " port: " + moduleNetwork.port);

		if (replay.RecordOutbound()) 
		{
			print(name + " - dumping packets to '" + Config.DumpPath(robot.sessionDirectory, name) + "'");
			Directory.CreateDirectory(Config.DUMPS_DIRECTORY);
			Directory.CreateDirectory(Config.DumpPath(robot.sessionDirectory));
			client = new TCPClient<MESSAGE>(network.robotIp, moduleNetwork.port, Config.DumpPath(robot.sessionDirectory, name), true);
		}
		else if (replay.ReplayOutbound()) //the client reading from dump & sending
		{
			print(name + " - replay from '" + Config.DumpPath(robot.sessionDirectory, name) + "'");

			try
			{
				client = new TCPClient<MESSAGE>(network.robotIp, moduleNetwork.port, Config.DumpPath(robot.sessionDirectory, name), false);
			}
			catch(System.Exception)
			{
				print(name + " - replay disabled (can't initialize from '" + Config.DumpPath(robot.sessionDirectory, name) + "' on port " + moduleNetwork.port + ")");
				replay.mode = ReplayMode.None;
				enabled = false;
			}
		}
		else
			client = new TCPClient<MESSAGE>(network.robotIp, moduleNetwork.port);
	}
		
	protected virtual void OnDestroy()
	{
		print(name + " - stop client");
		if(client != null)
			client.Stop();

	}
				
	protected abstract void Start();

	protected void StartConnecting()
	{
		print(name + " - connecting to " + network.robotIp  + " port: " + moduleNetwork.port);
		client.StartConnecting ();
	}
		
	protected void Disconnect()
	{
		if (client.State == TCPClientState.Disconnected || client.State == TCPClientState.Idle)
		{
			print(name + " - can't disconnect (not connected)");
			return;
		}
		print(name + " - disconnecting from " + network.robotIp  + " port: " + moduleNetwork.port);
		client.Disconnect();
	}

	protected void Send(MESSAGE message)
	{
		client.Send(message);
	}
		
	protected bool ReceiveOne(MESSAGE msg)
	{
		try
		{
			return client.ReceiveOne(msg);
		}
		catch(System.ArgumentException exc)
		{
			print (name + " - ignoring malformed message " + exc.ToString());
		}
		return false;
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

	public override ulong GetFirstPacketTimestampUs()
	{
		if (client == null || !replay.ReplayOutbound())
			return ulong.MaxValue;
		return client.GetFirstReplayTimestamp();
	}
}
