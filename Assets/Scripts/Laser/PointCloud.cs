// Inital code based on:
// http://www.kamend.com/2014/05/rendering-a-point-cloud-inside-unity/

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

		Vector3[] mesh_vertices = new Vector3[numberOfPoints];
		Color[] mesh_colors = new Color[numberOfPoints];
		int[] mesh_indices = new int[numberOfPoints];

		for (int i = 0; i < mesh_vertices.Length; ++i)
		{
			mesh_indices[i] = i;
			mesh_colors[i] = color; 
		//	mesh_vertices [i] = new Vector3 (Mathf.Cos (i * Constants.DEG2RAD), 0, Mathf.Sin (Constants.DEG2RAD * i));
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

	public int AssignVertices(Vector3[] data, int i_from, int len, bool[] is_invalid)
	{
		Vector3[] vertices = mesh.vertices;
		int assigned = 0;
		int unassigned = UnassignedCount();

		for (int i = i_from; i < i_from+len && assigned < unassigned; ++i)
		{
			int ind = i % data.Length;
			if (is_invalid[ind])
				continue;
			vertices[assignedPoints + assigned] = data[ind];
			++assigned;
		}

		mesh.vertices = vertices;

		//if this happens to be time consuming we can keep the bounds and update if they change while adding
		mesh.RecalculateBounds(); 
		assignedPoints += assigned;

		return assigned;
	}
	public void SaveToPlyPolygonFileFormat(BinaryWriter bw)
	{
		PLYPolygonFileFormat.EmitVertices(bw, mesh.vertices, AssignedCount());
	}
}
