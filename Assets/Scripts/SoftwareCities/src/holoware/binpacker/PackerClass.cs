using SoftwareCities.holoware.lsm;

namespace SoftwareCities.holoware.binpacker
{
    public class PackerClass : PackerNode
    {
        public PackerClass(string name, string fullyQualifiedName) : base(name, fullyQualifiedName)
        {
        }

        public override void Accept(IBinVisitor v)
        {
            v.VisitClazz(this);
        }
        
        public override bool IsCyclic(CycleDetector detector)
        {
            return detector.IsCyclic(FullyQualifiedName);
        }
    }
}