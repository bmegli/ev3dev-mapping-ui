using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class OdometryModuleProperties : ModuleProperties
{
	public string hostIp="192.168.0.103";
}

[RequireComponent (typeof (PositionHistory))]
public class Odometry : ReplayableUDPServer<OdometryPacket>, IRobotModule
{
	public MotionModel motionModel;
	public OdometryUI odometryUI;
	public OdometryModuleProperties module;

	private MotionModel model;

	private PositionHistory positionHistory;
	private PositionData actualPosition;
	private float averagedPacketTimeMs;

	#region UDP Thread Only Data
	private OdometryPacket lastPacket=new OdometryPacket();
	private PositionData lastPosition=new PositionData();
	#endregion

	#region Thread Shared Data
	private object odometryLock=new object();
	private PositionData thread_shared_position=new PositionData();
	private float thread_shared_averaged_packet_time_ms;
	#endregion

	protected override void OnDestroy()
	{
		base.OnDestroy ();
	}
		
	protected override void Awake()
	{
		base.Awake();
		model = SafeInstantiate<MotionModel>(motionModel);
		model.transform.parent = transform;
		SafeInstantiate<OdometryUI>(odometryUI).SetModuleDataSource(this);
	}

	protected override void Start ()
	{
		positionHistory = SafeGetComponent<PositionHistory>();
		base.Start();
	//	base.StartReplay(20000);
	}

	public void StartReplay()
	{
		base.StartReplay(20000);
	}
		
	void Update ()
	{
		lock (odometryLock)
		{
			actualPosition = thread_shared_position;
			averagedPacketTimeMs = thread_shared_averaged_packet_time_ms;
		}
			
		transform.position = actualPosition.position;
		transform.rotation = Quaternion.Euler(0.0f, actualPosition.heading, 0.0f);
	}

	#region UDP Thread Only Functions

	protected override void ProcessPacket(OdometryPacket packet)
	{
		//First call - set first udp packet with reference encoder positions
		if (lastPacket.timestamp_us == 0)
		{ 
			lastPacket.CloneFrom(packet);
			return;
		}
			
		// UDP doesn't guarantee ordering of packets, if previous odometry is newer ignore the received
		if (packet.timestamp_us <= lastPacket.timestamp_us)
		{
			print("odometry - ignoring out of time packet (previous, now):" + Environment.NewLine + lastPacket.ToString() + Environment.NewLine + packet.ToString());
			return;
		}

		// Use the motion model to estimate positoin
		lastPosition=motionModel.EstimatePosition(lastPosition, lastPacket, packet);
		lastPacket.CloneFrom(packet);

		// Share the new calculated position estimate with Unity thread
		lock (odometryLock)
		{
			thread_shared_position=lastPosition;
			thread_shared_averaged_packet_time_ms = AveragedPacketTimeMs();
		}

		positionHistory.PutThreadSafe(lastPosition);

	}
		
	#endregion

	public PositionHistory.PositionSnapshot GetPositionSnapshotThreadSafe(ulong timestamp_from, ulong timestamp_to, out bool no_position_data_yet, out bool data_not_in_history)
	{
		return positionHistory.GetPositionSnapshotThreadSafe(timestamp_from, timestamp_to, out no_position_data_yet, out data_not_in_history);
	}

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
		
	private T SafeGetComponent<T>() where T : MonoBehaviour
	{
		T component = GetComponent<T> ();

		if (component == null)
			Debug.LogError ("Expected to find component of type " + typeof(T) + " but found none");

		return component;
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
		
	public override string GetUniqueName ()
	{
		return "odometry";
	}
		
	public string ModuleCall()
	{
		return "ev3odometry " + module.hostIp + " " + udp.port ;
	}
	public int ModulePriority()
	{
		return module.priority;
	}
	public bool ModuleAutostart()
	{
		return module.autostart && udp.replayMode!=UDPReplayMode.Replay;
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

	public Vector3 GetPosition()
	{
		return actualPosition.position;
	}
	public float GetHeading()
	{
		return actualPosition.heading;
	}

	public float GetAveragedPacketTimeMs()
	{
		return averagedPacketTimeMs;
	}
}
