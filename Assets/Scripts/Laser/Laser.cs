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

public enum PlotType {Local, Global, Map, GlobalWithMap}

[Serializable]
public class LaserModuleProperties : ModuleProperties
{
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
	public Vector3[] readings=new Vector3[360];
	public bool[] invalid_data = new bool[360];
	public int from = -1;
	public int length = -1;
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
		from = thr_data.from;
		length = thr_data.length;
		averagedPacketTimeMs = thr_data.averagedPacketTimeMs;
		laserRPM = thr_data.laserRPM;
		crcFailurePercentage = thr_data.crcFailurePercentage;
		invalidPercentage = thr_data.invalidPercentage;

		for (int i = from; i < from + length; ++i)
		{
			int ind = i % readings.Length;
			readings[ind] = thr_data.readings[ind];
			invalid_data[ind] = thr_data.invalid_data[ind];
		}
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
	public Vector3[] readings = new Vector3[360];
	public bool[] invalid_data = new bool[360];
	public ulong[] timestamps = new ulong[360];

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

	public void SetPending(int from, int length, ulong time_from, ulong time_to)
	{
		pending = true;
		pending_from = from;
		pending_length = length;
		t_from = time_from;
		t_to = time_to;
	}
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
	private Features features;

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
		features = GetComponent<Features>();
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
			threadShared.consumed = true;
		}
			
		if (data.length > 360)
			print(name + " - huh, does this ever happen? If so we can optimize");

		if(plot.plotType!=PlotType.Map)
			laserPointCloud.SetVertices(data.readings);

		if(map3D!=null && plot.plotType==PlotType.Map || plot.plotType==PlotType.GlobalWithMap)
			map3D.AssignVertices (data.readings, data.from, data.length, data.invalid_data);

	}

	#region UDP Thread Only Functions

	protected override void ProcessPacket(LaserPacket packet)
	{			
		threadInternal.laserRPM = packet.laser_speed / 64.0f;
		int i_from, len;
		ulong t_from, t_to;

		// if we had unprocessed packet last time do it now
		if (plot.plotType != PlotType.Local && threadInternal.pending)
		{
			i_from = threadInternal.pending_from; len=threadInternal.pending_length;
			t_from = threadInternal.t_from; t_to = threadInternal.t_to;
			if (TranslateReadingsToGlobalReferenceFrame (ref i_from,ref len, t_from, t_to))
				PushCalculatedReadingsThreadSafe (i_from, len);
		}

		CalculateReadingsInLocalReferenceFrame(packet);

		i_from = packet.laser_angle; len = packet.laser_readings.Length; t_from = packet.timestamp_us; t_to = packet.GetEndTimestampUs();

		if (plot.plotType != PlotType.Local)
		if (!TranslateReadingsToGlobalReferenceFrame (ref i_from, ref len, t_from, t_to))
				return; //don't use the readings yet (or at all), no position data in this timeframe

		PushCalculatedReadingsThreadSafe (i_from, len);
	}

	private void CalculateReadingsInLocalReferenceFrame(LaserPacket packet)
	{
		int angle_index;
		float alpha, distance_mm, angle;
		Vector3 pos;

		Vector3[] readings = threadInternal.readings;
		bool[] invalid_data = threadInternal.invalid_data;
		ulong[] timestamps = threadInternal.timestamps;

		for (int i = 0; i < packet.laser_readings.Length; ++i)		
		{
			angle_index = packet.laser_angle + i;
			angle = angle_index;

			if (angle_index == 0)
			{
				threadInternal.crcFailurePercentage = threadInternal.crcFailures * 100 / 360;
				threadInternal.invalidPercentage = threadInternal.invalidCount * 100 / 360;
				threadInternal.crcFailures = threadInternal.invalidCount = 0;
			}
				
			readings [angle_index] = Vector3.zero;
			timestamps[angle_index] = packet.GetTimestampUs(i);
			invalid_data[angle_index] = packet.laser_readings[i].invalid_data == 1;

			if (invalid_data[angle_index])
			{
				++threadInternal.invalidCount;
				if (packet.laser_readings[i].distance == LIDAR_CRC_FAILURE_ERROR_CODE)
					++threadInternal.crcFailures;
			}
				
			//if distance is greater than maximum we allow, mark reading as inalid
			invalid_data[angle_index] |= packet.laser_readings[i].distance > plot.distanceLimit * 1000;

			if (invalid_data[angle_index])
				continue;
	
			// calculate reading in laser plane
			distance_mm = packet.laser_readings[i].distance;
			alpha = angle - Constants.BETA;

			pos.x = -(distance_mm * (float)Mathf.Sin(angle * Constants.DEG2RAD) + Constants.B * (float)Mathf.Sin(alpha * Constants.DEG2RAD)) / 1000.0f;
			pos.y = 0;
			pos.z = (distance_mm * (float)Mathf.Cos(angle * Constants.DEG2RAD) + Constants.B * (float)Mathf.Cos(alpha * Constants.DEG2RAD)) / 1000.0f;

			// translate/rotate reading taking into acount laser mounting position and rotation
			readings[angle_index] = laserTRS.MultiplyPoint3x4 (pos);
		}
	}

	private bool TranslateReadingsToGlobalReferenceFrame(ref int from,ref int len, ulong t_from, ulong t_to)
	{
		bool not_in_history, not_yet;

		if (threadInternal.pending && t_from != threadInternal.t_from)
			AddPendingDataToProcess(ref from, ref len, ref t_from);

		PositionHistory.PositionSnapshot snapshot= positionHistory.GetPositionSnapshotThreadSafe(t_from, t_to, out not_yet, out not_in_history);

		if (not_in_history)
		{
			print("Laser - ignoring packet (position data not in history) with timestamp " + t_from);
			threadInternal.pending = false;
			return false;
		}
		if (not_yet)
		{
			threadInternal.SetPending (from, len, t_from, t_to);
			return false;
		}

		threadInternal.pending = false;

		Matrix4x4 robotToGlobal = new Matrix4x4();
		Vector3 scale = Vector3.one;
		PositionData pos=new PositionData();
		ulong[] timestamps = threadInternal.timestamps;
		Vector3[] readings = threadInternal.readings;

		for (int i = from, ind; i < from+len; ++i)
		{
			ind = i % 360;
	
			pos = snapshot.PositionAt(timestamps[ind]);

			robotToGlobal.SetTRS(pos.position, Quaternion.Euler(0.0f, pos.heading, 0.0f), scale);
			readings[ind]=robotToGlobal.MultiplyPoint3x4(readings[ind]);
		}
			
		return true;
	}

	private void AddPendingDataToProcess(ref int from,ref int len, ref ulong t_from)
	{
		from = threadInternal.pending_from;
		len = threadInternal.pending_length + len;
		t_from = threadInternal.t_from;

		//Discard some pending data if it exceeds 360 degrees (single scan)
		if (len > 360)
		{
			int exceeds_by = len - 360;
			len -= exceeds_by;
			from = (from + exceeds_by) % 360;
			t_from = threadInternal.timestamps[from];
		}
	}

	public void PushCalculatedReadingsThreadSafe(int from, int length)
	{
		int readingsLength = threadInternal.readings.Length;

		lock (threadShared)
		{
			for (int i = from, ind; i < from + length; ++i)
			{
				ind = i % readingsLength;
				threadShared.readings[ind] = threadInternal.readings[ind];
				threadShared.invalid_data[ind] = threadInternal.invalid_data[ind];
			}
				
			if (threadShared.consumed)
			{
				threadShared.from = from;
				threadShared.length = length;
			}
			else //data was not consumed yet
				threadShared.length += length;

			threadShared.consumed = false;
			threadShared.averagedPacketTimeMs = AveragedPacketTimeMs();
			threadShared.laserRPM = threadInternal.laserRPM;
			threadShared.invalidPercentage = threadInternal.invalidPercentage;
			threadShared.crcFailurePercentage = threadInternal.crcFailurePercentage;

			if (threadShared.snapshot_request)
			{
				threadShared.snapshot_request = false;
				threadInternal.snapshots_left = threadShared.snapshots_left;
			}
			threadShared.snapshots_left = threadInternal.snapshots_left;
		}
			
		if (from + length < 360)
			return;
		if (features != null)
			features.PutScanThreadSafe(threadInternal.readings, threadInternal.invalid_data);

		if (threadInternal.snapshots_left == 0)
			return;
		//just finished revolution, dump to file

		--threadInternal.snapshots_left;
	
		DumpSnapshot();
	}

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
		return "ev3laser " + module.laserDevice + " " + module.motorPort + " " + network.hostIp + " " + moduleNetwork.port + " " + module.laserDutyCycle + " " + module.crcTolerancePct;
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
