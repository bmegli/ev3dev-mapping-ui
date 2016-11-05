﻿/*
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

public abstract class ReplayableTCPClient<MESSAGE> : RobotModule
	where MESSAGE : IMessage
{
	public NetworkProperties tcp;

	private TCPClient<MESSAGE> client;

	protected TCPClientState TCPClientState
	{
		get {return client.State;}
	}

	protected string LastErrorMessage
	{
		get{return client.LastError.Message; }
	}
	protected int LastErrorCode
	{
		get{return client.LastError.ErrorCode; }
	}
	protected long LastSeen
	{
		get{return client.LastSeen; }
	}

	protected override void Awake()
	{
		base.Awake ();

		print(name + " - address " + network.robotIp  + " port: " + tcp.port);
		client = new TCPClient<MESSAGE>(network.robotIp, tcp.port);
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
		print(name + " - connecting to " + network.robotIp  + " port: " + tcp.port);
		client.StartConnecting ();
	}
		
	protected void Disconnect()
	{
		if (client.State == TCPClientState.Disconnected || client.State == TCPClientState.Idle)
		{
			print(name + " - can't disconnect (not connected)");
			return;
		}
		print(name + " - disconnecting from " + network.robotIp  + " port: " + tcp.port);
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

}