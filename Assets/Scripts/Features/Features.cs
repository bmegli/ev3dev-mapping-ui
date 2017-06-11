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
