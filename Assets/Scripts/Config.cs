using UnityEngine;
using System.Collections;

public class Config
{
	public const string MAPS_DIRECTORY="PLY/";
	public const string DUMPS_DIRECTORY="UDP/";

	public static string DumpPath(string dump_file)
	{
		return DUMPS_DIRECTORY + dump_file;
	}
	public static string MapPath(string map_file)
	{
		return MAPS_DIRECTORY + map_file;
	}


}
