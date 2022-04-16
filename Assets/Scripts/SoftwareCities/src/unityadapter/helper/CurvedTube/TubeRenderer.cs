// code adapted from https://github.com/dajver/Rope-Unity3d-Examples/blob/master/Assets/RopeExamples/Rope4/TubeRenderer.cs

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace CurvedTubeHelper
{
	public class TubeRenderer : MonoBehaviour
	{
		public Material material;
		public int crossSegments = 12;
		public float segmentSize = 0.15f;
		public float radius;
		
		// for tracking changes without having to redraw the tubes every time
		private Material materialOld;
		private int crossSegmentsOld;
		private float segmentSizeOld;
		private float radiusOld;

		private Vector3[] crossPoints;
		private int lastCrossSegments;
		private Mesh mesh;

		public Vector3[] controlPoints = new Vector3[0];
		private Vector3[] controlPointsOld = new Vector3[0];
		private TubeVertex[] tubeVertices;

		void Start()
		{
			mesh = new Mesh();
			gameObject.AddComponent(typeof(MeshFilter));
			gameObject.AddComponent(typeof(MeshRenderer));

			// longer arcs with small segment size are not rendered correctly; shorter arcs with large segment size look bad
			segmentSize = Vector3.Distance(controlPoints[0], controlPoints[controlPoints.Length-1]) / 100;
			segmentSize = Math.Min(1, Math.Max(0.10f, segmentSize));
			
			Vector3[] smoothedPoints = LineSmoother.SmoothLine(controlPoints, segmentSize); 
			SetTubePoints(smoothedPoints, radius);

			UpdateMesh();
		}

		private void UpdateMesh()
		{
			if (tubeVertices.Length <= 1)
			{
				GetComponent<Renderer>().enabled = false;
				return;
			}

			GetComponent<Renderer>().enabled = true;
			
			MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
			mr.material = material;
			mr.shadowCastingMode = ShadowCastingMode.Off;
			
			if (crossSegments != lastCrossSegments)
			{
				crossPoints = new Vector3[crossSegments];
				float theta = 2 * Mathf.PI / crossSegments;
				for (int c = 0; c < crossSegments; c++)
				{
					crossPoints[c] = new Vector3(Mathf.Cos(theta * c), Mathf.Sin(theta * c), 0);
				}

				lastCrossSegments = crossSegments;
			}
			
			Vector3[] meshVertices = new Vector3[tubeVertices.Length * crossSegments];
			Vector2[] uvs = new Vector2[tubeVertices.Length * crossSegments];
			int[] tris = new int[tubeVertices.Length * crossSegments * 6];
			int[] lastVertices = new int[crossSegments];
			int[] theseVertices = new int[crossSegments];
			Quaternion rotation = Quaternion.identity;
			for (int p = 0; p < tubeVertices.Length; p++)
			{
				if (p < tubeVertices.Length - 1)
					rotation = Quaternion.FromToRotation(Vector3.forward, tubeVertices[p + 1].point - tubeVertices[p].point);

				for (int c = 0; c < crossSegments; c++)
				{
					int vertexIndex = p * crossSegments + c;
					meshVertices[vertexIndex] = tubeVertices[p].point + rotation * crossPoints[c] * tubeVertices[p].radius;
					uvs[vertexIndex] = new Vector2((float) c / (float) crossSegments,
						(float) p / (float) tubeVertices.Length);

					lastVertices[c] = theseVertices[c];
					theseVertices[c] = p * crossSegments + c;
				}

				//make triangles
				if (p > 0)
				{
					for (int c = 0; c < crossSegments; c++)
					{
						int start = (p * crossSegments + c) * 6;
						tris[start] = lastVertices[c];
						tris[start + 1] = lastVertices[(c + 1) % crossSegments];
						tris[start + 2] = theseVertices[c];
						tris[start + 3] = tris[start + 2];
						tris[start + 4] = tris[start + 1];
						tris[start + 5] = theseVertices[(c + 1) % crossSegments];
					}
				}
			}
			
			//Clear mesh for new build  (jf)   
			mesh.Clear();
			mesh.vertices = meshVertices;
			mesh.triangles = tris;
			mesh.RecalculateNormals();
			mesh.uv = uvs;

			(GetComponent(typeof(MeshFilter)) as MeshFilter).mesh = mesh;
		}

		/// <summary>
		/// Collect control points positions and update the tube renderer.
		/// </summary>
		public void Update()
		{
			GetControlPoints();
			
			if (ControlPointsChanged() | ParametersChanged())
			{
				Vector3[] smoothedPoints = LineSmoother.SmoothLine(controlPoints, segmentSize); 
				SetTubePoints(smoothedPoints, radius);
				UpdateMesh();
			}
		}

		private bool ParametersChanged()
		{
			crossSegments = Math.Max(3, crossSegments);
			segmentSize = Math.Max(0.01f, segmentSize);
			radius = Math.Max(0.01f, radius);
			
			bool changed = !(materialOld == material &&
			               crossSegmentsOld == crossSegments &&
			               segmentSizeOld.Equals(segmentSize) &&
			               radiusOld.Equals(radius));
			
			materialOld = material;
			crossSegmentsOld = crossSegments;
			segmentSizeOld = segmentSize;
			radiusOld = radius;
			
			return changed;
		}

		private bool ControlPointsChanged()
		{
			//check if control points have moved
			bool moved = (controlPointsOld.Length != controlPoints.Length);
			for (int i = 0; i < controlPoints.Length; i++)
			{
				if (!moved && controlPoints[i] != controlPointsOld[i])
				{
					moved = true;
				}
			}
			
			controlPointsOld = controlPoints;

			return moved;
		}

		private void GetControlPoints()
		{
			// find curved points in children
			// scan only the first hierarchy level to allow nested curved lines (like modelling a tree or a coral)
			List<Vector3> curvedLinePoints = new List<Vector3>();
			for(int i=0; i<transform.childCount;i++)
			{
				CurvedLinePoint childPoint = transform.GetChild(i).GetComponent<CurvedLinePoint>();
				if(childPoint!=null)
				{
					curvedLinePoints.Add(childPoint.transform.localPosition);
				}
			}
			controlPoints = curvedLinePoints.ToArray();
		}



		//sets all the points to points of a Vector3 array, as well as capping the ends.
		public void SetTubePoints(Vector3[] smoothedPoints, float radius)
		{
			if (smoothedPoints.Length < 2) return;
			tubeVertices = new TubeVertex[smoothedPoints.Length + 2];

			Vector3 v0offset = (smoothedPoints[0] - smoothedPoints[1]) * 0.01f;
			tubeVertices[0] = new TubeVertex(v0offset + smoothedPoints[0], 0.0f);
			Vector3 v1offset = (smoothedPoints[smoothedPoints.Length - 1] - smoothedPoints[smoothedPoints.Length - 2]) * 0.01f;
			tubeVertices[tubeVertices.Length - 1] = new TubeVertex(v1offset + smoothedPoints[smoothedPoints.Length - 1], 0.0f);

			for (int p = 0; p < smoothedPoints.Length; p++)
			{
				tubeVertices[p + 1] = new TubeVertex(smoothedPoints[p], radius);
			}
		}
	}
}