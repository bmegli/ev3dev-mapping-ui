﻿using UnityEngine;
using System.Collections;

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
			client = new UDPClient<DATAGRAM>(udp.host, udp.port);
		else if (udp.replayMode == UDPReplayMode.Record) 
		{
			print(GetUniqueName() + " - dumping packets to '" + udp.dumpFilename + "'");
			client = new UDPClient<DATAGRAM>(udp.host, udp.port, Config.DumpPath(udp.dumpFilename), true);
		} 
		else if (udp.replayMode == UDPReplayMode.Replay) //the client reading from dump & sending
		{
			print(GetUniqueName() + " - replay from '" + udp.dumpFilename + "'");

			try
			{
				client = new UDPClient<DATAGRAM>(udp.host, udp.port, Config.DumpPath(udp.dumpFilename), false);
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

		IReplayableUDPClient[] reps = GetComponentsInParent<IReplayableUDPClient>();
		ulong min_timestamp_us = ulong.MaxValue;
		foreach (IReplayableUDPClient rep in reps)
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
