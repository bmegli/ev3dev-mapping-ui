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

public delegate void DatagramHandler<DATAGRAM>(DATAGRAM data); 

public class UDPServer<DATAGRAM> 
	where DATAGRAM : IDatagram, new()
{
	public bool Run { get; set; }

	private UdpClient udpClient;
	private Thread recvThread;
	private DatagramHandler<DATAGRAM> onDatagram;
	private BinaryWriter dumpWriter;

	private IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
	private IPEndPoint expectedRemote;

	//packets per second statistics
	private ulong lastPacketTimeUs=0; 
	private float avgPacketTimeMs=0.0f;

	public UDPServer(string host, string remote, int udp_port)
	{
		expectedRemote = new IPEndPoint(IPAddress.Parse(remote), udp_port);
		udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(host), udp_port));
	}

	//can throw exception if unable to create file
	public UDPServer(string host, string remote, int udp_port, string dumpFile) : this(host, remote, udp_port)
	{
		dumpWriter = new BinaryWriter(File.Open(dumpFile, FileMode.Create, FileAccess.Write));
	}
		
	public void Start(DatagramHandler<DATAGRAM> datagramFunction)
	{
		if (recvThread != null)
			throw new InvalidOperationException("UDP Server is already running");
		if (datagramFunction == null)
			throw new InvalidOperationException("No DatagramHandler specified, can't start processing thread");

		onDatagram = datagramFunction;
		recvThread = new Thread(new ThreadStart(ReceiverThreadMain));
		Run = true;
		recvThread.Start();

	}
	public void Stop()
	{		
		Run = false;
		//this will make udpClient.Receive to throw an exception and return from the blocking call
		udpClient.Close();
	}
					
	public void ReceiverThreadMain()
	{
		byte[] data = null;
		DATAGRAM datagram=new DATAGRAM();

		int datagram_size = datagram.BinarySize();

		while (Run)
		{
			try
			{
				data=udpClient.Receive(ref remote);			
				if(!remote.Address.Equals(expectedRemote.Address))
				{
					Debug.Log("Ignoring packet from " + remote);
					continue;
				}
			}
			catch(SocketException)
			{   
				//Debug.LogError(e.ToString());
				break;
			}
			catch(ObjectDisposedException)
			{   //socket was closed
				//Debug.LogError(e.ToString());
				Run = false;
				break;
			}
	
			if (data.Length != datagram_size)
				throw new InvalidOperationException("Received datagram of incorrect size, different remote?");

			//gather server stats
			if(lastPacketTimeUs==0) //first packet case
				lastPacketTimeUs=Timestamp.TimestampUs();
			else
				UpdatePacketFrequencyStatistics();

			//process datagram
			datagram.FromBinary(new System.IO.BinaryReader(new System.IO.MemoryStream(data)));
			onDatagram(datagram);
	
			if (dumpWriter != null)
				dumpWriter.Write(data);
		}
			
		if(dumpWriter!=null)
			dumpWriter.Close();
		onDatagram = null;
	}

	// can be called only from datagram function (same thread)
	public float AveragedPacketTimeMs()
	{
		return avgPacketTimeMs;
	}

	private void UpdatePacketFrequencyStatistics()
	{
		//simple exponential smoothing of timings
		ulong currentUs = Timestamp.TimestampUs();
		float timeDiffMs = (currentUs - lastPacketTimeUs)/1000.0f;

		avgPacketTimeMs += (timeDiffMs - avgPacketTimeMs) * Constants.ExponentialSmoothingAlpha; 
		lastPacketTimeUs = currentUs;
	}


}
