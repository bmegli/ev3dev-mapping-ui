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

using System.Net;

namespace Ev3devMapping
{

public class DrivePacket : IDatagram
{
	public enum Commands : short {KEEPALIVE=0, SET_SPEED=1, TO_POSITION_WITH_SPEED};

	public ulong timestamp_us;
	public Commands command;
	public short param1;
	public short param2;
	public short param3;
	public short param4;

	public override string ToString()
	{
		return string.Format("[ts={0} cmd={1} a1={2} a2={3} a3={4} a4={5}", timestamp_us, command, param1, param2, param3, param4);
	}

	public void FromBinary(System.IO.BinaryReader reader)
	{
		timestamp_us = (ulong)IPAddress.NetworkToHostOrder(reader.ReadInt64());
		command = (Commands)IPAddress.NetworkToHostOrder(reader.ReadInt16());
		param1 = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		param2 = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		param3 = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		param4 = IPAddress.NetworkToHostOrder(reader.ReadInt16());
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write(IPAddress.HostToNetworkOrder((long)timestamp_us));
		writer.Write(IPAddress.HostToNetworkOrder((short)command));
		writer.Write(IPAddress.HostToNetworkOrder(param1));
		writer.Write(IPAddress.HostToNetworkOrder(param2));
		writer.Write(IPAddress.HostToNetworkOrder(param3));
		writer.Write(IPAddress.HostToNetworkOrder(param4));
	}

	public int BinarySize()
	{
		return 18; // 8 + 5*2=18
	}

	public ulong GetTimestampUs()
	{
		return timestamp_us;
	}
}

} //namespace
