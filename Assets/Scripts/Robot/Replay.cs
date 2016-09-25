using UnityEngine;
using System.Collections;

public class Replay : MonoBehaviour
{
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
		return other;
	}
}
