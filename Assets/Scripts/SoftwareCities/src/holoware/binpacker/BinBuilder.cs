using System;
using System.Collections.Generic;
using SoftwareCities.holoware.lsm;

namespace SoftwareCities.holoware.binpacker
{
    public class BinBuilder
    {
        private BinBuilder()
        {
        }

        /// <summary>
        /// Creates the hierachy for the bin packing
        /// </summary>
        /// <param name="dependencies">the dependency graph</param>
        /// <returns>the root node</returns>
        public static PackerNode BinFromDependencyGraph(DependencyGraph dependencies)
        {
            PackerPackage rootNode = new PackerPackage("root", "root");

            // build the structure (packages contains packages or classes) of the LSM
            HashSet<String> sources = dependencies.GetSources();
            foreach (String source in sources)
            {
                AddElement(rootNode, source);
                HashSet<String> targets = dependencies.GetTargets(source);
                foreach (String target in targets)
                {
                    AddElement(rootNode, target);
                }
            }

            return rootNode;
        }

        /**
     * Adds an element (class or package) to the LSM graph.
     *
     * @param name a full qualified classname.
     */
        public static void AddElement(PackerNode parent, String name)
        {
            String[] names = name.Split('.');
            String fullyQualifiedName = ""; // todo: calculate from parent (Add parent to LsmNode)
            for (int i = 0; i < names.Length; i++)
            {
                String s = names[i];

                PackerNode element;
                // assumes the last split in a dotted String is a class !
                if (i == names.Length - 1)
                {
                    fullyQualifiedName += "." + s; // assuming a class is in a package
                    element = new PackerClass(s, fullyQualifiedName);
                }
                else
                {
                    if (fullyQualifiedName.Length == 0)
                    {
                        fullyQualifiedName = s;
                    }
                    else
                    {
                        fullyQualifiedName = fullyQualifiedName + "." + s;
                    }

                    element = new PackerPackage(s, fullyQualifiedName);
                }

                if (!parent.Children.Contains(element))
                {
                    parent.AddChild(element);
                    parent = element;
                }
                else
                {
                    parent = parent.Children[parent.Children.IndexOf(element)];
                }
            }
        }
    }
}