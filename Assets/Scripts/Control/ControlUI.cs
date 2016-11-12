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

public class ControlUI : MonoBehaviour
{
	public Transform UiTransform;
	public Transform ModulesPanel;
	public Text ModuleName;
	public Text ModuleText;
	public Toggle ModuleStateToggle;
	public Button ModuleButton;

	protected Transform uiTransform;
	private Transform modulesPanel;

	private Text moduleName;
	private Text moduleState;
	private Toggle moduleStateToggle;
	private Button replayButton;
	private Button saveMapsButton;

	private Control control;

	private bool replayStarted = false;

	protected virtual void Awake()
	{
		modulesPanel = Instantiate<Transform>(ModulesPanel);
		uiTransform = Instantiate<Transform>(UiTransform);

		moduleName = SafeInstantiateText(ModuleName, uiTransform, "module");
		moduleStateToggle = SafeInstantiate<Toggle>(ModuleStateToggle, uiTransform);
		moduleStateToggle.onValueChanged.AddListener(SetEnable);
		moduleState = moduleStateToggle.GetComponentInChildren<Text>();
		moduleState.text = ModuleState.Offline.ToString().ToLower();

		//to be generalized later
		if (transform.parent.GetComponentsInChildren<Laser>().Length != 0)
		{
			saveMapsButton = SafeInstantiate<Button>(ModuleButton, uiTransform);
			saveMapsButton.onClick.AddListener(OnSaveMapsButtonClicked);
			saveMapsButton.GetComponentInChildren<Text>().text = "save maps";
		}

		if (GetComponentInParent<Replay>().ReplayAny())
		{
			replayButton = SafeInstantiate<Button>(ModuleButton, uiTransform);
			replayButton.onClick.AddListener(OnReplayButtonClicked);
			replayButton.GetComponentInChildren<Text>().text = "replay";
		}
	}
		
	protected virtual void Start ()
	{
		control = GetComponent<Control>();
		moduleName.text = transform.parent.name;

		uiTransform.SetParent(modulesPanel, false);

		ModuleUI[] modulesUIs = transform.parent.GetComponentsInChildren<ModuleUI>();
		System.Array.Sort(modulesUIs);

		foreach (ModuleUI module in modulesUIs)
			module.SetUIParent(modulesPanel);
		
		modulesPanel.SetParent(SceneManager.RobotsPanel.transform, false);
	}

	protected virtual void Update ()
	{
		moduleState.text = control.GetState().ToString().ToLower();

		if (control.GetState() == ModuleState.Online)
			moduleStateToggle.Set(true, false);
		else
			moduleStateToggle.Set(false, false);
	}

	public void OnReplayButtonClicked()
	{
		if (replayStarted)
		{
			print(name + " - unable to start replay (already started)");
			return;
		}
		replayStarted = true;

		ReplayableServer[] servers=transform.parent.GetComponentsInChildren<ReplayableServer>();
		ReplayableClient[] clients=transform.parent.GetComponentsInChildren<ReplayableClient>();

		foreach (ReplayableServer server in servers)
			server.StartReplay();
		foreach (ReplayableClient client in clients)
			client.StartReplay();
		
	}

	public void OnSaveMapsButtonClicked()
	{
		Laser[] lasers=transform.parent.GetComponentsInChildren<Laser>();
		foreach (Laser l in lasers)
			l.SaveMap();
	}


	public void SetEnable(bool enable)
	{
		control.EnableDisableSelf(enable);
	}

	protected Text SafeInstantiateText(Text original, Transform parent, string initial_text) 
	{
		Text instantiated=SafeInstantiate<Text>(original, parent);
		instantiated.text=initial_text;
		return instantiated;
	}

	protected T SafeInstantiate<T>(T original, Transform parent) where T : MonoBehaviour
	{
		if (original == null)
		{
			Debug.LogError ("Expected to find prefab of type " + typeof(T) + " but it was not set");
			return default(T);
		}
		T instantiated=Instantiate<T>(original);
		instantiated.transform.SetParent(parent, false);
		return instantiated;
	}

	protected GameObject SafeInstantiateGameObject(GameObject original, Transform parent)
	{
		if (original == null)
		{
			Debug.LogError ("Expected to find prefab of type GameObject but it was not set");
			return null;
		}
		GameObject instantiated=Instantiate<GameObject>(original);
		instantiated.transform.SetParent(parent, false);
		return instantiated;
	}




}
