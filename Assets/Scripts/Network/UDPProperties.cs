using System;

public enum UDPReplayMode {None, Record, Replay};

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
	public string host="";
	public int port=8000;
	public UDPReplayMode replayMode=UDPReplayMode.None;
	public string dumpFilename="dump.bin";
}
