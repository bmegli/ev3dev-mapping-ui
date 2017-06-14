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
using System.Threading;
using UnityEngine;

public enum FeaturesSegmentationLevel {Angle, AngleDistance};

[Serializable]
public class FeaturesPlotProperties
{
	public FeaturesSegmentationLevel level = FeaturesSegmentationLevel.AngleDistance;
	public PointCloud pointCloud;
	public Color[] colors={Color.gray, Color.green, Color.blue, Color.red, Color.magenta, Color.yellow, Color.cyan, Color.white};
}
	
[Serializable]
public class FeaturesSegmentationProperties
{
	public float angularEpsDeg = 2.0f;
	public int angularPoints = 10;
	public float distanceEpsCm = 1.0f;
	public int distancePoints =10; 
}


public class FeaturesThreadData
{
	public Vector3[] points=new Vector3[360];
	public Color[] colors=new Color[360];
	public bool consumed = true;

	public void FromScan360(Scan360 scan, FeaturesSegmentationLevel level, Color[] featureColors)
	{
		for (int i = 0; i < scan.readings.Count; ++i)
		{
			points[i] = scan.readings[i].Point;
			if (level == FeaturesSegmentationLevel.Angle)
				colors[i] = GetColor(scan.readings[i].ParallelCluster, featureColors);
			else
				colors[i] =  GetColor(scan.readings[i].DistanceCluster, featureColors);
		}
		for (int i = scan.readings.Count; i < 360; ++i)
		{
			points[i] = scan.readings[0].Point;
			colors[i] = Color.clear;
		}
	}

	private Color GetColor(int i, Color[] featureColors)
	{
		if (i == 0)
			return featureColors[0];
		return featureColors[1 + (i-1) % (featureColors.Length - 1)];
	}
}
	
// This component may be added to GameObject with Laser component
public class Features : MonoBehaviour
{	
	public FeaturesSegmentationProperties segmentation;
	public FeaturesPlotProperties plot;

	private PointCloud featuresPointCloud;

	private bool Run { get; set; }

	private Thread scanThread;
	private AutoResetEvent scanWaitEvent = new AutoResetEvent(false);

	private Queue<Scan360> waitingScans=new Queue<Scan360>();
	private object waitingScansLock = new object();

	private FeaturesThreadData uiData=new FeaturesThreadData();
	private FeaturesThreadData sharedData=new FeaturesThreadData();

	void Awake()
	{
		featuresPointCloud = SafeInstantiate<PointCloud>(plot.pointCloud);
	}
	void Start ()
	{
		print(name + " - starting extraction thread");
		StartScanThread();
	}	
	void Update ()
	{
		lock (sharedData)
		{
			if (sharedData.consumed)
				return;
			Array.Copy(sharedData.points, uiData.points, sharedData.points.Length);
			Array.Copy(sharedData.colors, uiData.colors, sharedData.colors.Length);
			sharedData.consumed = true;
		}

		featuresPointCloud.SetVertices(uiData.points, uiData.colors);
	}
	void OnDestroy()
	{
		print(name + " - stopping extraction thread");
		StopScanThread();
	}

	void StartScanThread()
	{
		if (scanThread != null)
		{
			Debug.LogError(name + " - scan thread is already running");
			throw new InvalidOperationException(name + " - scan thread is already running");
		}
		scanThread = new Thread(new ThreadStart(ScanThreadMain));
		Run = true;
		scanThread.Start();

	}

	void StopScanThread()
	{
		Run = false;
		scanWaitEvent.Set();
	}

	#region Thread Safe Functions

	public void PutScanThreadSafe(Vector3[] readings, bool[] invalid_data)
	{
		//to consider - circular buffer of scans and swap buffers instead of copy
		//copy is ok for now

		Scan360 scan = new Scan360(readings, invalid_data);

		lock(waitingScansLock)
		{
			waitingScans.Enqueue(scan);
		}

		scanWaitEvent.Set();
	}
		
