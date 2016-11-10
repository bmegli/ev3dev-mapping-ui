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
using System;

public class ModuleUI : MonoBehaviour, IComparable<ModuleUI>
{
	public Transform UiTransform;
	public Text ModuleName;
	public Text ModuleText;
	public Toggle ModuleStateToggle;

	protected Transform uiTransform;
	private Text moduleName;
	private Text moduleState;
	private Toggle moduleStateToggle;
	private Control control;

	protected RobotModule module;

	protected virtual void Awake()
	{
		uiTransform = Instantiate<Transform>(UiTransform);
		moduleName = SafeInstantiateText(ModuleName, uiTransform, "module");
		moduleStateToggle = SafeInstantiate<Toggle>(ModuleStateToggle, uiTransform);
		moduleStateToggle.onValueChanged.AddListener(SetEnable);
		moduleState = moduleStateToggle.GetComponentInChildren<Text>();
		moduleState.text = ModuleState.Offline.ToString().ToLower();
		control = transform.parent.GetComponentInChildren<Control> ();
		module = GetComponent<RobotModule> ();
	}

	public void SetUIParent(Transform rectTranform)
	{
		uiTransform.SetParent(rectTranform, false);
	}

	protected virtual void Start ()
	{
		if (module == null)
		{
			print("Module not set!");
			enabled = false;
			return;
		}
		moduleName.text = module.name;
	}

	protected virtual void Update ()
	{
		moduleState.text = module.GetState().ToString().ToLower();

		if (module.GetState() == ModuleState.Online)
			moduleStateToggle.Set(true, false);
		else
			moduleStateToggle.Set(false, false);
	}

	public void SetEnable(bool enable)
	{
		control.EnableDisableModule(module.name, enable);
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

	public int CompareTo(ModuleUI other)
	{
		return module.CompareTo(other.module);
	}


}
