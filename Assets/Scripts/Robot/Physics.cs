using UnityEngine;
using System.Collections;

namespace Ev3devMapping
{

public class Physics : MonoBehaviour
{
	public float wheelDiameterMm=43.2f;
	public float wheelbaseMm=250.0f;
	public int encoderCountsPerRotation=360;
	public int maxEncoderCountsPerSecond=1000;
	public bool reverseMotorPolarity=false;

	public float MMPerCount()
	{
		return Mathf.PI * wheelDiameterMm / encoderCountsPerRotation;
	}
	public float CountsPerMM()
	{
		return encoderCountsPerRotation / (Mathf.PI * wheelDiameterMm);
	}
		
	public Physics DeepCopy()
	{
		Physics other = (Physics) this.MemberwiseClone();
		return other;
	}
}

} //namespace