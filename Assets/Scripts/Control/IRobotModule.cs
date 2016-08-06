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

	Control GetControl();
}

