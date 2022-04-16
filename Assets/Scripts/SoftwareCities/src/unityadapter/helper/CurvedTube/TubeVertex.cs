using UnityEngine;

namespace CurvedTubeHelper
{
    
    public class TubeVertex
    {
        public Vector3 point = Vector3.zero;
        public float radius = 1;

        public TubeVertex(Vector3 pt, float r)
        {
            point = pt;
            radius = r;
        }
    }
}