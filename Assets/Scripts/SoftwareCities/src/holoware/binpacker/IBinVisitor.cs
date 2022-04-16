namespace SoftwareCities.holoware.binpacker
{
    public interface IBinVisitor
    {
        /// <summary>
        /// This method will be called for every PackerClass object in the tree.
        /// </summary>
        /// <param name="clazz">the class to be visited</param>
        void VisitClazz(PackerClass clazz);

        /// <summary>
        /// This method is called before the recursive descent of a package. 
        /// </summary>
        /// <param name="pkg">the package to be visited</param>
        void VisitPackageEnter(PackerPackage pkg);

        /// <summary>
        /// This method is called after the recursive descent of a package. 
        /// </summary>
        /// <param name="pkg">the visited package</param>
        void VisitPackageLeave(PackerPackage pkg);
    }
}