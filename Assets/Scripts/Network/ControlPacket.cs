using System.Net;

public class ControlPacket : IDatagram
{
	public const int MODULE_NAME_WITH0_MAX=20;
	public const int MODULE_CALL_WITH0_MAX=128;

	public enum Commands {Enable=0, Disable=1, DisableAll=3, Enabled=4, Disabled=5, Failed=6 };

	public ulong timestamp_us;
	public short command;
	public short creation_delay_ms;
	public string unique_name="";
	public string call="";

	public void FromBinary(System.IO.BinaryReader reader)
	{
		timestamp_us = (ulong)IPAddress.NetworkToHostOrder(reader.ReadInt64());
		command = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		creation_delay_ms = IPAddress.NetworkToHostOrder(reader.ReadInt16());
		byte[] name_ascii=reader.ReadBytes(MODULE_NAME_WITH0_MAX);
		byte[] call_ascii=reader.ReadBytes(MODULE_CALL_WITH0_MAX);
		unique_name = FromASCII(name_ascii);
		call = FromASCII(call_ascii);
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write(IPAddress.HostToNetworkOrder((long)timestamp_us));
		writer.Write(IPAddress.HostToNetworkOrder(command));
		writer.Write(IPAddress.HostToNetworkOrder(creation_delay_ms));
	
		byte[] unique_name_ascii = ASCII(unique_name);
		int to_write = unique_name.Length;
		if (to_write >= MODULE_NAME_WITH0_MAX)
			to_write = MODULE_NAME_WITH0_MAX - 1;

		writer.Write(unique_name_ascii, 0, to_write);
		for (int i = 0; i < MODULE_NAME_WITH0_MAX - to_write; ++i)
			writer.Write((byte)0);

		byte[] call_ascii = ASCII(call);
		to_write = call.Length;
		if (to_write >= MODULE_CALL_WITH0_MAX)
			to_write = MODULE_CALL_WITH0_MAX - 1;

		writer.Write(call_ascii, 0, to_write);
		for (int i = 0; i < MODULE_CALL_WITH0_MAX - to_write; ++i)
			writer.Write((byte)0);
	}

	public int BinarySize()
	{
		//const int CONTROL_PACKET_BYTES = sizeof(uint64_t) +  2 * sizeof(int16_t) +  MODULE_NAME_WITH0_MAX + MODULE_CALL_WITH0_MAX;   //8 + 4 + 20+128+=160
		return 160;
	}
		
	public ulong GetTimestampUs()
	{
		return timestamp_us;
	}

	private static byte[] ASCII(string s)
	{
		return System.Text.Encoding.ASCII.GetBytes(s);
	}
	private static string FromASCII(byte[] ascii)
	{
		int index_of_zero;
		for(index_of_zero=0;index_of_zero<ascii.Length;++index_of_zero)
			if(ascii[index_of_zero]==0)
				break;

		return System.Text.Encoding.ASCII.GetString(ascii, 0, index_of_zero);
	}

}
