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
using System.Collections.Generic;

//SceneManager just for testing, rewrite needed
public class SceneManager : MonoBehaviour
{
	private Transform dynamicObjects;
	private GameObject uiCanvas;
//	private GameObject robot;
//	private Control robotControl;
	private GameObject robotsPanel;

	public static SceneManager Instance { get; private set; }

	public static Transform DynamicObjects
	{
		get {return Instance.dynamicObjects;}
	}

	public static GameObject RobotsPanel
	{
		get{return Instance.robotsPanel; }
	}

	public static Transform UICanvas
	{
		get{ return Instance.uiCanvas.transform; }
	}

	private void Destroy()
	{
		Instance = null;
	}

	private void Awake()
	{
		if (Instance != null)
		{
			Debug.LogError("More than one SceneManager instance?");
			return;
		}
		Instance = this;
		dynamicObjects = new GameObject("DynamicObjects").transform;
		uiCanvas = GameObject.Find("UICanvas");
//		robot = GameObject.FindGameObjectWithTag ("Player");
//		robotControl = robot.GetComponent<Control> ();
		robotsPanel = GameObject.Find("RobotsPanel");
	}

	public void SaveMaps()
	{
		Laser[] lasers=FindObjectsOfType<Laser>();
		foreach (Laser l in lasers)
			l.SaveMap();
	}

	private bool replayStarted=false;
	public void StartReplay()
	{
		if (replayStarted)
			return;
		replayStarted = true;

		ReplayableServer[] servers=FindObjectsOfType<ReplayableServer>();
		ReplayableClient[] clients=FindObjectsOfType<ReplayableClient>();

		foreach (ReplayableServer server in servers)
			server.StartReplay();
		foreach (ReplayableClient client in clients)
			client.StartReplay();


	}
	public void ToggleShowUI()
	{
		uiCanvas.SetActive(!uiCanvas.activeSelf);
	}
		
	/*
	public void EnableModules()
	{
		robotControl.EnableModules();
	}
	*/
	/*
	public void DisableModules()
	{		
		robotControl.DisableModules();
	}
	*/
}
	