using System.Collections.Generic;
using SoftwareCities.figures;

namespace SoftwareCities.holoware.binpacker
{
    public class PackerCityPackage : PackerCityElement
    {
        public PackerCityPackage(Material material, PackerPackage package) : base(Position.Zero(), material, package)
        {
        }

        public override void Layout()
        {
            BinPacker packer = new BinPacker();
            List<Figure> children = GetChildren();
            foreach (Figure child in children)
            {
                if (!(child is PackerCityElement))
                    continue;
                ((PackerCityElement) child).Layout();
                // Add city elements to the bin packer
                PackerCityElement element = (PackerCityElement) child;
                packer.AddNode(element.name, element.size.Width, element.size.Height);
            }

            // fit all elements in the bin 
            packer.Fit();
            children.ForEach(child =>
            {
                if (child is PackerCityElement element)
                {
                    // set positions according to bin packing 
                    element.position = packer.GetPositionFor(element.name);
                }
            });
            size = packer.GetRootSize();
            AddChild(new Cuboid(name, Position.Zero(), material, size.Width, size.Height, 1));
        }
    }
}