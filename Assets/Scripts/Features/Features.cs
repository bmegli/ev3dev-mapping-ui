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

public class Scan360
{
	List<Vector3> readings = new List<Vector3>(360);
	List<int> index = new List<int>(360);
	List<float> angle = new List<float>(360);

	public Scan360(Vector3[] readings, bool[] invalid_data)
	{
		for (int i = 0; i < readings.Length; ++i)
			if (!invalid_data[i])
			{
				this.readings.Add(readings[i]);
				this.index.Add(i);
			}
	}
	public void EstimateLocalAngle(int index1=0, int index2=2)
	{	//note this function can be optimized by keeping partial TLS sums and updating as we go
		if (readings.Count < 3)
			return;
		for (int i = 0; i < readings.Count; ++i)
			angle.Add(TotalLeastSquares.EstimateAngle(readings, i - 1, i + 1, index1, index2)); 
	}
}

// This component may be added to GameObject with Laser component
public class Features : MonoBehaviour
{	
	private bool Run { get; set; }
	private Thread scanThread;
	private AutoResetEvent scanWaitEvent = new AutoResetEvent(false);

	private Queue<Scan360> waitingScans=new Queue<Scan360>();
	private object waitingScansLock = new object();

	void Awake()
	{		
	}
	void Start ()
	{
		print(name + " - starting extraction thread");
		StartScanThread();
	}	
	void Update ()
	{
		// this is the place to retrieve feature extraction in a thread safe manner and visualize it
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

	void ScanThreadMain()
	{
		Queue<Scan360> scans = new Queue<Scan360>();
		Queue<Scan360> temp = new Queue<Scan360>();

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

			while (scans.Count > 0)
				ProcessScan(scans.Dequeue());
		}

		print("Features - scan thread finished");
	}

	void ProcessScan(Scan360 scan)
	{
		// this is the place to make the actual feature extraction

		scan.EstimateLocalAngle();
		//dbscan

		// here is the place to push results to Unity in a thread safe manner
	}

	#endregion
}
