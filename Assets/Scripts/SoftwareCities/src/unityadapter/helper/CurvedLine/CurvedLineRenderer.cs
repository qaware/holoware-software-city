// Code from https://forum.unity.com/threads/easy-curved-line-renderer-free-utility.391219/
// and https://github.com/gpvigano/EasyCurvedLine

using System.Collections.Generic;
using System.Linq;
using CurvedTubeHelper;
using SoftwareCities.unityadapter.views;
using UnityEngine;

namespace EasyCurvedLine
{
    /// <summary>
    /// Render in 3D a curved line based on its control points.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class CurvedLineRenderer : MonoBehaviour
    {
        /// <summary>
        /// Size of line segments (in meters) used to approximate the curve.
        /// </summary>
        [Tooltip("Size of line segments (in meters) used to approximate the curve")]
        public float lineSegmentSize = 0.15f;
        /// <summary>
        /// Thickness of the line (initial thickness if useCustomEndWidth is true).
        /// </summary>
        [Tooltip("Width of the line (initial width if useCustomEndWidth is true)")]
        public float lineWidth = 0.1f;
        /// <summary>
        /// Use a different thickness for the line end.
        /// </summary>
        [Tooltip("Enable this to set a custom width for the line end")]
        public bool useCustomEndWidth = false;
        /// <summary>
        /// Thickness of the line at its end point (initial thickness is lineWidth).
        /// </summary>
        [Tooltip("Custom width for the line end")]
        public float endWidth = 0.1f;
        [Header("Gizmos")]
        /// <summary>
        /// Show gizmos at control points in Unity Editor.
        /// </summary>
        [Tooltip("Show gizmos at control points.")]
        public bool showGizmos = true;
        /// <summary>
        /// Size of the gizmos of control points.
        /// </summary>
        [Tooltip("Size of the gizmos of control points.")]
        public float gizmoSize = 0.1f;
        /// <summary>
        /// Color for rendering the gizmos of control points.
        /// </summary>
        [Tooltip("Color for rendering the gizmos of control points.")]
        public Color gizmoColor = new Color(1, 0, 0, 0.5f);

        private CurvedLinePoint[] linePoints = new CurvedLinePoint[0];
        private Vector3[] linePositions = new Vector3[0];
        private Vector3[] linePositionsOld = new Vector3[0];
        private LineRenderer lineRenderer = null;
        private Material lineRendererMaterial = null;

        public bool arrow = false;

        public CurvedLinePoint[] LinePoints
        {
            get
            {
                return linePoints;
            }
        }

        /// <summary>
        /// Collect control points positions and update the line renderer.
        /// </summary>
        public void Update()
        {
            GetPoints();
            SetPointsToLine();
            UpdateMaterial();
        }


        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        private void GetPoints()
        {
            // find curved points in children
            // scan only the first hierarchy level to allow nested curved lines (like modelling a tree or a coral)
            List<CurvedLinePoint> curvedLinePoints = new List<CurvedLinePoint>();
            for(int i=0; i<transform.childCount;i++)
            {
                CurvedLinePoint childPoint = transform.GetChild(i).GetComponent<CurvedLinePoint>();
                if(childPoint!=null)
                {
                    curvedLinePoints.Add(childPoint);
                }
            }
            linePoints = curvedLinePoints.ToArray();

            //add positions
            linePositions = new Vector3[linePoints.Length];
            for (int i = 0; i < linePoints.Length; i++)
            {
                linePositions[i] = linePoints[i].transform.position;
            }
        }


        private void SetPointsToLine()
        {
            //create old positions if they dont match
            if (linePositionsOld.Length != linePositions.Length)
            {
                linePositionsOld = new Vector3[linePositions.Length];
            }

            //check if line points have moved
            bool moved = false;
            for (int i = 0; i < linePositions.Length; i++)
            {
                //compare
                if (linePositions[i] != linePositionsOld[i])
                {
                    moved = true;
                }
            }

            //update if moved
            if (moved == true)
            {
                if (lineRenderer == null)
                {
                    lineRenderer = GetComponent<LineRenderer>();
                }

                if (!arrow)
                {
                    //get smoothed values
                    Vector3[] smoothedPoints = LineSmoother.SmoothLine(linePositions, lineSegmentSize);

                    //set line settings
                    lineRenderer.positionCount = smoothedPoints.Length;
                    lineRenderer.SetPositions(smoothedPoints);
                    lineRenderer.startWidth = lineWidth;
                    lineRenderer.endWidth = useCustomEndWidth ? endWidth : lineWidth;
                }
                else
                {
                    //add a point to make the arrow more straight
                    Vector3[] newLP = new Vector3[linePositions.Length + 1];
                    for (int i = 0; i < linePositions.Length; i++)
                    {
                        newLP[i] = linePositions[i];
                    }
                    newLP[linePositions.Length] = newLP[linePositions.Length - 1];
                    newLP[linePositions.Length - 1] += new Vector3(0, 4, 0);

                    //get smoothed values
                    Vector3[] smoothedPoints = LineSmoother.SmoothLine(newLP, lineSegmentSize);

                    //find the percentage of points until the start of the arrow
                    int indexDrop = 0;
                    for (int i = 0; i < smoothedPoints.Length; i++)
                    {
                        if ((smoothedPoints[i] - newLP[linePositions.Length - 1]).magnitude < 0.001)
                        {
                            indexDrop = i;
                            break;
                        }
                    }
                    //make the arrow head points vertical
                    for (int i = indexDrop; i < smoothedPoints.Length; i++)
                    {
                        smoothedPoints[i] = new Vector3(newLP[linePositions.Length].x, smoothedPoints[i].y, newLP[linePositions.Length].z);
                    }
                    //add some margin
                    indexDrop += (smoothedPoints.Length - 1 - indexDrop) / 3;
                    float tailLength = indexDrop / (float) smoothedPoints.Length;
                    if (tailLength < 0.5)
                    {
                        tailLength = 0.96f;
                    }

                    //set line properties
                    lineRenderer.positionCount = smoothedPoints.Length;
                    lineRenderer.SetPositions(smoothedPoints);
                    lineRenderer.widthCurve = new AnimationCurve(
                        new Keyframe(0, lineWidth)
                        , new Keyframe(tailLength, lineWidth) // neck of arrow
                        , new Keyframe(tailLength, 4*lineWidth) // max width of arrow head
                        , new Keyframe(1, 0f)); // tip of arrow
                }
            }
        }


        private void OnDrawGizmosSelected()
        {
            Update();
        }


        private void OnDrawGizmos()
        {
            if (linePoints.Length == 0)
            {
                GetPoints();
            }

            //settings for gizmos
            foreach (CurvedLinePoint linePoint in linePoints)
            {
                linePoint.showGizmo = showGizmos;
                linePoint.gizmoSize = gizmoSize;
                linePoint.gizmoColor = gizmoColor;
            }
        }

        private void UpdateMaterial()
        {
            if (lineRenderer==null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }
            Material lineMaterial = lineRenderer.sharedMaterial;
            if (lineRendererMaterial != lineMaterial)
            {
                if (lineMaterial != null)
                {
                    lineRenderer.generateLightingData = !lineMaterial.shader.name.StartsWith("Unlit");
                }
                else
                {
                    lineRenderer.generateLightingData = false;
                }
            }
            lineRendererMaterial = lineMaterial;
        }
    }
}
