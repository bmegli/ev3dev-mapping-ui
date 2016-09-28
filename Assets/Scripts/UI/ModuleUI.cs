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

public class ModuleUI : MonoBehaviour
{
	public Transform UiTransform;
	public Text ModuleName;
	public Text ModuleText;
	public Toggle EnabledToggle;

	protected Transform uiTransform;
	private Text moduleName;
	private Text moduleState;
	private Toggle enabledToggle;

	protected IRobotModule module;

	protected virtual void Awake()
	{
		uiTransform = Instantiate<Transform>(UiTransform);
		moduleName = SafeInstantiateText(ModuleName, uiTransform, "module");
		moduleState = SafeInstantiateText(ModuleText, uiTransform, ModuleState.Offline.ToString().ToLower());
		enabledToggle = SafeInstantiate<Toggle>(EnabledToggle, uiTransform);
		enabledToggle.onValueChanged.AddListener(SetEnable);
	}

	protected virtual void Start ()
	{
		module = GetComponent<IRobotModule> ();
		if (module == null)
		{
			print("Module not set!");
			enabled = false;
			return;
		}
		moduleName.text = module.GetUniqueName();
		uiTransform.SetParent (SceneManager.ModulesPanel.transform, false);
	}

	protected virtual void Update ()
	{
		moduleState.text = module.GetState().ToString().ToLower();

		if (module.GetState() == ModuleState.Online)
			enabledToggle.Set(true, false);
		else
			enabledToggle.Set(false, false);
	}

	public void SetEnable(bool enable)
	{
		Control c = module.GetControl();
		if (c == null)
		{
			print("No Control component, can't change enable state");
			return;
		}
		c.EnableDisableModule(module.GetUniqueName(), enable);
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
