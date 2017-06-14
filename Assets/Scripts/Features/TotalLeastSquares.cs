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

public struct TotalLeastSquaresFit
{
	public float angle;
	public float distance;
}

public static class TotalLeastSquares
{
	public static TotalLeastSquaresFit Fit(IList<ScanPoint> scan, int index1, int index2)
	{
		return Fit(scan, 0, scan.Count - 1, index1, index2, false); 
	}

	/// <summary>
	/// Estimates total least square angle. 
	/// </summary>
	/// <returns>The estimated angle in range <0, PI> and distance from (0,0) to nearest point if angleOnly==false</returns>
	/// <param name="scan">Scan points.</param>
	/// <param name="from">Estatimate starting from index (circular, can be negative).</param>
	/// <param name="to">Estimate up to index (inclusive, circular, can be > points.Count).</param>
	/// <param name="index1">Point Index 1: 0 for x, 1 for y, 2 for z.</param>
	/// <param name="index2">Point Index 2: 0 for x, 1 for y, 2 for z.</param>
	public static TotalLeastSquaresFit Fit(IList<ScanPoint> scan, int from, int to, int index1, int index2, bool angleOnly=false)
	{   
		int X = index1;
		int Y = index2;
		int PTS = to - from + 1;

		float xMean=0, yMean=0, sxy=0, syyLsxx=0, xMeanLessX, yMeanLessY, angle, distance;

		for (int i = from, ind; i <= to; ++i)
		{
			ind = Mod(i, scan.Count);
			xMean += scan[ind].Point[X];
			yMean += scan[ind].Point[Y];
		}

		xMean /= PTS;
		yMean /= PTS;

		for (int i = from, ind; i <= to; ++i)
		{
			ind = Mod(i, scan.Count);
			xMeanLessX = xMean - scan[ind].Point[X];
			yMeanLessY = yMean - scan[ind].Point[Y];
			sxy += xMeanLessX * yMeanLessY;
			syyLsxx += yMeanLessY * yMeanLessY - xMeanLessX * xMeanLessX;
		}

		angle = Mathf.Atan2(-2.0f * sxy, syyLsxx)/2.0f;

		if(angle<0)	
			angle += Mathf.PI;

		if (angleOnly)
			return new TotalLeastSquaresFit{ angle = angle, distance = 0 };

		distance = xMean * Mathf.Cos(angle) + yMean * Mathf.Sin(angle);

		return new TotalLeastSquaresFit{ angle = angle, distance = distance };
	}



	private static int Mod(int i, int m)
	{
		int r = i % m;
		return (r < 0) ? r+m : r;
	}
}
