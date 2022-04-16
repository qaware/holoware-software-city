using System;
using SoftwareCities.dynamic;
using Material = SoftwareCities.figures.Material;

namespace SoftwareCities.unityadapter.views
{
    public class ErrorView : View
    {
        public string traceDataPath = "Assets/Resources/TraceData/db_broken";
        public bool tracesShouldBeAggregated = true;
        public int aggregationSpanInSeconds = 60;
        public bool drawCyclicDeps;

        public ErrorView()
        {
            isNightView = true;
        }

        public override CityView GetCityView()
        {
            return CityView.ErrorCodes;
        }

        public override void SetMaterial()
        {
            Material.SetError();
        }

        public override void DrawArches()
        {
            ErrorCodesEventManager em = gameObject.AddComponent<ErrorCodesEventManager>();

            ElasticSearchImporter importer = new ElasticSearchImporter(TimeSpan.FromSeconds(aggregationSpanInSeconds),
                tracesShouldBeAggregated);
            importer.LoadDynamicDependencies(traceDataPath);
            DynamicDependenciesVisualizer.VisualizeDependenciesOf(importer, unity.GameObjects);

            if (drawCyclicDeps)
                DependencyDrawer.DrawCycles(cycles, unity.GameObjects, parent: rootArcsGO);

        }
    }
}