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
using System;

[Serializable]
public class DifferentialDrivePhysicalProperties
{
	public float wheelDiameterMm=43.2f;
	public float wheelDistanceMm=250f;
	public int encoderCountsPerRotation=360;
	public int maxTheorethicalCountsPerRotation=1000;
	public bool reverseMotorPolarity=false;

	public float MMPerCount()
	{
		return Mathf.PI * wheelDiameterMm / encoderCountsPerRotation;
	}
	public float CountsPerMM()
	{
		return encoderCountsPerRotation / (Mathf.PI * wheelDiameterMm);
	}
}

[Serializable]
public class DifferentialDriveLimitProperties
{
	public float MaxLinearSpeedMmPerSec=100;
	public float MaxAngularSpeedDegPerSec=90;

	public float AngularSpeedRadPerS()
	{
		return Mathf.Deg2Rad * MaxAngularSpeedDegPerSec;
	}
}

public class DifferentialDrive : MonoBehaviour
{
	public DifferentialDrivePhysicalProperties physics;
	public DifferentialDriveLimitProperties limits;

	private float maxAngularSpeedContributionMmPerS;
	private float countsPerMM;

	void Awake()
	{
		maxAngularSpeedContributionMmPerS = limits.AngularSpeedRadPerS() * physics.wheelDistanceMm / 2.0f;

		countsPerMM = physics.CountsPerMM ();

		short left, right;
		InputToEngineSpeeds (1.0f, 1.0f, 1.0f, out left, out right);

		if (Math.Abs(left) > physics.maxTheorethicalCountsPerRotation)
			Debug.LogWarning ("Limits of differential drive exceed physical capabilities: leads to " + Mathf.Abs(left) + " speed where limit is " + physics.maxTheorethicalCountsPerRotation);	
	}

	public void InputToEngineSpeeds(float in_hor, float in_ver, float in_scale,out short left_counts_s,out short right_counts_s)
	{
		float V_mm_s = in_ver * limits.MaxLinearSpeedMmPerSec;
		float angular_speed_contrib_mm_s = maxAngularSpeedContributionMmPerS * in_hor;

		float VL_mm_s = V_mm_s + angular_speed_contrib_mm_s;
		float VR_mm_s = V_mm_s - angular_speed_contrib_mm_s;

		float VL_counts_s = VL_mm_s * countsPerMM;
		float VR_counts_s = VR_mm_s * countsPerMM;

		float scale = in_scale * ( (physics.reverseMotorPolarity) ? -1.0f : 1.0f );

		left_counts_s = (short)(VL_counts_s * scale);
		right_counts_s = (short)(VR_counts_s * scale);
	}
	public void DistanceAndSpeedToEngineCountsAndSpeed(float distance_cm, float speed_cm_per_s, out short l_counts_s, out short r_counts_s, out short l_counts, out short r_counts)
	{
		float counts = distance_cm * 10.0f * countsPerMM;
		float counts_per_s = speed_cm_per_s * 10.0f * countsPerMM;

		float scale = ((physics.reverseMotorPolarity) ? -1.0f : 1.0f);
		l_counts = r_counts = (short)(scale * counts);
		l_counts_s = r_counts_s =(short)(counts_per_s * scale);
		//TO DO - check limits!
	}
}
