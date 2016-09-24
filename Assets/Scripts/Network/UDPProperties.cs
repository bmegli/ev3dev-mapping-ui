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

public enum UDPReplayMode {None, Record, Replay};
public enum UDPReplayDirection {Inbound, Outbound};

public interface IReplayableUDPServer 
{
	ulong GetFirstPacketTimestampUs();
}

// we need separate for client because those are two completely separate timestamp systems (PC vs EV3)
// also servers need to synchronize with servers and clients with clients
public interface IReplayableUDPClient
{
	ulong GetFirstPacketTimestampUs();
}
	
[Serializable]
public class UDPProperties
{
	public int port=8000;
	//public string dumpFilename="dump.bin";
}
