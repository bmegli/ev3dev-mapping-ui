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

using System;
using System.Net.Sockets;

public interface IMessage
{
	void FromBinary(System.IO.BinaryReader reader);
	int ToBinary(System.IO.BinaryWriter writer);

	int PayloadSize (System.IO.BinaryReader header);

	int HeaderSize ();
	ulong GetTimestampUs();

}


