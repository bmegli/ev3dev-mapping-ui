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

/*
 * This class is used to pass settings from MainMenu to other scenes. 
 *
 * It depends on:
 * - game object with name "Settings" in MainMenu scene
 * - START button in MainMenu calling LoadSceneWithSettings
 * - scene indexes (0 for MainMenu, 1 for loaded scene)
 * - InputFields and Dropdown in MainMenu with particular names
 * - "Settings" game object not destroyed on loading scenes
 * - SceneManager able to find "Settings" object
 * - RobotModules taking Settings from SceneManager.Settings if they exist
 *  (and from parent if not)
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent (typeof (RobotRequired))]
public class RobotSettings : MonoBehaviour
{		
	void Awake()
	{
		DontDestroyOnLoad(gameObject);
		UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Debug.Log("Loaded scene: " + scene.name);
	}

	//this function should be attached to START button in the MainMenu UI
	public void LoadSceneWithSettings(int sceneIndex)
	{
		SettingsFromUI();
		UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneIndex);
	}
		
	private void SettingsFromUI()
	{
		//for now only RobotRequired, Replay and Network configured through the menu
		RobotRequired robot=GetComponent<RobotRequired> ();
		Replay replay=GetComponent<Replay> ();
		Network network=GetComponent<Network> ();

		robot.sessionDirectory = TextFromInputField ("SessionInputField");
		replay.mode = (ReplayMode)IntFromDropdown ("ReplayDropdown");
		network.hostIp = TextFromInputField ("HostIPInputField");
		network.robotIp = TextFromInputField ("RobotIPInputField");
	}
	private string TextFromInputField(string gameObjectName)
	{
		return GameObject.Find (gameObjectName).GetComponent<InputField>().text;
	}
	private int IntFromDropdown(string gameObjectName)
	{
		return GameObject.Find (gameObjectName).GetComponent<Dropdown>().value;
	}
}
