using System;
using System.Collections.Generic;
using SoftwareCities.holoware.lsm;
using CurvedTubeHelper;
using EasyCurvedLine;
using UnityEngine;
using CurvedLinePoint = EasyCurvedLine.CurvedLinePoint;
using Material = SoftwareCities.figures.Material;
using Random = System.Random;
using Vector3 = UnityEngine.Vector3;

namespace SoftwareCities.unityadapter
{
    public class DependencyDrawer : MonoBehaviour
    {
        private static Random random = new Random();


        /// <summary>
        /// Draws an arc between the source and destination GameObjects.
        /// </summary>
        /// <param name="from">source GameObject</param>
        /// <param name="to">destination GameObject</param>
        /// <param name="material">Arc material</param>
        /// <param name="flat">true for using CurvedLineRenderer, false for using TubeRenderer</param>
        /// <param name="radius">thickness of the arc</param>
        /// <param name="randomOffset">true if there should be a random offset at the endpoints of the arc, false if the
        /// arc should start and end exactly in the middle of the GameObjects</param>
        /// <param name="parent">make the resulting arc a child of this GO</param>
        /// <returns>the resulting arc</returns>
        public static GameObject DrawArc(GameObject from, GameObject to, Material material = null, bool flat = false,
            float radius = 0.5f, bool randomOffset = false, GameObject parent = null, bool arrow = false)
        {
            material = material ?? Material.Default;

            Vector3[] controlPoints = GetControlPoints(from, to, randomOffset);
            GameObject arc = null;
            string name = $"{from.transform.parent.name}_{to.transform.parent.name}_";
            if (flat)
            {
                arc = DrawLine(GetStartingPoint(from), controlPoints, material, radius, name, arrow: arrow);
            }
            else
            {
                arc = DrawTube(GetStartingPoint(from), controlPoints, material, radius, name);
            }

            if (parent != null)
            {
                arc.transform.parent = parent.transform;
            }

            return arc;
        }

        /// <summary>
        /// Draws arcs from the list of pairs of objects (cycles).
        /// </summary>
        public static void DrawCycles(IEnumerable<KeyValuePair<string, string>> cycles, List<GameObject> gameObjects,
            float radius = 0.5f, GameObject parent = null)
        {
            foreach ((string key, string value) in cycles)
            {
                // parent because the actual Cuboid we want is a child of a GO with the class name
                GameObject from = gameObjects.Find(gameObject =>
                    gameObject.transform.parent.gameObject.name.Equals(key));
                GameObject to = gameObjects.Find(gameObject =>
                    gameObject.transform.parent.gameObject.name.Equals(value));
                if (from == null || to == null)
                {
                    Debug.Log($"GameObject {key} or {value} not found!");
                    continue;
                }

                DrawArc(from, to, Material.cyclicTube, radius: radius, flat: true, parent: parent);
            }
        }

        /// <summary>
        /// Creates a tube going through the control points, with the help of TubeRenderer.
        /// </summary>
        /// <returns>the resulting tube</returns>
        private static GameObject DrawTube(Vector3 startingPoint, Vector3[] controlPoints, Material material,
            float radius, string name = "Dependency arc")
        {
            GameObject arc = new GameObject(name);
            arc.transform.position = startingPoint;
            AddControlPoints(arc, controlPoints);

            TubeRenderer tubeRenderer = arc.AddComponent<TubeRenderer>();
            tubeRenderer.radius = radius;
            tubeRenderer.crossSegments = 12;
            tubeRenderer.material = Resources.Load("Materials/" + material,
                typeof(UnityEngine.Material)) as UnityEngine.Material;
            tubeRenderer.controlPoints = controlPoints;

            return arc;
        }

