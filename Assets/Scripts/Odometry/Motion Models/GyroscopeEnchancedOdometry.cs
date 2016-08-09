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

[Serializable]
public class GyroscopeEnchancedOdometryProperties
{
	public float wheelDiameterMilimeters=43.2f;
	public int encoderCountsPerRotation=360;
	public bool reverseMotorPolarity=false;
}

public class GyroscopeEnchancedOdometry : MotionModel
{
	public GyroscopeEnchancedOdometryProperties motionModel;

	public override PositionData EstimatePosition(PositionData lastPosition, OdometryPacket lastPacket, OdometryPacket packet)
	{
		// Calculate the linear displacement since last packet
		float distance_per_encoder_count_mm = Mathf.PI * motionModel.wheelDiameterMilimeters / motionModel.encoderCountsPerRotation;
		float ldiff = packet.position_left - lastPacket.position_left;
		float rdiff = packet.position_right - lastPacket.position_right;
		float displacement_m = (ldiff + rdiff) * distance_per_encoder_count_mm / 2.0f / Constants.MM_IN_M;

		if (motionModel.reverseMotorPolarity)
			displacement_m = -displacement_m;

		// Calculate the average heading from previous and current packet
		float angle_start_deg = lastPacket.HeadingInDegrees;
		float angle_end_deg = packet.HeadingInDegrees;
		float angle_difference_deg = angle_end_deg - angle_start_deg;

		// The tricky case when we cross 0 or -180/180 in packets has to be handled separately
		if (Mathf.Abs(angle_difference_deg) > 180.0f)
			angle_difference_deg = angle_difference_deg -  Mathf.Sign(angle_difference_deg) * 360.0f;

		float average_heading_rad = (angle_start_deg + angle_difference_deg / 2.0f) * Constants.DEG2RAD;

		// Finally update the position and heading
		lastPosition.timestamp = packet.timestamp_us;
		lastPosition.position = new Vector3(lastPosition.position.x + displacement_m * Mathf.Sin(average_heading_rad), lastPosition.position.y, lastPosition.position.z + displacement_m * Mathf.Cos(average_heading_rad));
		lastPosition.heading = angle_end_deg;

		return lastPosition;
	}
}
