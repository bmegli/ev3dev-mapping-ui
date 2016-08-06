using UnityEngine;
using System.Collections;

public class MotionModel : MonoBehaviour
{
	public virtual PositionData EstimatePosition(PositionData lastPosition, OdometryPacket lastPacket, OdometryPacket packet)
	{
		lastPosition.timestamp = packet.timestamp_us;
		return lastPosition;
	}
}
