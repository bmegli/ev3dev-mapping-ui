using System.Net;

public class DrivePacket : IDatagram
{
	public enum Commands {KEEPALIVE=0, SET_SPEED=1, TO_POSITION_WITH_SPEED};

	public ulong timestamp_us;
	public short command;
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
		command = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		param1 = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		param2 = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		param3 = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		param4 = IPAddress.NetworkToHostOrder(reader.ReadInt16());
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write(IPAddress.HostToNetworkOrder((long)timestamp_us));
		writer.Write(IPAddress.HostToNetworkOrder(command));
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
