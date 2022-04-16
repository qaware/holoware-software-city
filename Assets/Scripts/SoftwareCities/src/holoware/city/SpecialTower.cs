using SoftwareCities.figures;

namespace SoftwareCities.holoware.city
{
    /// <summary>
    /// A nice tower with windows and a several floors. Currently not used.
    /// </summary>
    public class SpecialTower : Figure
    {
        private readonly int width;
        private readonly int length;
        private readonly Material front;
        private readonly Material windows;

        public SpecialTower(string label, Position position, Material front, Material windows, bool glow, int length,
            int width, int height) : base(position)
        {
            this.label = label;
            this.front = front;
            this.windows = windows;
            this.width = width;
            this.length = length;

            for (int i = 0; i < height; i += 2)
            {
                // with floor:
                // AddChild(new Cuboid(Position.XYZ(0, i * 2, 0), front, realLength, realWidth, 1));
                AddSideElement(i); // no floor
                AddMiddleElement(i);
                if (glow)
                {
                    AddGlowStones(i);
                }
            }

            // top
            AddChild(new Cuboid(Position.Xyz(0, height, 0), front, this.length, this.width, 1));
        }

        private void AddSideElement(int level)
        {
            int y = level;

            // todo: use Cuboid
            // horizontal row front XXXXX (X Stone, 0 Window)
            for (int j = 0; j < length; j++)
            {
                base.AddChild(new Cuboid(Position.Xyz(0, y, j), front, 1, 1, 1));
            }

            // horizontal row back XXXXX (X Stone, 0 Window)
            for (int j = 0; j < length; j++)
            {
                base.AddChild(new Cuboid(Position.Xyz(width - 1, y, j), front, 1, 1, 1));
            }

            // row left XXXXX (X Stone, 0 Window)
            for (int j = 1; j < width - 1; j++)
            {
                base.AddChild(new Cuboid(Position.Xyz(j, y, 0), front, 1, 1, 1));
            }

            // row right XXXXX (X Stone, 0 Window)
            for (int j = 1; j < width - 1; j++)
            {
                base.AddChild(new Cuboid(Position.Xyz(j, y, length - 1), front, 1, 1, 1));
            }
        }

        private void AddMiddleElement(int level)
        {
            int y = level + 1;

            // horizontal row front X0X0X (X Stone, 0 Window)
            for (int j = 0; j < length; j++)
            {
                if (j % 2 == 1)
                {
                    base.AddChild(new Cuboid(Position.Xyz(0, y, j), windows, 1, 1, 1));
                }
                else
                {
                    base.AddChild(new Cuboid(Position.Xyz(0, y, j), front, 1, 1, 1));
                }
            }

            // horizontal row back X0X0X (X Stone, 0 Window)
            for (int j = 0; j < length; j++)
            {
                if (j % 2 == 1)
                {
                    base.AddChild(new Cuboid(Position.Xyz(width - 1, y, j), windows, 1, 1, 1));
                }
                else
                {
                    base.AddChild(new Cuboid(Position.Xyz(width - 1, y, j), front, 1, 1, 1));
                }
            }

            // row left X0X0X (X Stone, 0 Window)
            for (int j = 1; j < width - 1; j++)
            {
                if (j % 2 == 1)
                {
                    base.AddChild(new Cuboid(Position.Xyz(j, y, 0), windows, 1, 1, 1));
                }
                else
                {
                    base.AddChild(new Cuboid(Position.Xyz(j, y, 0), front, 1, 1, 1));
                }
            }

            // row right X0X0X (X Stone, 0 Window)
            for (int j = 1; j < width - 1; j++)
            {
                if (j % 2 == 1)
                {
                    base.AddChild(new Cuboid(Position.Xyz(j, y, length - 1), windows, 1, 1, 1));
                }
                else
                {
                    base.AddChild(new Cuboid(Position.Xyz(j, y, length - 1), front, 1, 1, 1));
                }
            }
        }

        private void AddGlowStones(int level)
        {
            int y = level + 1;
            // horizontal row front X0X0X (X Stone, 0 Window)
            for (int i = 1; i < length - 1; i += 2)
            {
                AddChild(new Cuboid(Position.Xyz(1, y, i), Material.Glowstone, 1, 1, 1));
            }

            // horizontal row back X0X0X (X Stone, 0 Window)
            for (int i = 1; i < length - 1; i += 2)
            {
                AddChild(new Cuboid(Position.Xyz(width - 2, y, i), Material.Glowstone, 1, 1, 1));
            }
        }
    }
}