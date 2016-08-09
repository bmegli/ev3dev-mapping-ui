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

public class OdometryPacket : IDatagram
{
	public ulong timestamp_us;
	public int position_left;
	public int position_right;
	public int speed_left;
	public int speed_right;
	public short heading;
	public short angular_speed;

	public float HeadingInDegrees
	{
		get{return heading / 100.0f;}
	}

	public override string ToString()
	{
		return string.Format("[timestamp={0} pl={1} pr={2} sl={3}, sr={4} h={5} as={6}]", timestamp_us, position_left, position_right, speed_left, speed_right, heading, angular_speed);
	}

	public void FromBinary(System.IO.BinaryReader reader)
	{
		timestamp_us = (ulong)IPAddress.NetworkToHostOrder(reader.ReadInt64());
		position_left = IPAddress.NetworkToHostOrder(reader.ReadInt32());
		position_right = IPAddress.NetworkToHostOrder(reader.ReadInt32());
		speed_left = IPAddress.NetworkToHostOrder(reader.ReadInt32());
		speed_right = IPAddress.NetworkToHostOrder(reader.ReadInt32());
		heading = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		angular_speed = IPAddress.NetworkToHostOrder(reader.ReadInt16());
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write(IPAddress.HostToNetworkOrder((long)timestamp_us));
		writer.Write(IPAddress.HostToNetworkOrder(position_left));
		writer.Write(IPAddress.HostToNetworkOrder(position_right));
		writer.Write(IPAddress.HostToNetworkOrder(speed_left));
		writer.Write(IPAddress.HostToNetworkOrder(speed_right));
		writer.Write(IPAddress.HostToNetworkOrder(heading));
		writer.Write(IPAddress.HostToNetworkOrder(angular_speed));
	}

	public int BinarySize()
	{
		return 28; // 8+ 4*4 +2*2
	}

	public void CloneFrom(OdometryPacket p)
	{
		timestamp_us = p.timestamp_us;
		position_left = p.position_left;
		position_right = p.position_right;
		speed_left = p.speed_left;
		speed_right = p.speed_right;
		heading = p.heading;
		angular_speed = p.angular_speed;
	}

	public ulong GetTimestampUs()
	{
		return timestamp_us;
	}
}
