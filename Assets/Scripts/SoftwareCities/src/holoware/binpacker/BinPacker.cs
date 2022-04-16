using System.Collections.Generic;
using System.Drawing;
using SoftwareCities.figures;

namespace SoftwareCities.holoware.binpacker
{
    public class BinPacker
    {
        private static readonly int Spacing = 2;
        private Node root;

        private readonly List<Node> blocks = new List<Node>();

        public BinPacker()
        {
        }

        public Size GetRootSize()
        {
            return new Size((int) root.w, (int) root.h);
        }

        public Position GetPositionFor(string name)
        {
            Node node = blocks.FindLast(block => block.name.Equals(name));
            if (node.fit != null)
                return Position.Xyz((int) node.fit.x + Spacing / 2, 1, (int) node.fit.y + Spacing / 2);
            return Position.Zero();
        }

        public void AddNode(string name, double w, double h)
        {
            blocks.Add(new Node(name, w + Spacing, h + Spacing));
        }

        public void Fit()
        {
            blocks.Sort((a, b) => (b.h * b.w).CompareTo(a.h * a.w));
            if (blocks.Count == 0)
                return;
            root = new Node(0, 0, blocks[0].w, blocks[0].h);
            blocks.ForEach(block =>
            {
                Node node = FindNode(root, block.w, block.h);
                if (node != null)
                {
                    block.fit = SplitNode(node, block.w, block.h);
                }
                else
                {
                    block.fit = GrowNode(block.w, block.h);
                }
            });
        }

        private Node FindNode(Node root, double w, double h)
        {
            if (root.used)
            {
                Node right = FindNode(root.right, w, h);
                return (right ?? FindNode(root.down, w, h));
            }
            else if ((w <= root.w) && (h <= root.h))
            {
                return root;
            }
            else
            {
                return null;
            }
        }

        private Node SplitNode(Node node, double w, double h)
        {
            node.used = true;
            node.down = new Node(node.x, node.y + h, node.w, node.h - h);
            node.right = new Node(node.x + w, node.y, node.w - w, h);
            return node;
        }

        private Node GrowNode(double w, double h)
        {
            bool canGrowDown = (w <= root.w);
            bool canGrowRight = (h <= root.h);

            bool shouldGrowRight = canGrowRight && (root.h >= (root.w + w));
            bool shouldGrowDown = canGrowDown && (root.w >= (root.h + h));

            if (shouldGrowRight)
                return GrowRight(w, h);
            else if (shouldGrowDown)
                return GrowDown(w, h);
            else if (canGrowRight)
                return GrowRight(w, h);
            else if (canGrowDown)
                return GrowDown(w, h);
            return null;
        }

        private Node GrowRight(double w, double h)
        {
            Node newRoot = new Node(0, 0, root.w + w, root.h)
            {
                used = true,
                down = root,
                right = new Node(root.w, 0, w, root.h)
            };
            root = newRoot;

            Node node = FindNode(root, w, h);
            if (node != null)
            {
                return SplitNode(node, w, h);
            }

            return null;
        }

        private Node GrowDown(double w, double h)
        {
            Node newRoot = new Node(0, 0, root.w, root.h + h)
            {
                used = true,
                down = new Node(0, root.h, root.w, h),
                right = root
            };
            root = newRoot;

            Node node = FindNode(root, w, h);
            if (node != null)
            {
                return SplitNode(node, w, h);
            }

            return null;
        }
    }

    internal class Node
    {
        public string name;
        public double x;
        public double y;
        public double w;
        public double h;
        public bool used = false;
        public Node right = null;
        public Node down = null;
        public Node fit = null;

        public Node(string name, double w, double h)
        {
            this.name = name;
            this.w = w;
            this.h = h;
        }

        public Node(double x, double y, double w, double h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }
    }
}