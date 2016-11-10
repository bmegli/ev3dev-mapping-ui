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

public class WifiPacket : IDatagram
{
	public const int BSSID_LENGTH=6;
	public const int SSID_LENGTH_WITH0_MAX=33;

	public ulong timestamp_us;
	public byte[] bssid;
	public string ssid;
	public sbyte signal_dbm;
	public uint rx_packets;
	public uint tx_packets;

	public string bssid_string="00:00:00:00:00:00";

	public void FromBinary(System.IO.BinaryReader reader)
	{
		timestamp_us = (ulong)IPAddress.NetworkToHostOrder(reader.ReadInt64());
		bssid = reader.ReadBytes(BSSID_LENGTH);
		byte[] ssid_bytes = reader.ReadBytes(SSID_LENGTH_WITH0_MAX);
		signal_dbm = reader.ReadSByte();
		rx_packets = (uint)IPAddress.NetworkToHostOrder(reader.ReadInt32());
		tx_packets = (uint)IPAddress.NetworkToHostOrder(reader.ReadInt32());
				
		bssid_string = BSSIDToString(bssid);
		ssid = FromASCII(ssid_bytes);
	}
	public void ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write(IPAddress.HostToNetworkOrder((long)timestamp_us));
		writer.Write(bssid);
		byte[] ssid_ascii = ASCII(ssid);
		int to_write = ssid.Length;
		if (to_write >= SSID_LENGTH_WITH0_MAX)
			to_write = SSID_LENGTH_WITH0_MAX - 1;


		writer.Write(ssid_ascii, 0, to_write);
		for (int i = 0; i < SSID_LENGTH_WITH0_MAX - to_write; ++i)
			writer.Write((byte)0);
		
		writer.Write(signal_dbm);
		writer.Write(IPAddress.HostToNetworkOrder((int)rx_packets));
		writer.Write(IPAddress.HostToNetworkOrder((int)tx_packets));
	}

	public int BinarySize()
	{
		return 56; // 8 + 6 + 33 + 1 + 4 + 4;
	}

	public ulong GetTimestampUs()
	{
		return timestamp_us;
	}

	private static string BSSIDToString(byte[] bssid)
	{
		return string.Format("{0:X}:{1:X}:{2:X}:{3:X}:{4:X}:{5:X}", bssid[0], bssid[1],bssid[2],bssid[3],bssid[4],bssid[5]);
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

	public WifiPacket DeepCopy()
	{
		WifiPacket other = (WifiPacket) this.MemberwiseClone();
		other.bssid_string = string.Copy(bssid_string);
		other.ssid = string.Copy(ssid);
		other.bssid = new byte[BSSID_LENGTH];
		bssid.CopyTo(other.bssid, 0);
		return other;
	}

}
