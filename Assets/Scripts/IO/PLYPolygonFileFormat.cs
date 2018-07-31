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

using System.IO;
using System.Text;
using System;
using UnityEngine;

//to do - encode in big or little endian specifically!

namespace Ev3devMapping
{

public static class PLYPolygonFileFormat
{
	public static void EmitHeader(BinaryWriter bw, int vertices, string comment)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("ply").Append(Environment.NewLine);

		if(BitConverter.IsLittleEndian)
			sb.Append("format binary_little_endian 1.0").Append(Environment.NewLine);
		else
			sb.Append("format binary_big_endian 1.0").Append(Environment.NewLine);
		
		if (comment != null)
			sb.Append("comment ").Append(comment).Append(Environment.NewLine);
		sb.Append("element vertex ").Append(vertices).Append(Environment.NewLine);
		sb.Append("property float x").Append(Environment.NewLine);
		sb.Append("property float y").Append(Environment.NewLine);
		sb.Append("property float z").Append(Environment.NewLine);
		sb.Append("end_header").Append(Environment.NewLine);

		bw.Write(ASCII(sb.ToString()));
	}
	//to do - explicit big or little endian or specify in header
	public static void EmitVertices(BinaryWriter bw, Vector3[] vertices, int count)
	{
		for (int i = 0; i < count; ++i)
		{
			bw.Write(vertices[i].x);
			bw.Write(vertices[i].y);
			bw.Write(vertices[i].z);
		}
	}

	private static byte[] ASCII(string s)
	{
		return System.Text.Encoding.ASCII.GetBytes(s);
	}
}

} //namespace