using UnityEngine;
using System.Collections;

namespace Ev3devMapping
{

public enum ReplayMode {None, RecordIn, ReplayIn, RecordOut, ReplayOut, RecordInRecordOut, RecordInReplayOut};

public class Replay : MonoBehaviour
{
	public ReplayMode mode = ReplayMode.None;

	public bool RecordInbound()
	{
		return mode == ReplayMode.RecordIn || mode == ReplayMode.RecordInRecordOut || mode == ReplayMode.RecordInReplayOut;
	}
	public bool RecordOutbound()
	{
		return mode == ReplayMode.RecordOut | mode == ReplayMode.RecordInRecordOut; 
	}
	public bool ReplayInbound()
	{
		return mode == ReplayMode.ReplayIn;
	}
	public bool ReplayOutbound()
	{
		return mode == ReplayMode.ReplayOut || mode == ReplayMode.RecordInReplayOut;
	}
	public bool ReplayAny()
	{
		return mode == ReplayMode.ReplayIn || mode == ReplayMode.ReplayOut || mode == ReplayMode.RecordInReplayOut;
	}

	public Replay DeepCopy()
	{
		Replay other = (Replay) this.MemberwiseClone();
		return other;
	}
}

} //namespace
