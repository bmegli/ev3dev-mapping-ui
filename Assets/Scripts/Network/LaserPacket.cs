using System.Net;

public class LaserReading
{
	/// distance : 14
	///strength_warning : 1
	///invalid_data : 1
	public ushort distance_w_i;
	/// unsigned short
	public ushort signal_strength;

	public void FromBinary(System.IO.BinaryReader reader)
	{
		distance_w_i = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
		signal_strength = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write(IPAddress.HostToNetworkOrder((short)distance_w_i));
		writer.Write(IPAddress.HostToNetworkOrder((short)signal_strength));
	}

	public override string ToString()
	{
		return string.Format("[LR: d={0}, sw={1}, id={2}]", distance, strength_warning, invalid_data);
	}

	public int BinarySize()
	{
		return 4;
	}

	public ushort distance
	{
		get
		{
			return ((ushort)((this.distance_w_i & 16383u)));
		}
		set
		{
			this.distance_w_i = ((ushort)((value | this.distance_w_i)));
		}
	}

	public ushort strength_warning
	{
		get
		{
			return ((ushort)(((this.distance_w_i & 16384u)
				/ 16384)));
		}
		set
		{
			this.distance_w_i = ((ushort)(((value * 16384)
				| this.distance_w_i)));
		}
	}

	public ushort invalid_data
	{
		get
		{
			return ((ushort)(((this.distance_w_i & 32768u)
				/ 32768)));
		}
		set
		{
			this.distance_w_i = ((ushort)(((value * 32768)
				| this.distance_w_i)));
		}
	}
}

public class LaserPacket : IDatagram
{
	public const int LASER_FRAMES_PER_360 = 90;
	public const int LASER_FRAMES_PER_PACKET = 10;

	public const ulong MICROSECONDS_PER_MINUTE = 60 * 1000000;
	public const ulong LASER_SPEED_FIXED_POINT_PRECISION = 64;

	public ulong timestamp_us;
	public ushort laser_speed; //fixed point, 6 bits precision, divide by 64.0 to get floating point 
	public ushort laser_angle;

	/// laser_readings[LASER_FRAMES_PER_PACKET*4]
	public LaserReading[] laser_readings = new LaserReading[LASER_FRAMES_PER_PACKET*4];

	public LaserPacket()
	{
		for (int i = 0; i < laser_readings.Length; ++i)
			laser_readings[i] = new LaserReading();
	}

	public ulong GetEndTimestampUs()
	{
		/* 
		float laser_rpm = laser_speed / 64.0f;
		float us_per_degree = MICROSECONDS_PER_MINUTE / (360.0f * laser_rpm);
		return (ulong)(timestamp_us + us_per_degree * LASER_FRAMES_PER_PACKET * 4);
		*/
		return GetTimestampUs(LASER_FRAMES_PER_PACKET * 4 - 1);
	}

	public ulong GetTimestampUs()
	{
		return timestamp_us;
	}
	public ulong GetTimestampUs(int reading)
	{
		return timestamp_us + (ulong)reading * MICROSECONDS_PER_MINUTE * LASER_SPEED_FIXED_POINT_PRECISION / (360UL * laser_speed);
	}
		
	public void FromBinary(System.IO.BinaryReader reader)
	{
		timestamp_us = (ulong)IPAddress.NetworkToHostOrder(reader.ReadInt64());
		laser_speed = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
		laser_angle = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());


		for (int i = 0; i < laser_readings.Length; ++i)
			laser_readings[i].FromBinary(reader);	
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write(IPAddress.HostToNetworkOrder((long)timestamp_us));
		writer.Write(IPAddress.HostToNetworkOrder((short)laser_speed));
		writer.Write(IPAddress.HostToNetworkOrder((short)laser_angle));

		for (int i = 0; i < laser_readings.Length; ++i)
			laser_readings[i].ToBinary(writer);	
	}


	public int BinarySize()
	{
		return 12 + 16 * LASER_FRAMES_PER_PACKET;
	}

	public override string ToString()
	{
		return string.Format("[ts={0} ls={1} la={2}]", timestamp_us, laser_speed, laser_angle) + laser_readings[3].ToString();
	}
}
