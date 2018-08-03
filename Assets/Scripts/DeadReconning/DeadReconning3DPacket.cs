/*
 * Copyright (C) 2018 Bartosz Meglicki <meglickib@gmail.com>
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
using System;

namespace Ev3devMapping
{

public class DeadReconning3DPacket : IDatagram
{
	public ulong timestamp_us;
	public int position_left;
	public int position_right;
	public short battery_voltage; //temp in 10*Volts
	//in degrees, consider array or vector4
	public float quat_w;
	public float quat_x; 
	public float quat_y;
	public float quat_z;

	/*
	public float HeadingInDegrees
	{
		get{return euler_y;}
	}
	*/

	public override string ToString()
	{
		return string.Format("[timestamp={0} pl={1} pr={2} w={3} x={4} y={5} z={6}]", timestamp_us, position_left, position_right, quat_w, quat_x, quat_y, quat_z);
	}

	public void FromBinary(System.IO.BinaryReader reader)
	{
		byte[] data;
		timestamp_us = (ulong)IPAddress.NetworkToHostOrder(reader.ReadInt64());
		position_left = IPAddress.NetworkToHostOrder(reader.ReadInt32());
		position_right = IPAddress.NetworkToHostOrder(reader.ReadInt32());
		battery_voltage = IPAddress.NetworkToHostOrder (reader.ReadInt16 ());

		data = reader.ReadBytes(4);
		Array.Reverse (data);
		quat_w=BitConverter.ToSingle (data, 0);

		data = reader.ReadBytes(4);
		Array.Reverse (data);
		quat_x=BitConverter.ToSingle (data, 0);

		data = reader.ReadBytes(4);
		Array.Reverse (data);
		quat_y=BitConverter.ToSingle (data, 0);

		data = reader.ReadBytes(4);
		Array.Reverse (data);
		quat_z=BitConverter.ToSingle (data, 0);
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		byte[] data;
		writer.Write(IPAddress.HostToNetworkOrder((long)timestamp_us));
		writer.Write(IPAddress.HostToNetworkOrder(position_left));
		writer.Write(IPAddress.HostToNetworkOrder(position_right));
		writer.Write(IPAddress.HostToNetworkOrder(battery_voltage));

		data = BitConverter.GetBytes (quat_w);
		Array.Reverse (data);
		writer.Write(data);

		data = BitConverter.GetBytes (quat_x);
		Array.Reverse (data);
		writer.Write(data);

		data = BitConverter.GetBytes (quat_y);
		Array.Reverse (data);
		writer.Write(data);

		data = BitConverter.GetBytes (quat_z);
		Array.Reverse (data);
		writer.Write(data);
	}

	public int BinarySize()
	{
		return 34; // 8 + 2*4 + 2 + 4*4
	}

	public void CloneFrom(DeadReconning3DPacket p)
	{
		timestamp_us = p.timestamp_us;
		position_left = p.position_left;
		position_right = p.position_right;
		quat_w = p.quat_w;
		quat_x = p.quat_x;
		quat_y = p.quat_y;
		quat_z = p.quat_z;
	}

	public ulong GetTimestampUs()
	{
		return timestamp_us;
	}
}

} //namespace