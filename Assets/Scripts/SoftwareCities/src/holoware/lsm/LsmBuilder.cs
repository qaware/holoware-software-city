using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoftwareCities.holoware.lsm
{
    public class LsmBuilder
    {
        private DependencyGraph graph;
        public LsmNode RootNode;

        public readonly List<KeyValuePair<string, string>> edgesWrongDirection = new List<KeyValuePair<string, string>>();

        private LsmBuilder(DependencyGraph dgraph)
        {
            graph = dgraph;
            RootNode = new LsmPackage("root", "root", null);
        }

        public static LsmBuilder LsmFromGraph(DependencyGraph graph)
        {
            LsmBuilder result = new LsmBuilder(graph);
            result.RootNode = result.Construct();
            return result;
        }

        /// <summary>
        /// The LsmFromGraph method constructs the LSM out of the dependency graph.
        /// </summary>
        /// <param fullQualifiedName="dependencies">The graph.</param>
        /// <returns>The root node.</returns>
        private LsmNode Construct()
        {
            // build the structure (packages contains packages or classes) of the LSM
            HashSet<string> sources = graph.GetSources();
            foreach (string source in sources)
            {
                AddElement(RootNode, source);
                HashSet<string> targets = graph.GetTargets(source);
                foreach (string target in targets)
                {
                    AddElement(RootNode, target);
                }
            }

            // Construct Dependencies
            ConstructDeps(RootNode);

            // setup the levels for later Layout
            Levelize(RootNode);
            return RootNode;
        }

        /// <summary>
        /// Build the relevant dependencies for the later LSM levelizing. 
        /// </summary>
        /// <param name="root">The root node.</param>
        private void ConstructDeps(LsmNode root)
        {
            root.BuildDependencies(graph);
        }

        /// <summary>
        /// Adds an element (class or package) to the LSM graph.
        /// </summary>
        /// <param fullQualifiedName="parent">The parent node.</param>
        /// <param fullQualifiedName="fullQualifiedName">The full qualified fullQualifiedName.</param>
        private void AddElement(LsmNode parent, String fullQualifiedName)
        {
            string[] names = fullQualifiedName.Split('.');
            string name = ""; // todo: calculate from parent (Add parent to LsmNode)
            for (int i = 0; i < names.Length; i++)
            {
                string s = names[i];

                LsmNode element;
                // assumes the last split in a dotted String is a class !
                if (i == names.Length - 1)
                {
                    name += "." + s; // assuming a class is in a package
                    element = new LsmClass(s, name, parent);
                }
                else
                {
                    if (name.Length == 0)
                    {
                        name = s;
                    }
                    else
                    {
                        name = name + "." + s;
                    }

                    element = new LsmPackage(s, name, parent);
                }

                if (!parent.children.Contains(element))
                {
                    parent.AddChild(element);
                    parent = element;
                }
                else
                {
                    parent = parent.children[parent.children.IndexOf(element)];
                }
            }
        }

        /// <summary>
        /// The LSM levelize algorithm traverses each node and sets the level appropriate to the dependencies.
        /// </summary>
        /// <param name="root">The root node</param>
        private void Levelize(LsmNode root)
        {
            // if the node has only one child, we're done
            if (root.children.Count == 1)
            {
                Levelize(root.children[0]);
                return;
            }

            if (root.children.Count <= 1)
            {
                return;
            }
            List<KeyValuePair<string, string>> removedDeps = CycleDetector.RemoveCyclicDependencies(root.children);

            // calculate and set level for all children
            foreach (LsmNode child in root.children)
            {
                SetPathDepth(new FastStack<LsmNode>(), child);
            }


            // recursive descent for all children
            foreach (LsmNode child in root.children)
            {
                Levelize(child);
            }

            // now that we have the levels, we can check whether the removed edges really have a wrong direction
            // e.g. A->B->A, A->C->B->A: if we remove A->B from the first cycle,
            // we still have to remove B->A from the second one, and one of them was actually correct
            if (removedDeps != null)
            {
                edgesWrongDirection.AddRange(removedDeps.Where(dep =>
                    root.GetByName(dep.Key).GetLevel() <= root.GetByName(dep.Value).GetLevel()));
            }
        }

        /// <summary>
        /// Sets the node level as the length of maximal path of dependencies.
        /// The maximal length of a path is +1 larger than the maximal level of a neighbor (dependency).
        /// This function first recursively finds the levels of neighbors, and then sets the level of the desired node.
        /// O(m+n)
        /// </summary>
        /// <param name="stack">The stack to store the current node path</param>
        /// <param name="node">The current node (not on the stack)</param>
        /// <returns></returns>
        private static void SetPathDepth(FastStack<LsmNode> stack, LsmNode node)
        {
            if (node.GetLevel() > 0) return;
            
            if (stack.Contains(node)) // cycle
            {
                Debug.Log("Cycle found when cyclic edges should have been removed!");
                LsmNode previous = stack.Peek();
                // remove dependency previous -> node
                previous.dependencies.Remove(node.GetFullName());
                return;
            }

            stack.Push(node);

            // Copy dependencies for later removal
            Dictionary<string, Dependencies> dependencies = new Dictionary<string, Dependencies>(node.dependencies);
            int nodeLevel = 0;
            foreach (Dependencies dependency in dependencies.Values)
            {
                LsmNode neighbor = dependency.GetTarget();

                if (neighbor.GetLevel() == 0) // level not set yet, or truly 0 (in which case the call returns quickly)
                {
                    SetPathDepth(stack, neighbor);
                }

                // Level of a node is 1 larger than the level of its neighbour with the highest level.
                nodeLevel = Math.Max(nodeLevel, neighbor.GetLevel() + 1);
            }
            node.SetLevel(nodeLevel);

            stack.Pop();
        }
    }
}