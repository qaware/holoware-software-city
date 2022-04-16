using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace SoftwareCities.holoware.lsm
{
    /// <summary>
    /// Detects cycles in a graph using the Tarjan algorithm.
    /// </summary>
    public class CycleDetector
    {
        /// <summary>
        /// The detected cycles for a given dependency graph 
        /// </summary>
        public HashSet<List<string>> cycles { get; private set; }

        private TarjanGraph tarjanGraph;


        /// <summary>
        /// Generates a cycle detector for the given dependency graph. 
        /// </summary>
        /// <param name="dependencyGraph">the dependency graph to be analyzed</param>
        /// <returns>the generated cycle detector</returns>
        public static CycleDetector ForGraph(DependencyGraph dependencyGraph)
        {
            // This function doesn't show dependencies between packages, they get created in LsmNode.BuildDependencies
            CycleDetector cycleDetector = new CycleDetector();
            cycleDetector.tarjanGraph = new TarjanGraph(dependencyGraph);
            cycleDetector.cycles = cycleDetector.tarjanGraph.GetScComponents();

            return cycleDetector;
        }

        /// <summary>
        /// Detect cycles within the given nodes. Needed to remove cyclic dependencies before building the LSM. 
        /// </summary>
        /// <param name="nodes">to detect cycles in</param>
        /// <returns>the cycle detector holding the cycles</returns>
        public static List<KeyValuePair<string, string>> RemoveCyclicDependencies(List<LsmNode> nodes)
        {
            CycleDetector cycleDetector = new CycleDetector();
            List<KeyValuePair<string, string>> removedDeps = new List<KeyValuePair<string, string>>();
            TarjanGraph tarjan = new TarjanGraph(nodes);
            cycleDetector.cycles = tarjan.GetScComponents();
            if (cycleDetector.cycles.Count <= 0) return null;

            foreach (List<KeyValuePair<LsmNode, LsmNode>> removalCandidates in cycleDetector.cycles.Select(cycle =>
                         CycleRemovalFirstHeuristics(nodes, cycle)))
            {
                if (removalCandidates.Count == 0)
                {
                    Debug.Log("No candidates found to remove a dependency!");
                }
                else if (removalCandidates.Count == 1)
                {
                    RemoveDependency(removalCandidates.First().Key, removalCandidates.First().Value);
                    removedDeps.Add(new KeyValuePair<string, string>(removalCandidates.First().Key.GetFullName(),
                        removalCandidates.First().Value.GetFullName()));
                }
                else
                {
                    // Second heuristics: Remove the dependency of the node with the least outgoing edges 
                    (LsmNode key, LsmNode value) = removalCandidates.Aggregate((l, r) =>
                        l.Key.dependencies.Count < r.Key.dependencies.Count ? l : r);
                    RemoveDependency(key, value);
                    removedDeps.Add(new KeyValuePair<string, string>(key.GetFullName(), value.GetFullName()));
                }
            }

            // recursive call to check if there are some cycles left after removing one dependency
            List<KeyValuePair<string, string>> removeCyclicDependencies = RemoveCyclicDependencies(nodes);
            if (removeCyclicDependencies != null)
            {
                removedDeps.AddRange(removeCyclicDependencies);
            }

            return removedDeps;
        }

        /// <summary>
        /// Remove the dependency between the two nodes. 
        /// </summary>
        /// <param name="from">from node</param>
        /// <param name="to">to node</param>
        private static void RemoveDependency(LsmNode from, LsmNode to)
        {
            Dictionary<string, Dependencies> fromDependencies = from.dependencies;
            from.dependencies = (from kv in fromDependencies
                where !kv.Key.Equals(to.GetFullName())
                select kv).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <summary>
        /// First heuristics: Remove edge with the least weight. 
        /// </summary>
        /// <param name="parent">holding the child nodes containing a cycle</param>
        /// <param name="cycle">the actual cycle</param>
        /// <returns>the removal candidates</returns>
        private static List<KeyValuePair<LsmNode, LsmNode>> CycleRemovalFirstHeuristics(List<LsmNode> nodes,
            IReadOnlyList<string> cycle)
        {
            List<KeyValuePair<LsmNode, LsmNode>> removalCandidates = new List<KeyValuePair<LsmNode, LsmNode>>();

            int minDepCount = int.MaxValue;
            for (int j = 0; j < cycle.Count; j++)
            {
                LsmNode from = nodes.Find(node => node.GetFullName().Equals(cycle[j]));
                LsmNode to = nodes.Find(node =>
                    node.GetFullName().Equals(cycle[j + 1 < cycle.Count ? j + 1 : 0]));
                if (from == null || to == null || from.Equals(to)) continue;
                int depCount = from.Depends(to);

                // How can depCount be 0?
                // The "cycle" returned by Tarjan algorithm is not always a cycle, just a strongly connected component.
                // Therefore, some edges between cycle[j] and cycle[j+1] don't have to exist.
                // Still, the edges that we find are part of some cycle.

                if (depCount < minDepCount && depCount > 0)
                {
                    removalCandidates = new List<KeyValuePair<LsmNode, LsmNode>>
                    {
                        new KeyValuePair<LsmNode, LsmNode>(from, to)
                    };
                    minDepCount = depCount;
                }
                else if (depCount == minDepCount)
                {
                    removalCandidates.Add(new KeyValuePair<LsmNode, LsmNode>(from, to));
                }
            }

            return removalCandidates;
        }


        /// <summary>
        /// Indicates whether the given class is cyclic 
        /// </summary>
        /// <param name="name">the name of the class</param>
        /// <returns>If the given class is in a cycle or not</returns>
        public bool IsCyclic(string name)
        {
            return cycles.Any(cycle => cycle.Contains(name));
        }

        /// <summary>
        /// Returns the cycles in which the passed class name is involved 
        /// </summary>
        /// <param name="name">the name of the class</param>
        /// <returns>A list of the matching cycles</returns>
        public HashSet<List<string>> GetCyclesFor(string name)
        {
            HashSet<List<string>> cyclesForClass = new HashSet<List<string>>();
            foreach (List<string> cycle in cycles.Where(cycle => cycle.Contains(name)))
            {
                cyclesForClass.Add(cycle);
            }

            return cyclesForClass;
        }
    }

    /// <summary>
    /// This is necessary for older .NET versions
    /// </summary>
    static class KvpExtensions
    {
        public static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> kvp,
            out TKey key,
            out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}