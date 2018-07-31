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

namespace Ev3devMapping
{

public static class Constants
{
	public const float DEG2RAD = Mathf.PI / 180f;
	public const float RAD2DEG = 180.0f / Mathf.PI;
	public const float MM_IN_M=1000.0f;
	public const float BETA=82.0f;
	public const float B=25.0f;
	public const string MAPS_DIRECTORY="maps";
	public const float ExponentialSmoothingAlpha=0.02f;
}

} //namespace
