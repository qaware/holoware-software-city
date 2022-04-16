using System;
using System.Collections.Generic;
using SoftwareCities.holoware.lsm;

namespace SoftwareCities.holoware.binpacker
{
    public abstract class PackerNode
    {
        public string Name { get; set; }
        public string FullyQualifiedName { get; set; }
        internal List<PackerNode> Children { get; set; }

        public PackerNode(string name, string fullyQualifiedName)
        {
            Children = new List<PackerNode>();
            Name = name;
            FullyQualifiedName = fullyQualifiedName;
        }

        public void AddChild(PackerNode node)
        {
            Children.Add(node);
        }

        /// <summary>
        /// Visitor pattern 
        /// </summary>
        /// <param name="v"></param>
        public abstract void Accept(IBinVisitor v);

        public override bool Equals(Object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;
            PackerNode node = (PackerNode) o;
            return Name.Equals(node.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public abstract bool IsCyclic(CycleDetector detector);
    }
}