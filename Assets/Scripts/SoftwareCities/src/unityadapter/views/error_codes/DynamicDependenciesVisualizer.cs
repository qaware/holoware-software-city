using System;
using System.Collections.Generic;
using System.Linq;
using SoftwareCities.dynamic;
using UnityEngine;
using UnityEngine.UI;
using Material = SoftwareCities.figures.Material;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace SoftwareCities.unityadapter.views
{
    public class DynamicDependenciesVisualizer
    {
        private readonly DynamicDependenciesImporter dependenciesImporter;

        private readonly List<GameObject> gameObjects;

        private IntensityMinMaxScaler scalerComponentLoad;
        private IntensityMinMaxScaler scalerCallLoad;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissiveColor");

        private readonly Color purple = new Color(174, 0, 191);
        private readonly Color turquoise = new Color(0, 220, 255);

        public static GameObject lineParent = new GameObject() {name = "Arches"};

        private LabelCreator labelCreator = LabelCreator.ForViewDescription();
        public GameObject timestampLabel;

        private MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        /// <summary>
        /// List holding the drawn dependency lines. Needed to redraw them on increasing / decreasing time frame
        /// </summary>
        private readonly List<GameObject> dependencyLines = new List<GameObject>();

        public static DynamicDependenciesVisualizer VisualizeDependenciesOf(
            DynamicDependenciesImporter dependenciesImporter,
            List<GameObject> gameObjects)
        {
            return new DynamicDependenciesVisualizer(dependenciesImporter, gameObjects);
        }

        /// <summary>
        /// Subscribe to user input to change time frames.
        /// </summary>
        /// <param name="dependenciesImporter">To get the activity of components in order to change the light intensity</param>
        /// <param name="gameObjects">To change the material / light of game objects</param>
        private DynamicDependenciesVisualizer(DynamicDependenciesImporter dependenciesImporter,
            List<GameObject> gameObjects)
        {
            this.dependenciesImporter = dependenciesImporter;
            this.gameObjects = gameObjects;
            InitMinMaxScaler();
            timestampLabel = labelCreator.CreateLabel(dependenciesImporter.GetCurrentTimestamp());
            UpdateIntensity();
            ErrorCodesEventManager.OnNextTimeFrame += IncreaseTimeFrame;
            ErrorCodesEventManager.OnPreviousTimeFrame += DecreaseTimeFrame;
            MouseClick.OnComponentClick += ShowDepsForComponent;
            MouseClick.OnComponentRelease += ShowAllDeps;
        }

        private void ShowAllDeps()
        {
            foreach (GameObject dependencyLine in dependencyLines)
            {
                if (dependencyLine != null)
                    dependencyLine.SetActive(true);
            }
        }

        private void ShowDepsForComponent(string componentName)
        {
            foreach (GameObject dependencyLine in dependencyLines)
            {
                if (dependencyLine != null && !dependencyLine.name.Contains(componentName + "_"))
                    dependencyLine.SetActive(false);
            }
        }

        /// <summary>
        /// Unsubscribe from events 
        /// </summary>
        ~DynamicDependenciesVisualizer()
        {
            ErrorCodesEventManager.OnNextTimeFrame -= IncreaseTimeFrame;
            ErrorCodesEventManager.OnPreviousTimeFrame -= DecreaseTimeFrame;
            MouseClick.OnComponentClick -= ShowDepsForComponent;
            MouseClick.OnComponentRelease -= ShowAllDeps;
        }

        /// <summary>
        /// Initialize MinMaxScaler based on the minimal and maximal load found in the dependencies 
        /// </summary>
        private void InitMinMaxScaler()
        {
            scalerComponentLoad =
                new IntensityMinMaxScaler(dependenciesImporter.GetMinLoad(), dependenciesImporter.GetMaxLoad());
            scalerCallLoad = new IntensityMinMaxScaler(dependenciesImporter.GetMinCallLoad(),
                dependenciesImporter.GetMaxCallLoad());
        }

        /// <summary>
        /// Update the light intensities of the game objects based on current time frame 
        /// </summary>
        private void UpdateIntensity()
        {
            foreach (GameObject t in dependencyLines)
            {
                Object.Destroy(t);
            }

            foreach (GameObject gameObject in gameObjects)
            {
                // Only one child meaning we're on building level 
                Transform parent = gameObject.transform.parent;
                if (parent == null || parent.childCount > 1) continue;
                ComponentLoad allLoads =
                    dependenciesImporter.GetActivityFor(parent.name.Split('.').Last());
                DrawDependenciesEdgeBundled(allLoads, gameObject);
            }

            labelCreator.UpdateLabel(timestampLabel, dependenciesImporter.GetCurrentTimestamp());
        }

        private void DrawDependencies(List<ComponentLoad> allLoads, GameObject gameObject)
        {
            foreach (ComponentLoad load in allLoads)
            {
                gameObject.GetComponent<Renderer>().material
                    .SetColor(BaseColorId, purple);
                // Draw lines for each parent child dynamic dependency 
                float stepSize = 0.4F / load.CallLoads.Count;
                float offset = load.CallLoads.Count > 1 ? -0.4F : 0F;
                foreach (CallLoad callLoad in load.CallLoads)
                {
                    GameObject to = gameObjects.Find(go =>
                        go.transform.parent.gameObject.name.Contains(callLoad.ParentName));
                    Vector3 startPos = gameObject.transform.position;
                    startPos.x += gameObject.GetComponent<Collider>().bounds.size.x * offset;
                    startPos.z += gameObject.GetComponent<Collider>().bounds.size.z * offset;
                    Vector3 endPos = to.transform.position;
                    endPos.x += to.GetComponent<Collider>().bounds.size.x * offset;
                    endPos.z += to.GetComponent<Collider>().bounds.size.z * offset;
                    //GameObject line = DependencyDrawer.DrawLine(startPos,
                    //    endPos, Material.RedGlass);
                    GameObject line = DependencyDrawer.DrawArc(gameObject, to, Material.GreenMetal, radius: 0.3f,
                        parent: lineParent, randomOffset: true, flat: true);
                    float successLoad = callLoad.Load[DynamicDependenciesImporter.HttpStatusCodes.Success];
                    float clientErrorLoad = callLoad.Load[DynamicDependenciesImporter.HttpStatusCodes.ClientError];
                    float serverErrorLoad = callLoad.Load[DynamicDependenciesImporter.HttpStatusCodes.ServerError];
                    float hueColor = (successLoad * 0 + clientErrorLoad * 45 + serverErrorLoad * 90) /
                                     (successLoad + clientErrorLoad + serverErrorLoad);
                    Color hsvColor = Color.HSVToRGB(hueColor, 1, 0.5F);
                    line.GetComponent<Renderer>().material
                        .SetColor(BaseColorId, hsvColor * 0.75F);
                    line.name = callLoad.ParentName + "_" + load.ComponentName + "_";
                    line.transform.parent = lineParent.transform;
                    dependencyLines.Add(line);
                    offset += stepSize;
                }
            }
        }

        private void DrawDependenciesEdgeBundled(ComponentLoad load, GameObject gameObject)
        {
            Color componentColorBasedOnLoad = calculateColorBasedOnLoad(load.TotalLoad, scalerComponentLoad);
            Renderer r = gameObject.GetComponent<Renderer>();

            // Workaround with PropertyBlock: r.material.SetColor("_EmissiveColor", componentColorBasedOnLoad) etc.
            // sets the color correctly (as seen in the inspector in Unity), but the emission doesn't show in the scene.
            r.GetPropertyBlock(mpb, 0);
            mpb.SetColor(BaseColorId, componentColorBasedOnLoad);
            mpb.SetColor(EmissionColorId, componentColorBasedOnLoad * 10);
            r.SetPropertyBlock(mpb, 0);

            foreach (CallLoad callLoad in load.CallLoads)
            {
                GameObject to = gameObjects.Find(go =>
                    go.transform.parent.gameObject.name.EndsWith(callLoad.ParentName));
                GameObject line = DependencyDrawer.DrawArc(gameObject, to, Material.GreenMetal, radius: 0.3f,
                    parent: lineParent, randomOffset: true, flat: true, arrow: true);
                Color arcColorBasedOnLoad = calculateColorBasedOnLoad(callLoad.Load, scalerCallLoad);
                line.GetComponent<Renderer>().material.SetColor(BaseColorId, arcColorBasedOnLoad);
                line.GetComponent<Renderer>().material.SetColor(EmissionColorId, arcColorBasedOnLoad * 100);
                line.name = callLoad.ParentName + "_" + load.ComponentName + "_";
                dependencyLines.Add(line);
            }
        }

        private Color calculateColorBasedOnLoad(Dictionary<DynamicDependenciesImporter.HttpStatusCodes, float> load,
            IntensityMinMaxScaler scaler)
        {
            load.TryGetValue(DynamicDependenciesImporter.HttpStatusCodes.Success,
                out float successLoad);
            load.TryGetValue(DynamicDependenciesImporter.HttpStatusCodes.ClientError,
                out float clientErrorLoad);
            load.TryGetValue(DynamicDependenciesImporter.HttpStatusCodes.ServerError,
                out float serverErrorLoad);
            float totalLoad = successLoad + clientErrorLoad + serverErrorLoad;
            float hueColor = (successLoad * 120 + clientErrorLoad * 60 + serverErrorLoad * 0) /
                             totalLoad;
            Color rgbColor = Color.HSVToRGB(hueColor / 360, 1, 1);
            return rgbColor * 255 * scaler.Scale(totalLoad);
        }

        /// <summary>
        /// Increase time frame and update intensities  
        /// </summary>
        private void IncreaseTimeFrame()
        {
            Debug.Log("IncreaseTimeFrame");
            if (dependenciesImporter.IncrementBucketIndex())
                UpdateIntensity();
        }

        /// <summary>
        /// Decrease time frame and update intensities  
        /// </summary>
        private void DecreaseTimeFrame()
        {
            Debug.Log("DecreaseTimeFrame");
            if (dependenciesImporter.DecrementBucketIndex())
                UpdateIntensity();
        }

        /// <summary>
        /// Helper class for MinMaxScaling 
        /// </summary>
        private class IntensityMinMaxScaler
        {
            private readonly float min;
            private readonly float max;
            private const float MINIntensity = 0.0F;
            private const float MAXIntensity = 0.05F;
            private const float OutlierIntensity = 0.05F;

            public IntensityMinMaxScaler(float min, float max)
            {
                this.min = min;
                this.max = max;
            }

            internal float Scale(float value)
            {
                float scaled = MINIntensity + (value - min) / (max - min) * (MAXIntensity - MINIntensity);
                return scaled > MAXIntensity ? OutlierIntensity : scaled;
            }
        }
    }
}