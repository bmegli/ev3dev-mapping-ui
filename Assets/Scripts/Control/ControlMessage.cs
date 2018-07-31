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

using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.IO;

namespace Ev3devMapping
{

public enum ControlCommands : sbyte {KEEPALIVE=0, ENABLE=1, DISABLE=2, DISABLE_ALL=3, ENABLED=-1, DISABLED=-2, FAILED=-3 };
public enum ControlAttributes : byte {UNIQUE_NAME=0, CALL=1, CRATION_DELAY_MS=2, RETURN_VALUE=3};

public class ControlMessage : IMessage
{
	public const byte CONTROL_PROTOCOL_VERSION=1;
	public const int CONTROL_MAX_PAYLOAD_LENGTH = 65535;

	public const sbyte MIN_KNOWN_COMMAND=(sbyte)ControlCommands.FAILED;
	public const sbyte MAX_KNOWN_COMMAND=(sbyte)ControlCommands.DISABLE_ALL;


	public readonly ControlAttributes[][] NEGATIVE_COMMANDS_ATTRIBUTES = {
		new ControlAttributes[] {},
		new ControlAttributes[] {ControlAttributes.UNIQUE_NAME},
		new ControlAttributes[] {ControlAttributes.UNIQUE_NAME},
		new ControlAttributes[] {ControlAttributes.UNIQUE_NAME, ControlAttributes.RETURN_VALUE}
	};

	public readonly ControlAttributes[][] POSITIVE_COMMANDS_ATTRIBUTES = {
		new ControlAttributes[] {},
		new ControlAttributes[] {ControlAttributes.UNIQUE_NAME, ControlAttributes.CALL, ControlAttributes.CRATION_DELAY_MS},
		new ControlAttributes[] {ControlAttributes.UNIQUE_NAME},
		new ControlAttributes[] {}
	};


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

	public ControlCommands GetCommand()
	{
		return header.command;
	}

	public VALUE Attribute<VALUE>(int index)
	{
		return (VALUE)attributes[index].Value();
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

	public ControlMessage FillWithKeepaliveMessage()
	{
		attributes.Clear ();
		NewMessage(ControlCommands.KEEPALIVE);
		return this;
	}

	public static ControlMessage KeepaliveMessage()
	{
		ControlMessage msg = new ControlMessage();
		msg.NewMessage(ControlCommands.KEEPALIVE);
		return msg;
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

			ControlAttribute attribute = ControlAttribute.FromBinary (reader);
			remaining -= attribute.GetTotalLength ();
			attributes.Add (attribute);
		}			
		Validate();
	}

