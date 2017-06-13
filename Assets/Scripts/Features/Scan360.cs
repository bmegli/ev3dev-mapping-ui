/*
 * Copyright (C) 2017 Bartosz Meglicki <meglickib@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 * This program is distributed "as is" WITHOUT ANY WARRANTY of any
 * kind, whether express or implied; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanPoint
{
	public Vector3 Point { get; set;}
	public int Index { get; set;}
	public float Angle { get; set;}
	public int Cluster { get; set; }
}
	
public class AngleComparer : Comparer<ScanPoint>
{
	public override int Compare(ScanPoint p1, ScanPoint p2)
	{
		return p1.Angle.CompareTo(p2.Angle);
	}
}

public abstract class FloatMetric<TYPE>
{
	public abstract float Distance(ScanPoint p1, ScanPoint p2);
}
	
public class AngleCircularComparer : FloatMetric<ScanPoint>
{
	public override float Distance(ScanPoint p1, ScanPoint p2)
	{
		float dist = Mathf.Abs(p1.Angle - p2.Angle);
		return (dist <= Mathf.PI / 2.0f) ? dist : Mathf.PI - dist;
	}
}


public class Scan360
{
	public List<ScanPoint> readings = new List<ScanPoint>(360);

	public Scan360(Vector3[] readings, bool[] invalid_data)
	{
		for (int i = 0; i < readings.Length; ++i)
			if (!invalid_data[i])
				this.readings.Add(new ScanPoint{Point=readings[i], Index=i, Angle=0.0f});
	}
	public void EstimateLocalAngle(int index1=0, int index2=2)
	{	//note this function can be optimized by keeping partial TLS sums and updating as we go
		if (readings.Count < 3)
			return;
		for (int i = 0; i < readings.Count; ++i)
			readings[i].Angle=TotalLeastSquares.EstimateAngle(readings, i - 1, i + 1, index1, index2); 
	}		
}
