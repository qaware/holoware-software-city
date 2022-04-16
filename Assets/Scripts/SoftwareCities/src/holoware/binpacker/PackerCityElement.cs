using SoftwareCities.figures;

namespace SoftwareCities.holoware.binpacker
{
    public abstract class PackerCityElement : Figure
    {
        protected PackerNode node;
        public Size size;
        public string name;

        public PackerCityElement(Position position, Material material, PackerNode node) : base(node.FullyQualifiedName,
            position, material)
        {
            this.node = node;
            name = node.FullyQualifiedName;
        }

        public abstract void Layout();
    }

    public class Size
    {
        public int Height;

        public int Width;

        public Size(int height, int width)
        {
            Height = height;
            Width = width;
        }
    }
}