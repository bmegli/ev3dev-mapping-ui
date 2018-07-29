﻿/*
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

namespace Ev3devMapping
{
	public class Config
	{
		#if UNITY_STANDALONE
		public static string PREFIX =  "";
		#else
		public static string PREFIX = Application.persistentDataPath + "/";
		#endif
		public const string MAPS_DIRECTORY="PLY";
		public const string DUMPS_DIRECTORY="UDP";
		public const string SNAPSHOTS_DIRECTORY="SNAPSHOT";

		public static string DumpPath(string dump_dir, string dump_subdir)
		{
			return PREFIX + DUMPS_DIRECTORY + "/" + dump_dir + "/" + dump_subdir;
		}

		public static string DumpPath(string dump_dir, string dump_subdir, string dump_file)
		{
			return PREFIX + DUMPS_DIRECTORY + "/" + dump_dir + "/" + dump_subdir + "/" + dump_file + ".bin";
		}

		public static string MapPath(string map_dir, string map_subdir)
		{
			return PREFIX + MAPS_DIRECTORY + "/" + map_dir + "/" + map_subdir;
		}
		public static string MapPath(string map_dir, string map_subdir, string map_file)
		{
			return PREFIX + MAPS_DIRECTORY + "/" + map_dir + "/" + map_subdir + "/" + map_file + ".ply";
		}

		public static string SnapshotPath(string snap_dir, string snap_subdir)
		{
			return PREFIX + SNAPSHOTS_DIRECTORY + "/" + snap_dir + "/" + snap_subdir;
		}

		public static string SnapshotPath(string snap_dir, string snap_subdir, string snap_file)
		{
			return PREFIX + SNAPSHOTS_DIRECTORY + "/" + snap_dir + "/" + snap_subdir + "/" + snap_file + ".csv";
		}
	}

} //namespace
