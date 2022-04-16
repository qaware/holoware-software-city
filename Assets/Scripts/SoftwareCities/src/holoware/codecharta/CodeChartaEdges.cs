using System;
using System.Collections.Generic;

namespace SoftwareCities.src.holoware.codecharta
{
    [Serializable]
    public class CodeChartaEdges
    {

        public List<Edge> edges; 
        
        [Serializable]
        public class Edge
        {
            public string fromNodeName;
            public string toNodeName; 
        }
    }
}