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
using System;
using System.IO;

public enum PlotType {Local, Global, Map, GlobalWithMap}

[Serializable]
public class LaserModuleProperties : ModuleProperties
{
	public string laserDevice = "/dev/tty_in1";
	public string motorPort = "outC";
	public int laserDutyCycle = -44;
}
	
[Serializable]
public class LaserPlotProperties
{
	public PlotType plotType;
	public float distanceLimit=10.0f;
	public PointCloud laserPointCloud;
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

	public void CopyNewDataFrom(LaserThreadSharedData other)
	{
		// Copy only the data that changed since last call
		from = other.from;
		length = other.length;
		averagedPacketTimeMs = other.averagedPacketTimeMs;
		laserRPM = other.laserRPM;

		for (int i = from; i < from + length; ++i)
		{
			int ind = i % readings.Length;
			readings[ind] = other.readings[ind];
			invalid_data[ind] = other.invalid_data[ind];
		}
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

	public void SetPending(int from, int length, ulong time_from, ulong time_to)
	{
		pending = true;
		pending_from = from;
		pending_length = length;
		t_from = time_from;
		t_to = time_to;
	}
}

[RequireComponent (typeof (LaserUI))]
[RequireComponent (typeof (Map3D))]
public class Laser : ReplayableUDPServer<LaserPacket>
{
	public LaserModuleProperties module;
	public LaserPlotProperties plot;

	private PointCloud laserPointCloud;
	private Map3D map3D;

	private LaserThreadSharedData data=new LaserThreadSharedData();

	private Matrix4x4 laserTRS;

	#region UDP Thread Only Data
	private LaserThreadInternalData threadInternal = new LaserThreadInternalData ();
	#endregion

	#region Thread Shared Data
	private LaserThreadSharedData threadShared = new LaserThreadSharedData();
	#endregion
			
	protected override void OnDestroy()
	{
		base.OnDestroy ();
	}

	protected override void Awake()
	{
		base.Awake();
		laserPointCloud = SafeInstantiate<PointCloud> (plot.laserPointCloud);
		laserTRS =  Matrix4x4.TRS (transform.localPosition, transform.localRotation, Vector3.one);
	}

	protected override void Start ()
	{
		map3D = GetComponent<Map3D> ();
		base.Start();
	}

	void Update ()
	{
		lock (threadShared)
		{
			if (threadShared.consumed)
				return; //no new data, nothing to do
			data.CopyNewDataFrom(threadShared);
			threadShared.consumed = true;
		}
			
		if (data.length > 360)
			print ("Huh, does this ever happen? If so we can optimize");

		if(plot.plotType!=PlotType.Map)
			laserPointCloud.SetVertices(data.readings);

		if(map3D!=null && plot.plotType==PlotType.Map || plot.plotType==PlotType.GlobalWithMap)
			map3D.AssignVertices (data.readings, data.from, data.length, data.invalid_data);

	}

	#region UDP Thread Only Functions

	protected override void ProcessPacket(LaserPacket packet)
	{			
		threadInternal.laserRPM = packet.laser_speed / 64.0f;

		// if we had unprocessed packet last time do it now
		if (plot.plotType != PlotType.Local && threadInternal.pending)
		{
			int i_from = threadInternal.pending_from, len=threadInternal.pending_length;
			ulong t_from = threadInternal.t_from, t_to = threadInternal.t_to;
			if (TranslateReadingsToGlobalReferenceFrame (i_from, len, t_from, t_to))
				PushCalculatedReadingsThreadSafe (i_from, len);
		}

		CalculateReadingsInLocalReferenceFrame(packet);


		if (plot.plotType != PlotType.Local)
			if (!TranslateReadingsToGlobalReferenceFrame (packet.laser_angle, packet.laser_readings.Length, packet.timestamp_us, packet.GetEndTimestampUs()))
				return; //don't use the readings yet (or at all), no position data in this timeframe

		PushCalculatedReadingsThreadSafe (packet.laser_angle, packet.laser_readings.Length);
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

			readings [angle_index] = Vector3.zero;
			timestamps[angle_index] = packet.GetTimestampUs(i);
			invalid_data[angle_index] = packet.laser_readings[i].invalid_data == 1;
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

	private bool TranslateReadingsToGlobalReferenceFrame(int from, int len, ulong t_from, ulong t_to)
	{
		bool not_in_history, not_yet;
		PositionHistory.PositionSnapshot snapshot= positionHistory.GetPositionSnapshotThreadSafe(t_from, t_to, out not_yet, out not_in_history);
		threadInternal.pending = false;

		if (not_in_history)
		{
			print("laser - ignoring packet (position data not in history) with timestamp " + t_from);
			return false;
		}
		if (not_yet)
		{
			threadInternal.SetPending (from, len, t_from, t_to);
			return false;
		}
			
		Matrix4x4 robotToGlobal = new Matrix4x4();
		Vector3 scale = Vector3.one;
		PositionData pos=new PositionData();
		ulong[] timestamps = threadInternal.timestamps;
		Vector3[] readings = threadInternal.readings;

		for (int i = from; i < from+len; ++i)
		{
			pos = snapshot.PositionAt(timestamps[i]);

			robotToGlobal.SetTRS(pos.position, Quaternion.Euler(0.0f, pos.heading, 0.0f), scale);
			readings[i]=robotToGlobal.MultiplyPoint3x4(readings[i]);
		}
			
		return true;
	}

	public void PushCalculatedReadingsThreadSafe(int from, int length)
	{
		lock (threadShared)
		{
			Array.Copy(threadInternal.readings, from, threadShared.readings, from, length);
			Array.Copy(threadInternal.invalid_data, from, threadShared.invalid_data, from, length);

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
		}

	}


	#endregion

	#region UI reactions

	public void SaveMap()
	{
		if (map3D == null)
		{
			print("map is null, unable to save!");
			return;
		}

		Directory.CreateDirectory(Config.MAPS_DIRECTORY);
		Directory.CreateDirectory(Config.MapPath(robot.sessionDirectory));

		print("saving map to file \"" + Config.MapPath(robot.sessionDirectory, name) + "\"");

		map3D.SaveToPlyPolygonFileFormat(Config.MapPath(robot.sessionDirectory, name), "created with ev3dev-mapping");
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

	#region RobotModule

	public override string ModuleCall()
	{
		return "ev3laser " + module.laserDevice + " " + module.motorPort + " " + network.hostIp + " " + moduleNetwork.port + " " + module.laserDutyCycle;
	}
	public override int ModulePriority()
	{
		return module.priority;
	}
	public override bool ModuleAutostart()
	{
		return module.autostart && !replay.ReplayInbound();
	}
	public override int CreationDelayMs()
	{
		return module.creationDelayMs;
	}

	#endregion

}