	#endregion

	#region Feature Extraction Thread

	private void ScanThreadMain()
	{
		Queue<Scan360> scans = new Queue<Scan360>();
		Queue<Scan360> temp = new Queue<Scan360>();
		DBSCAN dbscan = new DBSCAN(360);
		FeaturesThreadData resultData = new FeaturesThreadData();

		while (scanWaitEvent.WaitOne())
		{
			if (!Run)
				break;

			lock(waitingScansLock)
			{
				temp = waitingScans;
				waitingScans = scans;
				scans = temp;
			}

			if (scans.Count > 1)
				print("Features - more then one waiting scan, does this ever happen?");

			while (scans.Count > 0)
			{
				if(ProcessScan(dbscan, scans.Dequeue(), resultData))
					PushResultsToUIThreadSafe(resultData);
			}
		}

		print("Features - scan thread finished");
	}

	private bool ProcessScan(DBSCAN dbscan, Scan360 scan, FeaturesThreadData result)
	{
		if (scan.readings.Count < 3)
			return false;
		
		for (int i = 0; i < scan.readings.Count; ++i)
			scan.readings[i].ParallelCluster = scan.readings[i].DistanceCluster = 0;

		List<DBSCANCluster> parallelClusters = AngularSegmentation(dbscan, scan, segmentation.angularEpsDeg * Constants.DEG2RAD, segmentation.angularPoints);
		List<DBSCANCluster> distanceClusters = new List<DBSCANCluster>();

		int lastClusterId = 0;

		foreach (DBSCANCluster c in parallelClusters)
			distanceClusters.AddRange(DistanceSegmentation(dbscan, c, ref lastClusterId, segmentation.distanceEpsCm/100.0f, segmentation.distancePoints));

		result.FromScan360(scan, plot.level, plot.colors);

		return true;
	}

	private List<DBSCANCluster> AngularSegmentation(DBSCAN dbscan, Scan360 scan, float eps, int minPoints)
	{
		scan.EstimateLocalAngle();

		List<DBSCANCluster> clusters=dbscan.Cluster(scan.readings, eps, minPoints, new AngleComparer(), new AngleCircularMetric());

		foreach (DBSCANCluster c in clusters)
			foreach (ScanPoint dp in c)
				dp.ParallelCluster = c.Id;

		return clusters;
	}
	private List<DBSCANCluster> DistanceSegmentation(DBSCAN dbscan, DBSCANCluster c, ref int clusterOffset, float eps, int minPoints)
	{
		float angle = ScanPoint.MeanCircularAngle0Pi(c);
		float cos = Mathf.Cos(angle);
		float sin = Mathf.Sin(angle);

		foreach (ScanPoint p in c)
			p.Distance = p.Point.x * cos + p.Point.z * sin;

		List<DBSCANCluster> clusters=dbscan.Cluster(c, eps, minPoints, new DistanceComparer(), new DistanceMetric());

		if (clusters.Count == 0)
			return new List<DBSCANCluster>();

		foreach (DBSCANCluster dc in clusters)
			foreach (ScanPoint p in dc)
				p.DistanceCluster = dc.Id + clusterOffset;
		
		clusterOffset += clusters[clusters.Count - 1].Id;
		return clusters;
	}

	private void PushResultsToUIThreadSafe(FeaturesThreadData result)
	{
		// consider - change to double buffered
		lock (sharedData)
		{
			Array.Copy(result.points, sharedData.points, result.points.Length);
			Array.Copy(result.colors, sharedData.colors, result.colors.Length);
			sharedData.consumed = false;
		}
	}
		

	#endregion

	#region To be Refactored 

	//some utility class or common base class
	private T SafeInstantiate<T>(T original) where T : MonoBehaviour
	{
		if (original == null)
		{
			Debug.LogError ("Expected to find prefab of type " + typeof(T) + " but it was not set");
			return default(T);
		}
		return Instantiate<T>(original);
	}
		
	#endregion
}
