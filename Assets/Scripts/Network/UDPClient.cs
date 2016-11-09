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

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using UnityEngine;

public class UDPClient<DATAGRAM> 
	where DATAGRAM : IDatagram, new()
{
	public bool Run { get; set; }

	private UdpClient udpClient;

	private Thread replayThread;

	private byte[] packet_data;
	private BinaryWriter writer;

	private BinaryReader dumpReader;
	private BinaryWriter dumpWriter;

	private ulong firstTimestampUs = ulong.MaxValue; //this is our first timestamp
	private ulong baseline_timestamp_us; //this is baseline timestamp among all replaying servers

	private System.Diagnostics.Stopwatch stopwatch;
	private DATAGRAM packet = new DATAGRAM();

	private IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0); //just not to create it each time

	public UDPClient(string hostname, int udp_port)
	{
		udpClient = new UdpClient(hostname, udp_port);	
		packet_data = new byte[new DATAGRAM().BinarySize()];
		writer = new BinaryWriter(new MemoryStream(packet_data)); 
	}

	public UDPClient(string hostname, int udp_port, string dumpFile, bool record) : this(hostname, udp_port)
	{
		if (record) 
			dumpWriter = new BinaryWriter (File.Open (dumpFile, FileMode.Create, FileAccess.Write, FileShare.Read));
		else //replay
			InitReplayFrom(dumpFile);		
	}
			
	public void InitReplayFrom(string dumpFile)
	{
		stopwatch = new System.Diagnostics.Stopwatch();
		dumpReader = new BinaryReader(File.Open(dumpFile, FileMode.Open, FileAccess.Read));
		packet.FromBinary(dumpReader);
		firstTimestampUs = packet.GetTimestampUs ();
	}

	public void Send(DATAGRAM datagram)
	{
		writer.Seek(0, SeekOrigin.Begin);
		datagram.ToBinary(writer);
		writer.Flush();
		udpClient.Send(packet_data, packet_data.Length);

		if (dumpWriter != null)
			dumpWriter.Write(packet_data);
	}

	public bool IsPacketWaiting()
	{
		return udpClient.Available >= packet.BinarySize();
	}

	//implementation needs work - specifically errors
	public bool ReceiveOne(DATAGRAM datagram)
	{
		byte[] data;
		try
		{
			data=udpClient.Receive(ref remote);					
		}
		catch(SocketException)
		{   
			//Debug.LogError(e.ToString());
			return false;
		}
		catch(ObjectDisposedException)
		{   //socket was closed
			//Debug.LogError(e.ToString());
			return false;
		}

		if (data.Length != datagram.BinarySize())
			throw new InvalidOperationException("Received datagram of incorrect size, different remote?");

		//process datagram

		datagram.FromBinary(new System.IO.BinaryReader(new System.IO.MemoryStream(data)));

		return true;
	}

	public ulong GetFirstReplayTimestamp()
	{
		return firstTimestampUs;
	}
		
	public void StartReplay(ulong base_timestamp_us)
	{
		if (replayThread != null)
			throw new InvalidOperationException("UDP Client Replay is already running");
		if (dumpReader == null)
			throw new InvalidOperationException("Replay was not properly initialized");

		baseline_timestamp_us = base_timestamp_us;
		stopwatch.Start();
		replayThread = new Thread(new ThreadStart(ProcessingThreadMain));
		Run = true;
		replayThread.Start();
	}
	public void Stop()
	{		
		Run = false;
		//this will make udpClient.Send to throw an exception and return from the blocking call
		if(udpClient!=null)
			udpClient.Close();
		if(writer!=null)
			writer.Close();
		if(dumpReader!=null)
			dumpReader.Close();
		if (stopwatch != null)
			stopwatch.Stop();
		if (dumpWriter != null)
			dumpWriter.Close ();
	}

	public void ProcessingThreadMain()
	{
		ulong elapsed_ms;

		try
		{
			while (Run)
			{
				elapsed_ms = baseline_timestamp_us/1000 + (ulong)stopwatch.ElapsedMilliseconds;
				if(elapsed_ms < packet.GetTimestampUs()/1000)
					Thread.Sleep( (int)(packet.GetTimestampUs()/1000 - elapsed_ms) );

				Send(packet);

				packet.FromBinary(dumpReader);
			}
		}
		catch(System.Exception) //whatever happens just finish, to do - finish more softly when file ends, no need for exception
		{
		}
			
		Debug.Log("Replay - finished");

		Run = false;
		stopwatch.Stop ();
		stopwatch = null;
		dumpReader.Close ();
		dumpReader = null;
		replayThread = null;
	}

	public void FlushDump()
	{
		if (dumpWriter == null)
			return;
		dumpWriter.Flush ();
	}
}
