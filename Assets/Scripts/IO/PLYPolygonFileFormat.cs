using System.IO;
using System.Text;
using System;
using UnityEngine;

//to do - encode in big or little endian specifically!

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
