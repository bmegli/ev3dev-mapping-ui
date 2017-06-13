/*
 * Copyright (C) 2016-2017 Bartosz Meglicki <meglickib@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 * This program is distributed "as is" WITHOUT ANY WARRANTY of any
 * kind, whether express or implied; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * Inital code based on:
 * http://www.kamend.com/2014/05/rendering-a-point-cloud-inside-unity/
 *
 *
*/

using UnityEngine;
using System.Collections;
using System.IO;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PointCloud : MonoBehaviour
{
	public Color color;
	public int numberOfPoints=360;

	public const int MAX_VERTICES = 65534;

	private Mesh mesh;
	private Vector3[] mesh_vertices;

	private int assignedPoints; 

	public int UnassignedCount()
	{
		return numberOfPoints - assignedPoints;
	}
	public int AssignedCount()
	{
		return assignedPoints;
	}

	//we have to use awake, we instantiate the object and use it right away
	void Awake()
	{
		if (numberOfPoints > MAX_VERTICES)
			numberOfPoints = MAX_VERTICES;

		assignedPoints = 0;

		mesh_vertices = new Vector3[numberOfPoints];
		Color[] mesh_colors = new Color[numberOfPoints];
		int[] mesh_indices = new int[numberOfPoints];

		for (int i = 0; i < mesh_vertices.Length; ++i)
		{
			mesh_indices[i] = i;
			mesh_colors[i] = color; 
		}

		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		mesh.vertices = mesh_vertices;
		mesh.colors = mesh_colors;
		mesh.SetIndices(mesh_indices, MeshTopology.Points, 0);
	}
	public void Start()
	{
		if (transform.parent == null)
			transform.parent = SceneManager.DynamicObjects;
	}
		
	public void SetVertices(Vector3[] data)
	{
		mesh.vertices = data;
		mesh.RecalculateBounds();
		assignedPoints = data.Length;
	}
	public void SetVertices(Vector3[] data, Color[] colors)
	{
		mesh.vertices = data;
		mesh.colors = colors;
		mesh.RecalculateBounds();
		assignedPoints = data.Length;
	}

	public int AssignVertices(Vector3[] data, int len)
	{
		Vector3[] vertices = mesh_vertices;
		Bounds bounds = mesh.bounds;
		int assigned = 0;
		int unassigned = UnassignedCount();

		//this can be simplified to copy
		for (int i = 0; i < len && assigned < unassigned; ++i)
		{
			vertices[assignedPoints + assigned] = data[i];
			bounds.Encapsulate(data[i]);
			++assigned;
		}

		mesh.vertices = vertices;

		//if this happens to be time consuming we can keep the bounds and update if they change while adding
		mesh.bounds = bounds;
		assignedPoints += assigned;

		return assigned;
	}
		
	public int AssignVertices(Vector3[] data, int i_from, int len, bool[] is_invalid)
	{
		Vector3[] vertices = mesh_vertices;
		Bounds bounds = mesh.bounds;
		int assigned = 0;
		int unassigned = UnassignedCount();

		for (int i = i_from; i < i_from+len && assigned < unassigned; ++i)
		{
			int ind = i % data.Length;
			if (is_invalid[ind])
				continue;
			vertices[assignedPoints + assigned] = data[ind];
			bounds.Encapsulate(data[ind]);
			++assigned;
		}

		mesh.vertices = vertices;

		//if this happens to be time consuming we can keep the bounds and update if they change while adding
		mesh.bounds = bounds;
		assignedPoints += assigned;

		return assigned;
	}
	public void SaveToPlyPolygonFileFormat(BinaryWriter bw)
	{
		PLYPolygonFileFormat.EmitVertices(bw, mesh_vertices, AssignedCount());
	}
}
