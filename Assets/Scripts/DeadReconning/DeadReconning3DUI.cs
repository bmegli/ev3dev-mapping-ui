/*
 * Copyright (C) 2018 Bartosz Meglicki <meglickib@gmail.com>
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

public class DeadReconning3DUI : ModuleUI
{
	private Text ppsText;
	private Text positionText;
	private Text rotationText;

	private DeadReconning3D deadReconning;

	protected override void Awake()
	{
		base.Awake();
		ppsText = SafeInstantiateText(ModuleText, uiTransform, "pps 00 ms 00");
		positionText = SafeInstantiateText(ModuleText, uiTransform, "x +0.00 y +00.0 z +00.0");
		rotationText = SafeInstantiateText(ModuleText, uiTransform, "x 000.0 y 000.0 z 000.0");
	}

	protected override void Start ()
	{
		base.Start();
		deadReconning = module as DeadReconning3D;
	}

	protected override void Update ()
	{
		base.Update();
		Vector3 pos = deadReconning.GetPosition();
		Vector3 rot = deadReconning.GetRotation();

		float avgPacketMs = deadReconning.GetAveragedPacketTimeMs();

		if (avgPacketMs != 0)
			ppsText.text = string.Format("pps {0:00} ms {1:00}", 1000.0f / avgPacketMs, avgPacketMs);
		positionText.text = string.Format("p {0:+0.00;-0.00} {1:+0.00;-0.00} {2:+0.00;-0.00}", pos.x, pos.y, pos.z);
		rotationText.text= string.Format("r {0:+##0.0;-##0.0} {1:+##0.0;-##0.0} {2:+##0.0;-##0.0}", rot.x, rot.y, rot.z);
	}
}