        /// <summary>
        /// Creates a flat line going through the control points, with the help of CurvedLineRenderer.
        /// </summary>
        /// <returns>the resulting line</returns>
        private static GameObject DrawLine(Vector3 startingPoint, Vector3[] controlPoints, Material material,
            float radius, string name = "Dependency arc", bool arrow = false)
        {
            GameObject curvedLine = new GameObject(name);
            curvedLine.transform.position = startingPoint;
            CurvedLineRenderer curvedLineRenderer = curvedLine.AddComponent<CurvedLineRenderer>();
            curvedLineRenderer.showGizmos = false;
            curvedLineRenderer.lineWidth = radius;
            curvedLineRenderer.arrow = arrow;

            AddControlPoints(curvedLine, controlPoints);

            curvedLine.GetComponent<Renderer>().material =
                Resources.Load("Materials/" + material, typeof(UnityEngine.Material)) as
                    UnityEngine.Material;
            return curvedLine;
        }

        /// <summary>
        /// Adds control point GameObjects to the parent arc GameObject
        /// </summary>
        /// <param name="arc">parent object</param>
        /// <param name="points">points to be added</param>
        private static void AddControlPoints(GameObject arc, Vector3[] points)
        {
            int i = 0;
            foreach (Vector3 point in points)
            {
                GameObject linePoint = new GameObject($"LinePoint_{i++}");
                linePoint.AddComponent<CurvedLinePoint>();
                linePoint.transform.position = point;
                linePoint.transform.SetParent(arc.transform, false);
            }
        }

        /// <summary>
        /// Computes control points which the resulting line should go through, to have a nice arc shape.
        /// </summary>
        /// <param name="from">source GameObject</param>
        /// <param name="to">destination GameObject</param>
        /// <param name="randomOffset">true if there should be a random offset for all control points, false if all
        /// lines should start and end exactly in the middle of the GameObjects</param>
        /// <returns>Control points for a line / tube</returns>
        private static Vector3[] GetControlPoints(GameObject from, GameObject to, bool randomOffset = false)
        {
            Vector3 src = GetStartingPoint(from);
            Vector3 dest = GetStartingPoint(to);

            // weight to attract middle control points to the other end
            float w = randomOffset ? 0.05f : 0.2f;

            Vector3[] offsets = {Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero};
            if (randomOffset)
            {
                float offsetX = random.Next(-40, 40) / 100F;
                float offsetZ = random.Next(-40, 40) / 100F;
                Vector3 fromBounds = from.transform.localScale / 2;
                Vector3 toBounds = to.transform.localScale / 2;
                offsets[0] = new Vector3(fromBounds.x * offsetX, 0, fromBounds.z * offsetZ);
                offsets[1] = new Vector3(fromBounds.x * -offsetX, 0, fromBounds.z * -offsetZ);
                offsets[2] = new Vector3(toBounds.x * -offsetX, 0, toBounds.z * -offsetZ);
                offsets[3] = new Vector3(toBounds.x * offsetX, 0, toBounds.z * offsetZ);
            }

            float lineHeight = Math.Abs(src.y - dest.y) + 15;
            Vector3[] controlPoints = new Vector3[4];
            controlPoints[0] = Vector3.zero;
            controlPoints[1] = new Vector3((dest.x - src.x) * w, lineHeight, (dest.z - src.z) * w);
            controlPoints[2] = new Vector3((dest.x - src.x) * (1 - w), lineHeight, (dest.z - src.z) * (1 - w));
            controlPoints[3] = dest - src;

            for (int i = 0; i < 4; i++)
            {
                controlPoints[i] += offsets[i];
            }

            return controlPoints;
        }

        /// <summary>
        /// Returns the starting point for a line, in the middle of width / length, but on top of the object (height)
        /// </summary>
        private static Vector3 GetStartingPoint(GameObject obj)
        {
            Vector3 objPos = obj.transform.position;
            Vector3 linePos = new Vector3(
                objPos.x,
                objPos.y + obj.transform.localScale.y / 2,
                objPos.z
            );
            return linePos;
        }
    }
}