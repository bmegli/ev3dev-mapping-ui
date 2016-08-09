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
using UnityEngine.UI;

public class OdometryUI : ModuleUI
{
	private Text ppsText;
	private Text positionText;
	private Text headingText;

	private Odometry odometry;

	protected override void Awake()
	{
		base.Awake();
		ppsText = SafeInstantiateText(ModuleText, transform, "pps 00 ms 00");
		positionText = SafeInstantiateText(ModuleText, transform, "x +0.00 y +00.0");
		headingText = SafeInstantiateText(ModuleText, transform, "head 000.0");
	}

	protected override void Start ()
	{
		base.Start();
		odometry = module as Odometry;
	}

	protected override void Update ()
	{
		base.Update();
		Vector3 pos = odometry.GetPosition();
		float avgPacketMs = odometry.GetAveragedPacketTimeMs();

		if (avgPacketMs != 0)
			ppsText.text = string.Format("pps {0:00} ms {1:00}", 1000.0f / avgPacketMs, avgPacketMs);
		positionText.text = string.Format("x {0:+0.00;-0.00} y {1:+0.00;-0.00}", pos.x, pos.z);
		headingText.text= string.Format("head {0:+##0.0;-##0.0}", odometry.GetHeading());
	}
}
