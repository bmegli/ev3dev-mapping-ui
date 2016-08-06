using UnityEngine;
using System.Collections;

public struct PositionData : System.IComparable<PositionData>
{
	public ulong timestamp;
	public Vector3 position;
	public float heading;
	public string dummy_string;
	public bool exc;

	public int CompareTo(PositionData other)
	{
		return timestamp.CompareTo(other.timestamp);
	}

	public override string ToString()
	{
		if (dummy_string == null)
			dummy_string = "";
		return string.Format("ts={0} ps={1} hd={2} {3}", timestamp, position, heading, dummy_string);
	}
}
