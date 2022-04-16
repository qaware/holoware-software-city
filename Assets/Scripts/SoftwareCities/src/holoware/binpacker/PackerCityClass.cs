using System.Collections.Generic;
using System.Drawing;
using SoftwareCities.figures;
using SoftwareCities.holoware.lsm;

namespace SoftwareCities.holoware.binpacker
{
    public class PackerCityClass : PackerCityElement
    {
        private readonly DependencyGraph graph;

        private int fanOut = 0;
        private int fanIn = 0;

        public PackerCityClass(Material material, PackerClass clazz, DependencyGraph graph) : base(Position.Zero(),
            material, clazz)
        {
            this.graph = graph;
            CalculateSize();
        }

        public override void Layout()
        {
            // don't be confused, we have other dimensions
            int width = size.Height;
            int length = size.Width;
            int height = fanIn;

            AddChild(new Cuboid(node.FullyQualifiedName, Position.Zero(), material, length, width, height));
        }

        private void CalculateSize()
        {
            HashSet<string> fanOut = graph.GetTargets(node.FullyQualifiedName);
            HashSet<string> fanIn = graph.GetSources(node.FullyQualifiedName);
            this.fanIn = fanIn.Count + 1;
            this.fanOut = fanOut.Count + 1;
            size = new Size(this.fanOut, this.fanOut); // z,x coordinates
        }
    }
}