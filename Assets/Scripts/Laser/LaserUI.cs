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
using UnityEngine.UI;

public class LaserUI : ModuleUI
{
	private Text ppsText;
	private Text laserSpeedText;
	private Text laserInvalidCRCText;
	private Text featuresElapsedTimeText;

	private Laser laser;
	private Features features;

	protected override void Awake()
	{
		base.Awake();
		ppsText = SafeInstantiateText(ModuleText, uiTransform, "pps 00 ms 00");
		laserSpeedText = SafeInstantiateText(ModuleText, uiTransform, "rpm 000");
		laserInvalidCRCText = SafeInstantiateText(ModuleText, uiTransform, "inv 000 crc 000");
		features = GetComponent<Features>();
		if (features != null)
			featuresElapsedTimeText = SafeInstantiateText(ModuleText, uiTransform, "seg 00 ms");

	}

	protected override void Start ()
	{
		base.Start();
		laser = module as Laser;
	}

	protected override void Update ()
	{
		base.Update();
		float avgPacketMs = laser.GetAveragedPacketTimeMs();

		if (avgPacketMs != 0)
			ppsText.text = string.Format("pps {0:00} ms {1:00}", 1000.0f / avgPacketMs, avgPacketMs);

		int snapshotsLeft = laser.GetSnapshotsLeft();
		string laserSpeedAndSnapshots;
		if(snapshotsLeft == 0)
			laserSpeedAndSnapshots = string.Format("rpm {0:000}", laser.GetAveragedLaserRPM());
		else
			laserSpeedAndSnapshots = string.Format("rpm {0:000} snp {1:00}", laser.GetAveragedLaserRPM(), laser.GetSnapshotsLeft());
				
		laserSpeedText.text = laserSpeedAndSnapshots;
		laserInvalidCRCText.text = string.Format("inv {0:00} crc {1:00}",laser.GetInvalidPercentage() ,laser.GetCRCFailurePercentage());

		if(features!=null)
			featuresElapsedTimeText.text = string.Format("seg {0:00} ms", features.SegmentationElapsedMs);
		
	}
}
