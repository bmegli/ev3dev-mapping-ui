using UnityEngine;
using System.Collections;

public class Replay : MonoBehaviour
{
	public string directory="Replay1";
	public UDPReplayMode mode = UDPReplayMode.None;
	public UDPReplayDirection direction = UDPReplayDirection.Inbound;

	public bool RecordInbound()
	{
		return mode == UDPReplayMode.Record && direction == UDPReplayDirection.Inbound;
	}
	public bool RecordOutbound()
	{
		return mode == UDPReplayMode.Record && direction == UDPReplayDirection.Outbound;
	}
	public bool ReplayInbound()
	{
		return mode == UDPReplayMode.Replay && direction == UDPReplayDirection.Inbound;
	}
	public bool ReplayOutbound()
	{
		return mode == UDPReplayMode.Replay && direction == UDPReplayDirection.Outbound;
	}



	public Replay DeepCopy()
	{
		Replay other = (Replay) this.MemberwiseClone();
		other.directory = string.Copy(directory);
		return other;
	}
}
