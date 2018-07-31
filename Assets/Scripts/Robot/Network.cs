using UnityEngine;
using System.Collections;

namespace Ev3devMapping
{

public class Network : MonoBehaviour
{
	public string hostIp="192.168.0.103";
	public string robotIp="192.168.0.101";

	public Network DeepCopy()
	{
		Network other = (Network) this.MemberwiseClone();
		other.hostIp = string.Copy(hostIp);
		other.robotIp = string.Copy(robotIp);
		return other;
	}
}

} //namespace
