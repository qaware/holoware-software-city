using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoftwareCities.holoware.lsm
{
    /// <summary>
    /// Detects cycles in a graph using the Tarjan algorithm. 
    /// <see href="https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm>"/>
    /// <see href="https://rosettacode.org/wiki/Tarjan"/> 
    /// </summary>
    public class TarjanGraph
    {
        /// <summary>
        /// the tarjan graph nodes for the given graph
        /// </summary>
        private readonly Dictionary<string, GraphNode> graphNodes = new Dictionary<string, GraphNode>();

        /// <summary>
        /// the dependencies 
        /// </summary>
        private readonly Dictionary<GraphNode, HashSet<GraphNode>>
            dependencies = new Dictionary<GraphNode, HashSet<GraphNode>>();

        /// <summary>
        /// The detected cycles for a given dependency graph 
        /// </summary>
        private HashSet<List<string>> scComponents = null;

        public HashSet<List<string>> GetScComponents()
        {
            if (scComponents == null)
            {
                FindScCs();
            }

            return scComponents;
        }


        /// <summary>
        /// Generates a Tarjan graph with the internal GraphNode class.
        /// We only need this for cycle detection so we don't put the index stuff in the original dependency graph. 
        /// </summary>
        /// <param name="nodes">the nodes to analyze for cycles</param>
        /// <param name="detector">the cycle detector to be generated for this graph</param>
        public TarjanGraph(List<LsmNode> nodes)
        {
            // first add all nodes to the graph
            foreach (LsmNode node in nodes)
            {
                string name = node.GetFullName();
                graphNodes.Add(name, new GraphNode(name));
            }

            // then add all dependencies, using the nodes we have in the graph
            foreach (LsmNode node in nodes)
            {
                GraphNode src = graphNodes[node.GetFullName()];
                HashSet<string> namesDests = new HashSet<string>(node.dependencies.Keys
                    .Where(dep => nodes.Exists(n => n.GetFullName().Equals(dep))));
                
                HashSet<GraphNode> dests = new HashSet<GraphNode>(namesDests.Select(n => graphNodes[n]));

                dependencies.Add(src, dests);
            }
        }

        /// <summary>
        /// Generates a Tarjan graph with the internal GraphNode class.
        /// We only need this for cycle detection so we don't put the index stuff in the original dependency graph. 
        /// </summary>
        /// <param name="graph">the dependency graph</param>
        /// <param name="detector">the cycle detector to be generated for this graph</param>
        public TarjanGraph(DependencyGraph graph)
        {
            foreach ((string key, HashSet<string> value) in graph.src2dest)
            {
                if (!graphNodes.TryGetValue(key, out GraphNode src))
                {
                    src = new GraphNode(key);
                    graphNodes.Add(key, src);
                }

                foreach (string destName in value)
                {
                    if (!graphNodes.TryGetValue(destName, out GraphNode dest))
                    {
                        dest = new GraphNode(destName);
                        graphNodes.Add(destName, dest);
                    }
                }
                
                HashSet<GraphNode> dests = new HashSet<GraphNode>(value.Select(
                    n => graphNodes[n]));
                dependencies.Add(src, dests);
            }
        }

        /// <summary>
        /// Find cycles via Tarjan's strongly connected components algorithm.
        /// Strongly Connected Components with more than one node are cycles. 
        /// </summary>
        private void FindScCs()
        {
            scComponents = new HashSet<List<string>>();

            int index = 0; // number of nodes
            FastStack<GraphNode> s = new FastStack<GraphNode>();
            //Stack<GraphNode> s = new Stack<GraphNode>(); // slow !

            void StrongConnect(GraphNode v)
            {
                // Set the depth index for v to the smallest unused index
                v.Index = index;
                v.LowLink = index;

                index++;
                s.Push(v);
                // Consider successors of v
                if (dependencies.TryGetValue(v, out HashSet<GraphNode> adjSet))
                {
                    foreach (GraphNode w in adjSet)
                    {
                        if (w.Index < 0)
                        {
                            // Successor w has not yet been visited; recurse on it
                            StrongConnect(w);
                            v.LowLink = Math.Min(v.LowLink, w.LowLink);
                        }
                        else if (s.Contains(w))
                            // Successor w is in stack S and hence in the current SCC
                            v.LowLink = Math.Min(v.LowLink, w.Index);
                    }
                }

                // If v is a root node, pop the stack and generate an SCC
                if (v.LowLink == v.Index)
                {
                    List<string> cycle = new List<string>();
                    GraphNode w;
                    do
                    {
                        w = s.Pop();
                        // add to the beginning to keep the correct order
                        cycle.Insert(0, w.Name);
                    } while (!Equals(w, v));

                    // SCC with more than one component is a cycle 
                    if (cycle.Count > 1)
                    {
                        scComponents.Add(cycle);
                    }
                }
            }

            foreach (GraphNode v in graphNodes.Values.Where(v => v.Index < 0))
                StrongConnect(v);
        }

        /// <summary>
        /// Internal graph node class for Tarjan algorithm. 
        /// </summary>
        private class GraphNode : IComparable<GraphNode>
        {
            public int LowLink { get; set; }
            public int Index { get; set; }
            public string Name { get; }

            public GraphNode(string name)
            {
                Name = name;
                Index = -1;
                LowLink = 0;
            }

            public override bool Equals(object obj)
            {
                if (this == obj) return true;
                if (obj == null || GetType() != obj.GetType()) return false;
                GraphNode graphNode = (GraphNode) obj;
                return Name.Equals(graphNode.Name);
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public int CompareTo(GraphNode other)
            {
                if (other == null)
                {
                    return 1;
                }

                return Name.CompareTo(other.Name);
            }
        }
    }
}