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

public class TCPClient<MESSAGE> 
	where MESSAGE : IMessage
{
	const int INITIAL_BUFFER_SIZE = 256;

	private TcpClient tcpClient;
	private NetworkStream stream;
	private string remoteHost;
	private int remotePort;

	private MemoryStream outMemoryStream;
	private MemoryStream inMemoryStream;

	private BinaryWriter writer;
	private BinaryReader reader;

//	private BinaryReader dumpReader;
//	private BinaryWriter dumpWriter;

	private ulong firstTimestampUs; //this is our first timestamp
	private ulong baseline_timestamp_us; //this is baseline timestamp among all replaying servers

//	private System.Diagnostics.Stopwatch stopwatch;

	private bool receivedHeader=false;
	private int receivedPayloadSize = 0;

	public TCPClient(string host, int port)
	{
		tcpClient = new TcpClient ();
		outMemoryStream = new MemoryStream (INITIAL_BUFFER_SIZE);
		inMemoryStream = new MemoryStream (INITIAL_BUFFER_SIZE);
		writer = new BinaryWriter(outMemoryStream); 
		reader = new BinaryReader (inMemoryStream);
		remoteHost = host;
		remotePort = port;
	}
		
	public void Connect()
	{		
		tcpClient.Connect (remoteHost, remotePort);
		stream = tcpClient.GetStream ();
	}


	public void Stop()
	{
		if (tcpClient != null)
			tcpClient.Close ();
		if (stream != null)
			stream.Close ();
		if (reader != null)
			reader.Close();
		if (writer != null)
			writer.Close();
		/*
		if (dumpReader != null)
			dumpReader.Close ();
		if (stopwatch != null)
			stopwatch.Stop();
		if (dumpWriter != null)
			dumpWriter.Close ();
		*/
	}

	public bool ReceiveOne(MESSAGE msg)
	{
		if (!receivedHeader)
		{
			if (tcpClient.Available < msg.HeaderSize ())
				return false;

			if(inMemoryStream.Capacity < msg.HeaderSize())
				inMemoryStream.Capacity = msg.HeaderSize();

			stream.Read(inMemoryStream.GetBuffer(), 0, msg.HeaderSize());
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
			stream.Read(inMemoryStream.GetBuffer(), msg.HeaderSize(), receivedPayloadSize);

		reader.BaseStream.Position = 0;

		receivedHeader = false;
		receivedPayloadSize = 0;

		msg.FromBinary (reader);

		return true;
	}

	public void Send(MESSAGE message)
	{
		writer.Seek(0, SeekOrigin.Begin);
		int packet_length=message.ToBinary(writer);
		writer.Flush();
		stream.Write (outMemoryStream.GetBuffer(), 0, packet_length);
	
//		if (dumpWriter != null)
//			dumpWriter.Write (messageMemoryStream.GetBuffer(), 0, packet_length);
	}

}

