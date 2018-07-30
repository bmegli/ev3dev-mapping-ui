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

using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class ModuleNetwork
{
	public int port=8000;
}
	
[Serializable]
public class ModuleProperties
{
	public bool autostart=true;
	public int priority=10;
	public int creationDelayMs=0;
}

public enum ModuleState {Offline=0, Initializing=1, Online=2, Shutdown=3, Failed=4 }

// Note - this component depends on its parent in hierarchy!
// Note - settings are taken from SceneManager.Settings if they exist
// or parent otherwise (this allows using standalone scenes or menu loaded scenes)

// Common functions for all ev3dev-mapping-ui components
// Common robot configuration from parent/menu
public abstract class RobotModule : MonoBehaviour, IComparable<RobotModule>
{
	public ModuleNetwork moduleNetwork;

	protected RobotRequired robot;
	protected Network network;
	protected Replay replay;
	protected PositionHistory positionHistory;
	protected Physics physics;
	protected Limits limits;
	protected UserInput input;

	private ModuleState moduleState=ModuleState.Offline;

	public abstract string ModuleCall();
	public abstract int ModulePriority();
	public abstract bool ModuleAutostart();
	public abstract int CreationDelayMs();

	public ModuleState GetState()
	{
		return moduleState;
	}
	public void SetState(ModuleState state)
	{
		moduleState = state;
	}

	public int CompareTo(RobotModule other)
	{
		return ModulePriority().CompareTo( other.ModulePriority() );
	}
		
	protected virtual void Awake()
	{
		GameObject settings = SceneManager.Settings;

		if (!settings)
		{
			robot = SafeGetComponentInParent<RobotRequired> ().DeepCopy ();
			network = SafeGetComponentInParent<Network> ().DeepCopy ();
			replay = SafeGetComponentInParent<Replay> ().DeepCopy ();
			positionHistory = SafeGetComponentInParent<PositionHistory>();
			physics = SafeGetComponentInParent<Physics>().DeepCopy();
			limits = SafeGetComponentInParent<Limits>().DeepCopy();
			input = SafeGetComponentInParent<UserInput>().DeepCopy();
			return;
		}

		//for now only RobotRequired (Session), Replay and Network from the menu
		robot = settings.GetComponent<RobotRequired> ().DeepCopy ();
		replay = settings.GetComponent<Replay> ().DeepCopy ();
		network = settings.GetComponent<Network> ().DeepCopy ();
		/*
		positionHistory = settings.GetComponent<PositionHistory>();
		physics = settings.GetComponent<Physics>().DeepCopy();
		limits = settings.GetComponent<Limits>().DeepCopy();
		input = settings.GetComponent<UserInput>().DeepCopy();
		*/
		positionHistory = SafeGetComponentInParent<PositionHistory>();
		physics = SafeGetComponentInParent<Physics>().DeepCopy();
		limits = SafeGetComponentInParent<Limits>().DeepCopy();
		input = SafeGetComponentInParent<UserInput>().DeepCopy();

	}

	protected T SafeInstantiate<T>(T original) where T : MonoBehaviour
	{
		if (original == null)
		{
			Debug.LogError ("Expected to find prefab of type " + typeof(T) + " but it was not set");
			return default(T);
		}
		return Instantiate<T>(original);
	}

	protected T SafeGetComponentInParent<T>() where T : MonoBehaviour
	{
		T component = GetComponentInParent<T> ();

		if (component == null)
			Debug.LogError ("Expected to find component of type " + typeof(T) + " but found none");

		return component;
	}

}
