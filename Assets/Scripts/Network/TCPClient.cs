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

	private MemoryStream messageMemoryStream;

	private BinaryWriter writer;

//	private BinaryReader dumpReader;
//	private BinaryWriter dumpWriter;

	private ulong firstTimestampUs; //this is our first timestamp
	private ulong baseline_timestamp_us; //this is baseline timestamp among all replaying servers

//	private System.Diagnostics.Stopwatch stopwatch;

	public TCPClient(string host, int port)
	{
		tcpClient = new TcpClient ();
		messageMemoryStream = new MemoryStream (INITIAL_BUFFER_SIZE);
		writer = new BinaryWriter(messageMemoryStream); 
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
		/*
		if (dumpReader != null)
			dumpReader.Close ();
		if (stopwatch != null)
			stopwatch.Stop();
		if (dumpWriter != null)
			dumpWriter.Close ();
		*/
	}

	public void Send(MESSAGE message)
	{
		writer.Seek(0, SeekOrigin.Begin);
		int packet_length=message.ToBinary(writer);
		writer.Flush();
		stream.Write (messageMemoryStream.GetBuffer(), 0, packet_length);
	
//		if (dumpWriter != null)
//			dumpWriter.Write (messageMemoryStream.GetBuffer(), 0, packet_length);
	}

}

