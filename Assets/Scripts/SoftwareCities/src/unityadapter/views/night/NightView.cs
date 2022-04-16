using SoftwareCities.figures;
using UnityEngine;
using Material = SoftwareCities.figures.Material;

namespace SoftwareCities.unityadapter.views
{
    public class NightView : View
    {
        public bool drawCyclicDeps;
        public string spansFilePath = "Assets/Resources/TraceData/use-cases";

        public NightView()
        {
            isNightView = true;
        }

        public override CityView GetCityView()
        {
            return CityView.Night;
        }

        public override void SetMaterial()
        {
            Material.SetNight();
        }

        public override void DrawArches()
        {
            UseCaseEventManager ucem = gameObject.AddComponent<UseCaseEventManager>();
            UseCaseVisualizer ucv = new UseCaseVisualizer();
            ucv.SetupUseCases(unity.GameObjects, spansFilePath, false);

            if (drawCyclicDeps)
                DependencyDrawer.DrawCycles(cycles, unity.GameObjects, parent: rootArcsGO);
        }
    }
}