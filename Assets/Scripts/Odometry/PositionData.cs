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

using UnityEngine;
using System.Collections;

namespace Ev3devMapping
{

public struct PositionData : System.IComparable<PositionData>
{
	public ulong timestamp;
	public Vector3 position;
	public Vector3 rotation;
	public Quaternion quaternion;

	public float heading {
		get {return rotation.y; }
		set {rotation = new Vector3(rotation.x, value, rotation.z); }
	}

	public int CompareTo(PositionData other)
	{
		return timestamp.CompareTo(other.timestamp);
	}

	public override string ToString()
	{
		return string.Format("ts={0} ps={1} hd={2}", timestamp, position, rotation.y);
	}
}

} //namespace