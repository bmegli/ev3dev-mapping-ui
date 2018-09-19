/*
 * Copyright (C) 2016-2017 Bartosz Meglicki <meglickib@gmail.com>
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
using System;
using System.IO;
using CircularBuffer;

namespace Ev3devMapping
{

	public enum PlotType {Local, Global, Map, GlobalWithMap}

	[Serializable]
	public class LaserModuleProperties : ModuleProperties
	{
		public string program="ev3laser";
		public string laserDevice = "/dev/tty_in1";
		public string motorPort = "outC";
		public int laserDutyCycle = 44;
		public int crcTolerancePct = 10;
	}
		
	[Serializable]
	public class LaserPlotProperties
	{
		public PlotType plotType;
		public float distanceLimit=10.0f;
		public PointCloud laserPointCloud;
	}

	[Serializable]
	public class LaserSnapshotProperties
	{
		public int snapshotNumber=20;
	}
		
	class LaserThreadSharedData
	{
		const int READINGS_SIZE = 360 * 3;
		public CircularBuffer<Vector3> readings = new CircularBuffer<Vector3> (READINGS_SIZE, true);

		public bool consumed = true;
		public float averagedPacketTimeMs;
		public float laserRPM;
		public int crcFailurePercentage=0;
		public int invalidPercentage=0;
		public bool snapshot_request=false;
		public int snapshots_left=0;

		public void CopyNewDataFrom(LaserThreadSharedData thr_data)
		{			
			// Copy only the data that changed since last call
			averagedPacketTimeMs = thr_data.averagedPacketTimeMs;
			laserRPM = thr_data.laserRPM;
			crcFailurePercentage = thr_data.crcFailurePercentage;
			invalidPercentage = thr_data.invalidPercentage;

			readings.Put (thr_data.readings);
		}
		public void HandleSnapshotRequest(LaserThreadSharedData unity_data)
		{
			if (snapshots_left == 0 && unity_data.snapshot_request)
			{
				snapshots_left = unity_data.snapshots_left; 
				snapshot_request = true;
				unity_data.snapshot_request=false;
			}

			unity_data.snapshots_left = snapshots_left;		
		}

	}

	class LaserThreadInternalData
	{
		const int READINGS_SIZE = 360 * 3;
		public CircularBuffer<Vector3> readings = new CircularBuffer<Vector3> (READINGS_SIZE, true);
		public CircularBuffer<ulong> timestamps = new CircularBuffer<ulong>(READINGS_SIZE, true);

		public bool pending=false;
		public int pending_from=0;
		public int pending_length=0;
		public ulong t_from=0;
		public ulong t_to=0;
		public float laserRPM;
		public int invalidCount=0;
		public int invalidPercentage = 0;
		public int crcFailures=0;
		public int crcFailurePercentage=0;
		public int snapshots_left=0;
	}

	//OptionalComponent (typeof (Features))
	[RequireComponent (typeof (LaserUI))]
	[RequireComponent (typeof (Map3D))]
	public class Laser : ReplayableUDPServer<LaserPacket>
	{
		public const ushort LIDAR_CRC_FAILURE_ERROR_CODE = 0x66;
		public const int INVALID_DATA_SNAPSHOT_REPLACEMENT_VALUE = 0;

		public LaserModuleProperties module;
		public LaserPlotProperties plot;
		public LaserSnapshotProperties snapshot;

		private PointCloud laserPointCloud;
		private Map3D map3D;
		private LaserFeatures features;

		private LaserThreadSharedData data=new LaserThreadSharedData();

		private Matrix4x4 laserTRS;

		private StreamWriter snapshotWriter;

		#region UDP Thread Only Data
		private LaserThreadInternalData threadInternal = new LaserThreadInternalData ();
		#endregion

		#region Thread Shared Data
		private LaserThreadSharedData threadShared = new LaserThreadSharedData();
		#endregion
				
		protected override void OnDestroy()
		{
			base.OnDestroy ();
			if(snapshotWriter != null)
				snapshotWriter.Close();		
		}

		protected override void Awake()
		{
			base.Awake();
			laserPointCloud = SafeInstantiate<PointCloud> (plot.laserPointCloud);
			laserTRS =  Matrix4x4.TRS (transform.localPosition, transform.localRotation, Vector3.one);

			string robotName = transform.parent.name;

			print(name + " - saving maps to \"" + Config.MapPath(robot.sessionDirectory, robotName, name) + "\"");
			Directory.CreateDirectory(Config.MapPath(robot.sessionDirectory, robotName));

			print(name + " - dumping snapshots to '" + Config.SnapshotPath(robot.sessionDirectory, robotName, name) + "'");
			Directory.CreateDirectory(Config.SnapshotPath(robot.sessionDirectory, robotName));
			snapshotWriter = new StreamWriter(Config.SnapshotPath(robot.sessionDirectory, robotName, name));
		}


		protected override void Start ()
		{
			map3D = GetComponent<Map3D>();
			features = GetComponent<LaserFeatures>();
			base.Start();
		}
			
		void Update ()
		{
			lock (threadShared)
			{
				threadShared.HandleSnapshotRequest(data);
				if (threadShared.consumed)
					return; //no new data, nothing to do
				data.CopyNewDataFrom(threadShared);
				threadShared.readings.Clear();
				threadShared.consumed = true;
			}
				
			if(plot.plotType!=PlotType.Map && features==null)
				laserPointCloud.SetVertices(data.readings.GetBuffer());

			if (map3D != null && plot.plotType == PlotType.Map || plot.plotType == PlotType.GlobalWithMap)
			{	//rethink if ok (global with map will not work as expected)
				map3D.AssignVertices (data.readings.GetBuffer(), data.readings.Size);
				data.readings.Clear ();
			}
		}

		#region UDP Thread Only Functions

		protected override void ProcessPacket(LaserPacket packet)
		{	
			// if we had unprocessed packet last time do it now
			if (plot.plotType != PlotType.Local && threadInternal.pending)
				if (TranslateReadingsToGlobalReferenceFrame ())
					PushCalculatedReadingsThreadSafe ();
		
			CalculateReadingsInLocalReferenceFrame(packet);

			if (plot.plotType != PlotType.Local)
				if (!TranslateReadingsToGlobalReferenceFrame ())
					return; //don't use the readings yet (or at all), no position data in this timeframe
			
			
			PushCalculatedReadingsThreadSafe();
			
		}
		
		private void CalculateReadingsInLocalReferenceFrame(LaserPacket packet)
		{
			float angle, distance_mm;
			Vector3 pos;

			CircularBuffer<Vector3> readings = threadInternal.readings;
			CircularBuffer<ulong> timestamps = threadInternal.timestamps;

			for (int i = 0; i < packet.laser_readings.Length; ++i)		
			{
				if (packet.laser_readings [i].distance_mm == 0 || packet.laser_readings [i].distance_mm > plot.distanceLimit * 1000)
					continue;

				// calculate reading in laser plane
				distance_mm = packet.laser_readings[i].distance_mm;
				angle = packet.laser_readings[i].AngleDeg;

				pos.x = -(distance_mm * (float)Mathf.Sin(angle * Constants.DEG2RAD)) / 1000.0f;
				pos.y = 0;
				pos.z = (distance_mm * (float)Mathf.Cos(angle * Constants.DEG2RAD)) / 1000.0f;

				readings.Put(laserTRS.MultiplyPoint3x4 (pos));
				timestamps.Put (packet.GetTimestampUs (i));
			}
		}

		private bool TranslateReadingsToGlobalReferenceFrame()
		{
			CircularBuffer<ulong> timestamps = threadInternal.timestamps;
			CircularBuffer<Vector3> readings = threadInternal.readings;
			bool not_in_history, not_yet;
			ulong t_from, t_to;

			if (timestamps.Size == 0)
				return false;

			t_from = timestamps.PeekOldest ();
			t_to = timestamps.PeekNewest ();

			PositionHistory.PositionSnapshot snapshot= positionHistory.GetPositionSnapshotThreadSafe(t_from, t_to, out not_yet, out not_in_history);

			if (not_in_history)
			{
				print("Laser - ignoring packet (position data not in history) with timestamp " + t_from);
				threadInternal.pending = false;
				//possibly just purge the relevant fragment
				timestamps.Clear ();
				readings.Clear ();
				return false;
			}
			if (not_yet)
			{
				print("Laser - ignoring packet (position data not yet in history) " + t_from);
				threadInternal.pending = true;
				return false;
			}

			threadInternal.pending = false;

			Matrix4x4 robotToGlobal = new Matrix4x4();
			Vector3 scale = Vector3.one;
			PositionData pos=new PositionData();

			for (int i = 0; i < readings.Size; ++i)
			{	
				pos = snapshot.PositionAt(timestamps[i]);

				//robotToGlobal.SetTRS(pos.position, Quaternion.Euler(0.0f, pos.heading, 0.0f), scale);
				robotToGlobal.SetTRS(pos.position, pos.quaternion, scale);
				readings[i]=robotToGlobal.MultiplyPoint3x4(readings[i]);
			}
				
			return true;
		}

		public void PushCalculatedReadingsThreadSafe()
		{
			if (threadInternal.readings.Size == 0)
				return;

			lock (threadShared)
			{
				threadShared.readings.Put (threadInternal.readings);
				threadInternal.readings.Clear ();
				threadInternal.timestamps.Clear ();

				threadShared.consumed = false;
				threadShared.averagedPacketTimeMs = AveragedPacketTimeMs();
				threadShared.laserRPM = threadInternal.laserRPM;
				threadShared.invalidPercentage = threadInternal.invalidPercentage;
				threadShared.crcFailurePercentage = threadInternal.crcFailurePercentage;
			}
		}
		/*

		void DumpSnapshot()
		{
			Vector3[] readings = threadInternal.readings;
			bool[] invalid = threadInternal.invalid_data;

			for (int i = 0; i < 360; ++i)
			{						
				for (int j = 0; j < 3; ++j)
				{
					int coordinate = (int)(readings[i][j] * 1000);
					if (invalid[i])
						coordinate = INVALID_DATA_SNAPSHOT_REPLACEMENT_VALUE;
					snapshotWriter.Write(coordinate);
				
					if(!(i==359 && j==2))
						snapshotWriter.Write(";");
				}
			}

			snapshotWriter.WriteLine();
		}

		*/

		#endregion

		#region UI reactions

		public void SaveMap()
		{
			if (map3D == null)
			{
				print(name + " - map is null, unable to save!");
				return;
			}

			string robotName = transform.parent.name;

			print(name + " - saving map to file \"" + Config.MapPath(robot.sessionDirectory, robotName, name) + "\"");

			map3D.SaveToPlyPolygonFileFormat(Config.MapPath(robot.sessionDirectory, robotName, name), "created with ev3dev-mapping");
		}

		public void TakeSnapshot()
		{
			if (data.snapshots_left > 0 || data.snapshot_request)
			{
				print(name + " - ignoring snapshot request (in progress)");
				return;
			}

			string robotName = transform.parent.name;

			print(name + " - dumping snapshot to file \"" + Config.SnapshotPath(robot.sessionDirectory, robotName, name) + "\"");

			data.snapshot_request = true;
			data.snapshots_left = snapshot.snapshotNumber;
		}

		#endregion

		public float GetAveragedPacketTimeMs()
		{
			return data.averagedPacketTimeMs;
		}
		public float GetAveragedLaserRPM()
		{
			return data.laserRPM;
		}
		public float GetInvalidPercentage()
		{
			return data.invalidPercentage;
		}

		public float GetCRCFailurePercentage()
		{
			return data.crcFailurePercentage;
		}

		public int GetSnapshotsLeft()
		{
			return data.snapshots_left;
		}

		#region RobotModule

		public override string ModuleCall()
		{
			return  module.program + " " + module.laserDevice + " " + network.hostIp + " " + moduleNetwork.port;
		}
		public override int ModulePriority()
		{
			return module.priority;
		}
		public override bool ModuleAutostart()
		{
			return module.autostart;
		}
		public override int CreationDelayMs()
		{
			return module.creationDelayMs;
		}

		#endregion
	}

} //namespace