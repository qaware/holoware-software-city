using System.Linq;
using SoftwareCities.holoware.lsm;

namespace SoftwareCities.holoware.binpacker
{
    public class PackerPackage : PackerNode
    {
        public PackerPackage(string name, string fullyQualifiedName) : base(name, fullyQualifiedName)
        {
        }

        public override void Accept(IBinVisitor v)
        {
            v.VisitPackageEnter(this);
            foreach (PackerNode c in Children)
            {
                c.Accept(v);
            }

            v.VisitPackageLeave(this);
        }
        
        public bool IsTopPackage()
        {
            return !Children.OfType<PackerPackage>().Any();
        }
        
        public override bool IsCyclic(CycleDetector detector)
        {
            return Children.Any(next => next.IsCyclic(detector));
        }
    }
}