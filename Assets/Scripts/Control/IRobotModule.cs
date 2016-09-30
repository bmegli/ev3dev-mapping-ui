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

using System;

[Serializable]
public class ModuleProperties
{
	public bool autostart=true;
	public int priority=10;
	public int creationDelayMs=0;
}

public enum ModuleState {Offline=0, Initializing=1, Online=2, Shutdown=3, Failed=4 }

public interface IRobotModule : IComparable<IRobotModule>
{
	string ModuleCall();
	string GetUniqueName();
	int ModulePriority();
	bool ModuleAutostart();
	int CreationDelayMs();

	ModuleState GetState();
	void SetState(ModuleState state);
}

