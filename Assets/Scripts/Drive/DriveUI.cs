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

public class DriveUI : ModuleUI
{
	private Drive drive;

	public GameObject SpeedDistanceLayout;
	public InputField DistanceInputField;
	public Text DistanceUnitText;
	public InputField SpeedInputField;
	public Text SpeedUnitText;
	public Button GoButton;

	private InputField distanceInputField;
	private InputField speedInputField;
	private Button goButton;

	protected override void Awake()
	{
		base.Awake();

		GameObject speedDistanceLayout = SafeInstantiateGameObject(SpeedDistanceLayout, transform);
		distanceInputField = SafeInstantiate<InputField> (DistanceInputField, speedDistanceLayout.transform);
		SafeInstantiateText (DistanceUnitText, speedDistanceLayout.transform, "cm");
		speedInputField = SafeInstantiate<InputField> (SpeedInputField, speedDistanceLayout.transform);
		SafeInstantiateText (SpeedUnitText, speedDistanceLayout.transform, "cm/s");
		goButton = SafeInstantiate<Button> (GoButton, transform);
		goButton.onClick.AddListener (OnGoButtonClicked);
	}

	public void OnGoButtonClicked()
	{
		float distance_cm=float.Parse (distanceInputField.text);
		float speed_cm_per_s=float.Parse (speedInputField.text);
		drive.DriveAhead (distance_cm, speed_cm_per_s);
	}

	protected override void Start ()
	{
		base.Start();
		drive = module as Drive;
	}

	protected override void Update ()
	{
		base.Update();
	}
}
