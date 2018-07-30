/*
 * Copyright (C) 2016-2018 Bartosz Meglicki <meglickib@gmail.com>
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
 * This class provides strongly typed access to game objects
 * that should be available on robot scene.
 *
 * (e.g. DynamicObjects list, UI Panel, Settings from MainMenu scene)
*/

using UnityEngine;
using System.Collections.Generic;

//SceneManager just for testing, rewrite needed
public class SceneManager : MonoBehaviour
{
	private Transform dynamicObjects;
	private GameObject uiCanvas;
	private GameObject robotsPanel;
	private GameObject settings;

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

	public static GameObject Settings
	{
		get{ return Instance.settings;}
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
		robotsPanel = GameObject.Find("RobotsPanel");
		settings = GameObject.Find ("Settings");
	}

	public void ToggleShowUI()
	{
		uiCanvas.SetActive(!uiCanvas.activeSelf);
	}
}
