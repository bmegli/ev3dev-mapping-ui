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

public class LaserReading
{
	public ushort angle_q14;
	public ushort distance_mm;

	public float AngleDeg
	{
			get{return angle_q14 * 90.0f / (1 << 14);}
	}

	public void FromBinary(System.IO.BinaryReader reader)
	{
		angle_q14 = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
		distance_mm = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write(IPAddress.HostToNetworkOrder((short)angle_q14));
		writer.Write(IPAddress.HostToNetworkOrder((short)distance_mm));
	}


	public override string ToString()
	{
			return string.Format("[LR: a={0}, d={1}]", angle_q14, distance_mm);
	}

	public int BinarySize()
	{
		return 4;
	}

	public ushort distance
	{
		get
		{
			return distance_mm;
		}
		set
		{
			distance_mm = value;
		}
	}
}

public class LaserPacket : IDatagram
{
	public const int LASER_READINGS_PER_PACKET = 96;

	public const ulong MICROSECONDS_PER_MINUTE = 60 * 1000000;

	public ulong timestamp_us;
	public ushort sample_us; 

	/// laser_readings[LASER_READINGS_PER_PACKET]
	public LaserReading[] laser_readings = new LaserReading[LASER_READINGS_PER_PACKET];

	public LaserPacket()
	{
		for (int i = 0; i < laser_readings.Length; ++i)
			laser_readings[i] = new LaserReading();
	}

	public ulong GetEndTimestampUs()
	{
        return GetTimestampUs(LASER_READINGS_PER_PACKET-1);
	}

	public ulong GetTimestampUs()
	{
		return timestamp_us;
	}
	public ulong GetTimestampUs(int reading)
	{
		return timestamp_us + (ulong)reading * sample_us;
	}
		
	public void FromBinary(System.IO.BinaryReader reader)
	{
		timestamp_us = (ulong)IPAddress.NetworkToHostOrder(reader.ReadInt64());
		sample_us = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

		for (int i = 0; i < laser_readings.Length; ++i)
			laser_readings[i].FromBinary(reader);	
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write(IPAddress.HostToNetworkOrder((long)timestamp_us));
		writer.Write(IPAddress.HostToNetworkOrder((short)sample_us));

		for (int i = 0; i < laser_readings.Length; ++i)
			laser_readings[i].ToBinary(writer);	
	}
			
	public int BinarySize()
	{
		return 8 + 2 + 4 * LASER_READINGS_PER_PACKET;
	}

	public override string ToString()
	{
		return string.Format("[ts={0} us={1}", timestamp_us, sample_us) + laser_readings[3].ToString();
	}
}

} //namespace