using System.Collections.Generic;
using SoftwareCities.figures;
using SoftwareCities.holoware.lsm;

namespace SoftwareCities.holoware.binpacker
{
    public class Bin2CityVisitor : IBinVisitor
    {
        private readonly Stack<PackerCityElement> currentFigure;
        private readonly DependencyGraph graph;
        private PackerCityElement city;

        /// <summary>
        /// The cycle detector to color cyclic components. 
        /// </summary>
        public readonly CycleDetector detector;

        public Bin2CityVisitor(DependencyGraph graph)
        {
            this.graph = graph;
            detector = CycleDetector.ForGraph(graph);
            currentFigure = new Stack<PackerCityElement>();
        }

        public void VisitClazz(PackerClass clazz)
        {
            PackerCityClass tower = new PackerCityClass(Material.BlueMetal2, clazz, graph);
            // red glass material if class is cyclic
            if (clazz.IsCyclic(detector))
            {
                //tower.material = Material.RedMetal;
            }

            currentFigure.Peek().AddChild(tower);
        }

        public void VisitPackageEnter(PackerPackage pkg)
        {
            // set material based on package properties 
            Material material;
            if (pkg.IsCyclic(detector) && pkg.IsTopPackage())
            {
                material = Material.RedGlass;
            }
            else if (!pkg.IsTopPackage())
            {
                material = Material.OrangeMetal;
            }
            else
            {
                material = Material.WhiteMetal;
            }

            PackerCityPackage packageBasement = new PackerCityPackage(material, pkg);
            if (currentFigure.Count > 0)
            {
                currentFigure.Peek().AddChild(packageBasement);
            }

            currentFigure.Push(packageBasement);
        }

        public void VisitPackageLeave(PackerPackage pkg)
        {
            city = currentFigure.Pop(); // done with this figure
        }

        /**
     * Gets the city after the visitor is complete run (with accept)
     *
     * @return the constructed city
     */
        public PackerCityElement GetCity()
        {
            city.Layout();
            return city;
        }
    }
}