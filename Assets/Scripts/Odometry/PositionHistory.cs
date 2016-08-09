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
using CircularBuffer;
using System;

[Serializable]
public class PositionHistoryProperties
{
	public int positionsKept=512;
}

public sealed class PositionHistory : MonoBehaviour
{
	public PositionHistoryProperties properties;

	private object syncRoot = new object();

	//this will keep positions from around last second, odometry is at 10 ms
	private CircularBuffer<PositionData> history;

	public PositionHistory()
	{
	}

	public PositionHistory(int historySize)
	{
		history=new CircularBuffer<PositionData>(historySize, true);
	}

	public void Awake()
	{
		history = new CircularBuffer<PositionData>(properties.positionsKept, true);
	}


	public void PutThreadSafe(PositionData pos)
	{
		lock (syncRoot)
		{
			if (history.Size == 0)
			{
				history.Put(pos);
				return;
			}
			// if we get some UDP packets not in chronological order, ignore them
			if (history.PeekNewest().timestamp >= pos.timestamp)
				return;

			history.Put(pos);
		}
	}

	/// <summary>
	/// Gets the snapshot of position history in timestamp range. O( log(n) )
	/// </summary>
	/// <returns>The snapshot or null if upper bound readings haven't arrived yet or lower bound readings are not in history</returns>
	/// <param name="timestamp_from">Timestamp from.</param>
	/// <param name="timestamp_to">Timestamp to.</param>
	public PositionSnapshot GetPositionSnapshotThreadSafe(ulong timestamp_from, ulong timestamp_to, out bool no_position_data_yet, out bool data_not_in_history)
	{
		no_position_data_yet = data_not_in_history = false;

		lock (syncRoot)
		{			
			if (history.Size < 2 || history.PeekNewest().timestamp < timestamp_to)
				no_position_data_yet = true;
			if (history.Size > 0 && history.PeekOldest().timestamp > timestamp_from)
				data_not_in_history = true;

			if (no_position_data_yet || data_not_in_history)
				return null; //or possibly later return just the fragment that is in

			int index_from = history.BinarySearch(new PositionData{ timestamp = timestamp_from });
		
			if (index_from < 0)
				index_from = ~index_from - 1;
			
			int index_to = history.BinarySearch(new PositionData{ timestamp = timestamp_to });

			if (index_to < 0)
				index_to = ~index_to;

			//possibly use some pool of snapshots that return on destruction
			return new PositionSnapshot(history.CloneSubset(index_from, index_to));
		}
	}

	public class PositionSnapshot
	{
		private PositionData[] data;

		public PositionSnapshot(PositionData[] dat)
		{
			data=dat;
		}

		public PositionData PositionAt(ulong timestamp)
		{
			try
			{

				if (timestamp < data[0].timestamp || timestamp > data[data.Length - 1].timestamp)
					throw new System.ArgumentOutOfRangeException("timestamp not in snapshot");

				PositionData pos = new PositionData{ timestamp = timestamp };

				int index=System.Array.BinarySearch<PositionData>(data, pos);

				if (index >= 0)
					return data[index];

				int after = ~index;
				int before = after - 1;

				//now interpolate position

				float at=(float)(timestamp-data[before].timestamp)/(float)(data[after].timestamp-data[before].timestamp);
				pos.position=data[before].position + at * (data[after].position - data[before].position);

				//now interpolate heading
				float angle_difference = data[after].heading - data[before].heading;

				//if we are wrapping around -180/180 it's the small angle, not the big one
				if (Mathf.Abs(angle_difference) > 180.0f)
					angle_difference = angle_difference - Mathf.Sign(angle_difference) * 360.0f;

				pos.heading = data[before].heading + at * angle_difference;

				return pos;
			}
			catch(System.Exception e) {

				return new PositionData{exc=true,heading=0,position=new Vector3(data[0].timestamp, timestamp, data[data.Length-1].timestamp), timestamp=timestamp, dummy_string=e.ToString()}; //DUMMY!
			}
		}
	}
}