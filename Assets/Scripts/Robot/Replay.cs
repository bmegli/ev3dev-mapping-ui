using UnityEngine;
using System.Collections;

public enum ReplayMode {None, Record, Replay};
public enum ReplayDirection {Inbound, Outbound};

public class Replay : MonoBehaviour
{
	public ReplayMode mode = ReplayMode.None;
	public ReplayDirection direction = ReplayDirection.Inbound;

	public bool RecordInbound()
	{
		return mode == ReplayMode.Record && direction == ReplayDirection.Inbound;
	}
	public bool RecordOutbound()
	{
		return mode == ReplayMode.Record && direction == ReplayDirection.Outbound;
	}
	public bool ReplayInbound()
	{
		return mode == ReplayMode.Replay && direction == ReplayDirection.Inbound;
	}
	public bool ReplayOutbound()
	{
		return mode == ReplayMode.Replay && direction == ReplayDirection.Outbound;
	}
		
	public Replay DeepCopy()
	{
		Replay other = (Replay) this.MemberwiseClone();
		return other;
	}
}
