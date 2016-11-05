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

using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System;

using UnityEngine;

public enum TCPClientState {Disconnected, Connecting, Connected, Idle};

public class TCPClient<MESSAGE> 
	where MESSAGE : IMessage, new()
{	
	const int INITIAL_BUFFER_SIZE = 256;

	private TcpClient tcpClient;

	private Thread replayThread;

	private string remoteHost;
	private int remotePort;

	private MemoryStream outMemoryStream;
	private MemoryStream inMemoryStream;

	private BinaryWriter writer;
	private BinaryReader reader;

	private BinaryReader dumpReader;
	private BinaryWriter dumpWriter;

	private ulong firstTimestampUs; //this is our first timestamp
	private ulong baseline_timestamp_us; //this is baseline timestamp among all replaying clients

	private System.Diagnostics.Stopwatch lastSeenStopwatch;
	private System.Diagnostics.Stopwatch stopwatch;
	private MESSAGE message = new MESSAGE();

	private bool receivedHeader=false;
	private int receivedPayloadSize = 0;

	public long LastSeen { get {return lastSeenStopwatch.ElapsedMilliseconds ; } }
	public bool Run { get; set; }

	public TCPClientState State { get; private set; }

	public SocketException LastError { get; private set; }

	public TCPClient(string host, int port)
	{
		tcpClient = new TcpClient ();
		outMemoryStream = new MemoryStream (INITIAL_BUFFER_SIZE);
		inMemoryStream = new MemoryStream (INITIAL_BUFFER_SIZE);
		writer = new BinaryWriter(outMemoryStream); 
		reader = new BinaryReader (inMemoryStream);
		remoteHost = host;
		remotePort = port;
		lastSeenStopwatch = new System.Diagnostics.Stopwatch();
	}

	public TCPClient(string host, int port, string dumpfile, bool record)
		: this(host, port)
	{
		stopwatch = new System.Diagnostics.Stopwatch();

		if(record)
			dumpWriter = new BinaryWriter(File.Open(dumpfile, FileMode.Create));
		else //replay
		{
			dumpReader = new BinaryReader(File.Open(dumpfile, FileMode.Open));
			message.FromBinary(dumpReader);
			firstTimestampUs = message.GetTimestampUs ();
		}
	}

	public void StartConnecting()
	{
		if (State == TCPClientState.Connected || State == TCPClientState.Connected)
			throw new InvalidOperationException ("Unable to start connecting, already connected or connecting");
	
		tcpClient.BeginConnect (remoteHost, remotePort, OnConnect, tcpClient);

		State = TCPClientState.Connecting;
	}

	private void OnConnect(IAsyncResult result)
	{
		try
		{
			tcpClient.EndConnect(result);
		}
		catch(SocketException exc)
		{
			LastError = exc;
		}
			
		if (tcpClient.Connected)
		{
			lastSeenStopwatch.Reset();
			lastSeenStopwatch.Start();
			State = TCPClientState.Connected;
		}
		else
		{
			State = TCPClientState.Idle;
		}
	}
		
	public void Disconnect()
	{
		if (State == TCPClientState.Disconnected || State == TCPClientState.Idle)
			return;
		if (tcpClient == null)
			return;
		if(tcpClient.Connected)
			tcpClient.GetStream().Close();
		tcpClient.Close();
		tcpClient = new TcpClient();
		State = TCPClientState.Idle;
	}

	public void Stop()
	{
		Run = false;

		if (lastSeenStopwatch != null)
			lastSeenStopwatch.Stop();

		if (tcpClient != null)
		{
			if(tcpClient.Connected)
				tcpClient.GetStream().Close();
			tcpClient.Close();
		}
		if (reader != null)
			reader.Close();
		if (writer != null)
			writer.Close();

		if (stopwatch != null)
			stopwatch.Stop();
	
		if (dumpReader != null)
			dumpReader.Close ();
		if (dumpWriter != null)
			dumpWriter.Close ();
	}

	public bool ReceiveOne(MESSAGE msg)
	{
		if (!receivedHeader)
		{
			if (tcpClient.Available < msg.HeaderSize ())
				return false;

			if(inMemoryStream.Capacity < msg.HeaderSize())
				inMemoryStream.Capacity = msg.HeaderSize();

			tcpClient.GetStream().Read(inMemoryStream.GetBuffer(), 0, msg.HeaderSize());
			reader.BaseStream.Position = 0;
			reader.BaseStream.SetLength(msg.HeaderSize());
		
			receivedPayloadSize=msg.PayloadSize (reader);

			if (inMemoryStream.Capacity < msg.HeaderSize () + receivedPayloadSize)
				inMemoryStream.Capacity = msg.HeaderSize () + receivedPayloadSize;

			reader.BaseStream.SetLength(msg.HeaderSize() + receivedPayloadSize);
		}

		if (tcpClient.Available < receivedPayloadSize)
			return false;

		if (receivedPayloadSize > 0)
			tcpClient.GetStream().Read(inMemoryStream.GetBuffer(), msg.HeaderSize(), receivedPayloadSize);

		reader.BaseStream.Position = 0;

		receivedHeader = false;
		receivedPayloadSize = 0;

		msg.FromBinary (reader);

		lastSeenStopwatch.Reset();
		lastSeenStopwatch.Start();

		return true;
	}

	public void Send(MESSAGE message)
	{
		writer.Seek(0, SeekOrigin.Begin);
		int packet_length=message.ToBinary(writer);
		writer.Flush();

		try
		{
			tcpClient.GetStream().Write (outMemoryStream.GetBuffer(), 0, packet_length);

			lastSeenStopwatch.Reset();
			lastSeenStopwatch.Start();

			if (dumpWriter != null)
				dumpWriter.Write (outMemoryStream.GetBuffer(), 0, packet_length);
			
		}
		catch(SocketException exc)
		{
			State = TCPClientState.Idle;
			LastError = exc;
		}
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

	private void ProcessingThreadMain()
	{
		ulong elapsed_ms;

		try
		{
			while (Run && State == TCPClientState.Connected)
			{
				elapsed_ms = baseline_timestamp_us/1000 + (ulong)stopwatch.ElapsedMilliseconds;
				if(elapsed_ms < message.GetTimestampUs()/1000)
					Thread.Sleep( (int)(message.GetTimestampUs()/1000 - elapsed_ms) );

				Send(message);

				message.FromBinary(dumpReader);
			}
		}
		catch(System.Exception) //whatever happens just finish, to do - finish more softly when file ends, no need for exception
		{
		}

		Debug.Log("Replay - finished");
		Stop();
	}
}

