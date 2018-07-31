using UnityEngine;
using System.Collections;

namespace Ev3devMapping
{

[RequireComponent (typeof (Network))]
[RequireComponent (typeof (Replay))]
[RequireComponent (typeof (PositionHistory))]
[RequireComponent (typeof (Physics))]
[RequireComponent (typeof (Limits))]
[RequireComponent (typeof (UserInput))]
public class RobotRequired : MonoBehaviour
{
	public string sessionDirectory="Mapping1";

	public RobotRequired DeepCopy()
	{
		RobotRequired other = (RobotRequired) this.MemberwiseClone();
		other.sessionDirectory = string.Copy(sessionDirectory);
		return other;
	}
}

} //namespace