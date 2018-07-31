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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Ev3devMapping
{

/// <summary>
/// Density Based Scan implementation
/// </summary>
public class DBSCAN
{
	public DBSCAN(){}
	public DBSCAN(int capacity)
	{
		Capacity = capacity;
	}

	public int Capacity {
		get { return capacity;}
		set
		{
			if (capacity == value)
				return;
			U=new int[value];
			L=new int[value];
			C=new int[value];
		} 
	}
		
	private int[] U, L, C;

	private int capacity=0;

	private const int NOT_VISITED=-1;
	private const int NOISE=0; 
			
	public List<DBSCANCluster> Cluster(IList<ScanPoint> scan, float eps, int minPoints, Comparer<ScanPoint> comparer, FloatMetric<ScanPoint> circularMetric)
	{
		List<DBSCANCluster> clusters = new List<DBSCANCluster>();
		int cluster = 0, N=scan.Count;

		if (N > Capacity)
			Capacity = N;

		ArrayList.Adapter((IList)scan).Sort(comparer);
		CalculateNeighbourhood(scan, eps, circularMetric); 

		for (int i = 0; i < N; ++i)
			C[i] = NOT_VISITED;

		for (int i = 0; i < N; ++i)
		{
			if (C[i] != NOT_VISITED)
				continue;

			if (NeighbourCount(i) < minPoints)
				C[i] = NOISE;
			else
				clusters.Add(ExpandCluster(scan, ++cluster, i, minPoints));		
		}
		//	DumpScan(scan, C);

		return clusters;
	}
		
	// Note:
	// -u can be up to N+i-1
	// -l can be down to -N+1+u
	private void CalculateNeighbourhood(IList<ScanPoint> scan, float eps, FloatMetric<ScanPoint> circularMetric)
	{	//both the scan and feature space are circular which makes implemantation tricky!
		int N=scan.Count, l = N-1, u = 0;

		for (int i = 0; i < N; ++i)
		{
			for (; u < N + i && circularMetric.Distance(scan[Mod(u, N)], scan[i]) < eps; ++u)
				;
			U[i] = u-1;
		}
		for (int i = N-1; i >= 0; --i)
		{
			for(; U[i]-l+1<=N && circularMetric.Distance(scan[Mod(l, N)], scan[i]) < eps; --l)
				;
			L[i] = l+1;
		}			
	}
		
	private DBSCANCluster ExpandCluster(IList<ScanPoint> scan, int cluster, int p, int minPoints)
	{
		C[p] = cluster;
		int u = U[p], l=L[p];
		int N = scan.Count;
		int i = p + 1;

		for (; i <= u && i < N + p; ++i)
		{
			int ind = Mod(i, N);
			if (C[ind] == NOT_VISITED)
			{
				C[ind] = cluster;
				if (NeighbourCount(ind) >= minPoints)
					u = (U[ind] > p) ? U[ind] : U[ind] + N;
			}
			else if (C[ind] == NOISE)
				C[ind] = cluster;
		}
		u=i-1; // u is from p up to N + p -1
		i=p-1;

		for (; i >= l && u-i+1 <= N ; --i)// i>=-N+1 +u
		{
			int ind = Mod(i, N);
			if (C[ind] == NOT_VISITED)
			{
				C[ind] = cluster;
				if (NeighbourCount(ind) >= minPoints)
					l = (L[ind] < p) ? L[ind] : L[ind]-N;
			}
			else if (C[ind] == NOISE)
				C[ind] = cluster;		
		}
		l = i + 1; //l is from p down to -N+1+u

		return new DBSCANCluster(scan, l, u, cluster);
	}

	private int NeighbourCount(int i)
	{
		return U[i]-L[i]+1;
	}

	private int Mod(int i, int m)
	{
		int r = i % m;
		return (r < 0) ? r+m : r;
	}

	/*
	private static System.IO.StreamWriter snapshotWriter=new System.IO.StreamWriter(Config.SnapshotPath("SnapshotTour", "Robot", "temp"));

	private static void DumpScan(List<ScanPoint> scan, int[] C)
	{
		for (int i = 0; i < 360; ++i)
		{						
			for (int j = 0; j < 3; ++j)
			{
				int coordinate = 0;

				if (i < scan.Count)
				{
					if (j != 1)
						coordinate = (int)(scan[i].Point[j] * 1000);
					else
						coordinate = C[i];
				}
				snapshotWriter.Write(coordinate);

				if(!(i==359 && j==2))
					snapshotWriter.Write(";");
			}
		}

		snapshotWriter.WriteLine();
	}
	*/
}

} //namespace