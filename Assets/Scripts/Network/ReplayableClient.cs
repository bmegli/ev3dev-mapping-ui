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

public abstract class ReplayableClient : RobotModule
{
	//consider adding common class for ReplayableClient and ReplayableServer with those functions
	public abstract ulong GetFirstPacketTimestampUs();
	protected abstract void StartReplay(int time_offset_us);
	public virtual void StartReplay()
	{
		StartReplay(0);
	}
}
