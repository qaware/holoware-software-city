using UnityEngine;
using Material = SoftwareCities.figures.Material;

namespace SoftwareCities.unityadapter.views
{
    public class DayView : View
    {
        public override CityView GetCityView()
        {
            return CityView.Day;
        }

        public override void SetMaterial()
        {
            Material.SetDay();
        }

        public override void DrawArches()
        {
            GameObject lineParent = new GameObject {name = "Arches"};
            DependencyDrawer.DrawCycles(cycles, unity.GameObjects, parent: lineParent);
        }
    }
}