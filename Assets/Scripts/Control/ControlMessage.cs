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

/*
*  For high level protocol overview see:
*  https://github.com/bmegli/ev3dev-mapping/issues/5
* 
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.IO;

public enum ControlCommands : sbyte {KEEPALIVE=0, ENABLE=1, DISABLE=2, DISABLE_ALL=3, ENABLED=-1, DISABLED=-2, FAILED=-3 };
public enum ControlAttributes : byte {UNIQUE_NAME=0, CALL=1, CRATION_DELAY_MS=2, RETURN_VALUE=3};
	
public class ControlMessage : IMessage
{
	public const byte CONTROL_PROTOCOL_VERSION=1;
	public const int CONTROL_MAX_PAYLOAD_LENGTH = 65535;

	private ControlHeader header = new ControlHeader();
	private List<ControlAttribute> attributes = new List<ControlAttribute> ();

	public int HeaderSize()
	{
		return ControlHeader.CONTROL_HEADER_BYTES;
	}

	public int PayloadSize(BinaryReader header_data)
	{
		header.FromBinary (header_data);
		return header.payload_length;

	}

	public override string ToString()
	{
		return "t=" + " v=" + header.protocol_version + " c=" + header.command + " p=" + header.payload_length; 
	}

	public ulong GetTimestampUs()
	{
		return header.timestamp_us;
	}
		
	public void NewMessage(ControlCommands command)
	{
		header.timestamp_us = Timestamp.TimestampUs ();
		header.protocol_version = CONTROL_PROTOCOL_VERSION;
		header.command = command;
		header.payload_length = 0;
	}

	public void PutAttribute(ControlAttribute attribute)
	{
		if (ControlHeader.CONTROL_HEADER_BYTES + header.payload_length + attribute.GetTotalLength() > CONTROL_MAX_PAYLOAD_LENGTH)
			throw new ArgumentException ("Putting attribute would exceed maximum message length");

		attributes.Add (attribute);
		header.payload_length += attribute.GetTotalLength ();
	}
	public void PutString(ControlAttributes attribute, string value)
	{
		PutAttribute (new ControlAttributeString (attribute, value));
	}
	public void PutU16(ControlAttributes attribute, ushort value)
	{
		PutAttribute (new ControlAttributeU16 (attribute, value));
	}
	public void PutI32(ControlAttributes attribute, int value)
	{
		PutAttribute (new ControlAttributeI32 (attribute, value));
	}
		
	public static ControlMessage EnableMessage(string unique_name, string call, ushort creation_delay_ms)
	{
		ControlMessage msg = new ControlMessage ();

		msg.NewMessage (ControlCommands.ENABLE);
		msg.PutString (ControlAttributes.UNIQUE_NAME, unique_name);
		msg.PutString (ControlAttributes.CALL, call);
		msg.PutU16 (ControlAttributes.CRATION_DELAY_MS, creation_delay_ms);

		return msg;
	}

	public static ControlMessage DisableMessage(string unique_name)
	{
		ControlMessage msg = new ControlMessage ();

		msg.NewMessage (ControlCommands.DISABLE);
		msg.PutString (ControlAttributes.UNIQUE_NAME, unique_name);

		return msg;
	}

	public static ControlMessage DisableAllMessage()
	{
		ControlMessage msg = new ControlMessage ();

		msg.NewMessage (ControlCommands.DISABLE_ALL);

		return msg;
	}


	public void FromBinary(BinaryReader reader)
	{
		attributes.Clear ();
		header.FromBinary (reader);
		if (header.payload_length == 0)
			return;

		int remaining = header.payload_length;

		while (remaining > 0)
		{
			if (remaining < ControlAttribute.CONTROL_ATTRIBUTE_HEADER_BYTES)
				throw new ArgumentException ("Incorrect payload - not enough data for attribute header");
			#warning work in progress
		}



	}
	public int ToBinary(BinaryWriter writer)
	{
		int written = 0;
		written +=header.ToBinary(writer); 
		foreach (ControlAttribute a in attributes)
			written += a.ToBinary (writer);
		return written;
	}
		
	public class ControlHeader
	{
		public const int CONTROL_HEADER_BYTES = 12;

		public ulong timestamp_us;
		public byte protocol_version;
		public ControlCommands command;
		public ushort payload_length;

		public void FromBinary(BinaryReader reader)
		{
			timestamp_us = (ulong)IPAddress.NetworkToHostOrder(reader.ReadInt64());
			protocol_version = reader.ReadByte ();
			command = (ControlCommands) reader.ReadSByte ();
			payload_length = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
		}
		public int ToBinary(BinaryWriter writer)
		{
			writer.Write(IPAddress.HostToNetworkOrder((long)timestamp_us));
			writer.Write(protocol_version);
			writer.Write((sbyte)command);
			writer.Write (IPAddress.HostToNetworkOrder ((short)payload_length));
			return CONTROL_HEADER_BYTES;
		}
	}
}

public abstract class ControlAttribute
{
	public const int CONTROL_ATTRIBUTE_HEADER_BYTES = 2;
	public const int CONTROL_MAX_ATTRIBUTE_DATA_LENGTH = 255;

	private ControlAttributes attribute;
	protected byte length;

	public ushort GetTotalLength()
	{
		return (ushort)(CONTROL_ATTRIBUTE_HEADER_BYTES + length);
	}

	public ControlAttribute(ControlAttributes attr, byte len)
	{
		attribute = attr;
		length = len;
	}

	public virtual void FromBinary(System.IO.BinaryReader reader)
	{
		attribute = (ControlAttributes) reader.ReadByte ();
		length = reader.ReadByte ();
	}
	public virtual int ToBinary(System.IO.BinaryWriter writer)
	{
		writer.Write ((byte)attribute);
		writer.Write (length);
		return CONTROL_ATTRIBUTE_HEADER_BYTES;
	}
}

public class ControlAttributeString : ControlAttribute
{
	private string data;

	public ControlAttributeString(ControlAttributes attr, string text) : base(attr, (byte)(System.Text.Encoding.ASCII.GetByteCount(text)+1))
	{
		data = text;
		if ((System.Text.Encoding.ASCII.GetByteCount(text)+1) > CONTROL_MAX_ATTRIBUTE_DATA_LENGTH)
			throw new ArgumentException ("The string data is too long to encode in attribute");	
	}

	public override void FromBinary(System.IO.BinaryReader reader)
	{
		base.FromBinary(reader);
		byte[] ascii_data=reader.ReadBytes (length);
		data=System.Text.Encoding.ASCII.GetString(ascii_data, 0 , length-1);

	}
	public override int ToBinary(System.IO.BinaryWriter writer)
	{
		byte[] ascii_data = System.Text.ASCIIEncoding.ASCII.GetBytes (data);						
		base.ToBinary (writer);
		writer.Write (ascii_data);
		writer.Write ((byte)0); //null terminate the string

		return CONTROL_ATTRIBUTE_HEADER_BYTES + length;
	}		
}

public class ControlAttributeU16 : ControlAttribute
{
	private ushort data;

	public ControlAttributeU16(ControlAttributes attr, ushort value) : base(attr, 2)
	{
		data = value;
	}

	public override void FromBinary(System.IO.BinaryReader reader)
	{
		base.FromBinary(reader);
		data=(ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

	}
	public override int ToBinary(System.IO.BinaryWriter writer)
	{
		base.ToBinary (writer);
		writer.Write (IPAddress.HostToNetworkOrder ((short)data));

		return CONTROL_ATTRIBUTE_HEADER_BYTES + length;
	}		
}

public class ControlAttributeI32 : ControlAttribute
{
	private int data;

	public ControlAttributeI32(ControlAttributes attr, int value) : base(attr, 4)
	{
		data = value;
	}

	public override void FromBinary(System.IO.BinaryReader reader)
	{
		base.FromBinary(reader);
		data = IPAddress.NetworkToHostOrder(reader.ReadInt32());

	}
	public override int ToBinary(System.IO.BinaryWriter writer)
	{
		base.ToBinary (writer);
		writer.Write(IPAddress.HostToNetworkOrder(data));

		return CONTROL_ATTRIBUTE_HEADER_BYTES + length;
	}		
}