	private void Validate()
	{
		if ((sbyte)header.command > MAX_KNOWN_COMMAND || (sbyte)header.command < MIN_KNOWN_COMMAND)
			throw new ArgumentException ("Command not supported: " + header.command);
	
		ControlAttributes[] expected;
		if (header.command <= 0)
			expected = NEGATIVE_COMMANDS_ATTRIBUTES[-(sbyte)header.command];
		else
			expected = POSITIVE_COMMANDS_ATTRIBUTES[(sbyte)header.command];
		
		if(attributes.Count < expected.Length)
			throw new ArgumentException ("Incorrect number of attributes in command " + header.command + " ," + attributes.Count + " expected: " + expected.Length);

		for (int i = 0; i < expected.Length; ++i)
		{
			if(attributes[i].GetAttribute() != expected[i])
				throw new ArgumentException ("Incorrect attribute in command " + header.command + " at position " + i + " is " + attributes[i].GetAttribute() + " expected: " + expected[i]);
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
	
public abstract class ControlAttribute
{
	public const int CONTROL_ATTRIBUTE_HEADER_BYTES = 2;
	public const int CONTROL_MAX_ATTRIBUTE_DATA_LENGTH = 255;

	private ControlAttributes attribute;
	protected byte length;

	public ControlAttributes GetAttribute()
	{
		return attribute;
	}

	public ushort GetTotalLength()
	{
		return (ushort)(CONTROL_ATTRIBUTE_HEADER_BYTES + length);
	}

	public abstract object Value();

	public ControlAttribute(ControlAttributes attr, byte len)
	{
		attribute = attr;
		length = len;
	}

	public static ControlAttribute FromBinary(BinaryReader reader)
	{
		ControlAttributes attribute = (ControlAttributes) reader.ReadByte ();
		byte length = reader.ReadByte ();

		switch (attribute)
		{
		case ControlAttributes.UNIQUE_NAME:
		case ControlAttributes.CALL:
			return ControlAttributeString.FromBinary (attribute, length, reader);
		case ControlAttributes.CRATION_DELAY_MS:
			return ControlAttributeU16.FromBinary (attribute, length, reader);
		case ControlAttributes.RETURN_VALUE:
			return ControlAttributeI32.FromBinary (attribute, length, reader);
		default:
			return ControlAttributeUnknown.FromBinary (attribute, length, reader);
		}			
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

	private ControlAttributeString(ControlAttributes attr, byte len, BinaryReader reader) : base(attr, len)
	{
		byte[] ascii_data=reader.ReadBytes(len);
		data=System.Text.Encoding.ASCII.GetString(ascii_data, 0 , len-1);
	}

	public ControlAttributeString(ControlAttributes attr, string text) : base(attr, (byte)(System.Text.Encoding.ASCII.GetByteCount(text)+1))
	{
		data = text;
		if ((System.Text.Encoding.ASCII.GetByteCount(text)+1) > CONTROL_MAX_ATTRIBUTE_DATA_LENGTH)
			throw new ArgumentException ("The string data is too long to encode in attribute");	
	}

	public static ControlAttributeString FromBinary(ControlAttributes attr, byte len, BinaryReader reader)
	{
		return new ControlAttributeString (attr, len, reader);
	}

	public override int ToBinary(System.IO.BinaryWriter writer)
	{
		byte[] ascii_data = System.Text.ASCIIEncoding.ASCII.GetBytes (data);						
		base.ToBinary (writer);
		writer.Write (ascii_data);
		writer.Write ((byte)0); //null terminate the string

		return CONTROL_ATTRIBUTE_HEADER_BYTES + length;
	}		

	public override object Value()
	{
		return data;
	}
}

public class ControlAttributeU16 : ControlAttribute
{
	private ushort data;

	private ControlAttributeU16(ControlAttributes attr, byte len, BinaryReader reader) : base(attr, 2)
	{
		if (len != 2)
			throw new ArgumentException ("Incorrect length for U16 attribute");

		data=(ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
	}

	public ControlAttributeU16(ControlAttributes attr, ushort value) : base(attr, 2)
	{
		data = value;
	}

	public static ControlAttributeU16 FromBinary(ControlAttributes attr, byte len, BinaryReader reader)
	{
		return new ControlAttributeU16 (attr, len, reader);
	}


	public override int ToBinary(System.IO.BinaryWriter writer)
	{
		base.ToBinary (writer);
		writer.Write (IPAddress.HostToNetworkOrder ((short)data));

		return CONTROL_ATTRIBUTE_HEADER_BYTES + length;
	}

	public override object Value()
	{
		return data;
	}

}

public class ControlAttributeI32 : ControlAttribute
{
	private int data;

	private ControlAttributeI32(ControlAttributes attr, byte len, BinaryReader reader) : base(attr, 4)
	{
		if (len != 4)
			throw new ArgumentException ("Incorrect length for I32 attribute");

		data = IPAddress.NetworkToHostOrder(reader.ReadInt32());
	}
	public ControlAttributeI32(ControlAttributes attr, int value) : base(attr, 4)
	{
		data = value;
	}

	public static ControlAttributeI32 FromBinary(ControlAttributes attr, byte len, BinaryReader reader)
	{
		return new ControlAttributeI32 (attr, len, reader);
	}


	public override int ToBinary(System.IO.BinaryWriter writer)
	{
		base.ToBinary (writer);
		writer.Write(IPAddress.HostToNetworkOrder(data));

		return CONTROL_ATTRIBUTE_HEADER_BYTES + length;
	}		

	public override object Value()
	{
		return data;
	}
}

public class ControlAttributeUnknown : ControlAttribute
{
	private byte[] data;

	private ControlAttributeUnknown(ControlAttributes attr, byte len, BinaryReader reader) : base(attr, len)
	{
		data = reader.ReadBytes (len);
	}

	public static ControlAttributeUnknown FromBinary(ControlAttributes attr, byte len, BinaryReader reader)
	{
		return new ControlAttributeUnknown (attr, len, reader);
	}
		
	public override int ToBinary(System.IO.BinaryWriter writer)
	{
		base.ToBinary (writer);
		writer.Write (data);

		return CONTROL_ATTRIBUTE_HEADER_BYTES + length;
	}

	public override object Value()
	{
		return data;
	}
}

} //namespace