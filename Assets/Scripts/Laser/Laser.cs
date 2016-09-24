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
public enum PlaneType {XZ, XY}
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
	public Map3D map3D;
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
	
public class Laser : ReplayableUDPServer<LaserPacket>, IRobotModule
{
	public LaserModuleProperties module;
	public LaserPlotProperties plot;
	public LaserUI laserUI;

	private PointCloud laserPointCloud;
	private Map3D map3D;
	private PositionHistory positionHistory;

	private LaserThreadSharedData data=new LaserThreadSharedData();

	Matrix4x4 laserTRS;
	private Vector3 laserPosition;
	private Vector3 laserRotation;

	#region UDP Thread Only Data
	private LaserThreadInternalData threadInternal = new LaserThreadInternalData ();
	#endregion

	#region Thread Shared Data
	private LaserThreadSharedData threadShared=new LaserThreadSharedData();
	#endregion

	public override string GetUniqueName ()
	{
		return name;
	}
		
	protected override void OnDestroy()
	{
		base.OnDestroy ();
	}

	protected override void Awake()
	{
		laserPointCloud = SafeInstantiate<PointCloud> (plot.laserPointCloud);
		map3D = SafeInstantiate<Map3D> (plot.map3D);
		base.Awake();
		SafeInstantiate<LaserUI>(laserUI).SetModuleDataSource(this);

		laserTRS =  Matrix4x4.TRS (transform.localPosition, transform.localRotation, Vector3.one);
	}

	protected override void Start ()
	{
		positionHistory = SafeGetComponentInParent<PositionHistory>();	
		base.Start();
	}
	public void StartReplay()
	{
		base.StartReplay(0);
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

		if(plot.plotType==PlotType.Map || plot.plotType==PlotType.GlobalWithMap)
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

	#region Init

	private T SafeInstantiate<T>(T original) where T : MonoBehaviour
	{
		if (original == null)
		{
			Debug.LogError ("Expected to find prefab of type " + typeof(T) + " but it was not set");
			return default(T);
		}
		return Instantiate<T>(original);
	}

	private T SafeGetComponentInParent<T>() where T : MonoBehaviour
	{
		T component = GetComponentInParent<T> ();

		if (component == null)
			Debug.LogError ("Expected to find component of type " + typeof(T) + " but found none");

		return component;
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

		string filename = GetUniqueName() + ".ply";
		print("saving map to file \"" + filename + "\"");

		map3D.SaveToPlyPolygonFileFormat(Config.MapPath(filename), "created with ev3dev-mapping");
	}

	#endregion

	#region IRobotModule 

	private ModuleState moduleState=ModuleState.Offline;

	public ModuleState GetState()
	{
		return moduleState;
	}
	public void SetState(ModuleState state)
	{
		moduleState = state;
	}
		
	public string ModuleCall()
	{
		return "ev3laser " + module.laserDevice + " " + module.motorPort + " " + network.hostIp + " " + udp.port + " " + module.laserDutyCycle;
	}
	public int ModulePriority()
	{
		return module.priority;
	}
	public bool ModuleAutostart()
	{
		return module.autostart && !replay.ReplayInbound();
	}
	public int CreationDelayMs()
	{
		return module.creationDelayMs;
	}

	public int CompareTo(IRobotModule other)
	{
		return ModulePriority().CompareTo( other.ModulePriority() );
	}

	public Control GetControl()
	{
		return GetComponent<Control>();
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
}
