﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LaserUI : ModuleUI
{
	private Text ppsText;
	private Text laserSpeedText;

	private Laser laser;

	protected override void Awake()
	{
		base.Awake();
		ppsText = SafeInstantiateText(ModuleText, transform, "pps 00 ms 00");
		laserSpeedText = SafeInstantiateText(ModuleText, transform, "rpm 000");
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

		laserSpeedText.text = string.Format("rpm {0:000}", laser.GetAveragedLaserRPM());

	}
}