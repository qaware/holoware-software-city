using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoftwareCities.dynamic;
using UnityEngine;
using Material = SoftwareCities.figures.Material;

namespace SoftwareCities.unityadapter.views
{
    public class UseCaseVisualizer
    {
        private Dictionary<string, List<Span>> useCases = new Dictionary<string, List<Span>>();

        private int currentUseCaseIndex;

        private int currentPOIIndex;

        private bool stepThroughEnabled;

        private List<GameObject> useCaseLines = new List<GameObject>();

        private List<GameObject> gameObjects;

        public static GameObject lineParent = new GameObject() {name = "Arches"};

        private LabelCreator labelCreator = LabelCreator.ForViewDescription();
        public GameObject usecaseLabel;

        /**
         * Representing start and end points of spans as points of interest of an use case 
         */
        private SortedSet<long> currentPOIs = new SortedSet<long>();

        public UseCaseVisualizer()
        {
            UseCaseEventManager.OnNextUseCase += DisplayNextUseCase;
            UseCaseEventManager.OnPreviousUseCase += DisplayPreviousUseCase;
            UseCaseEventManager.OnNextPOI += DisplayNextPOI;
            UseCaseEventManager.OnPreviousPOI += DisplayPreviousPOI;
        }

        ~UseCaseVisualizer()
        {
            UseCaseEventManager.OnNextUseCase -= DisplayNextUseCase;
            UseCaseEventManager.OnPreviousUseCase -= DisplayPreviousUseCase;
            UseCaseEventManager.OnNextPOI -= DisplayNextPOI;
            UseCaseEventManager.OnPreviousPOI -= DisplayPreviousPOI;
        }

        public void SetupUseCases(List<GameObject> gameObjects, string useCasePath, bool stepThroughEnabled)
        {
            this.stepThroughEnabled = stepThroughEnabled;
            this.gameObjects = gameObjects;
            string[] spanFiles =
                Directory.GetFiles(useCasePath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string spanFile in spanFiles)
            {
                using (StreamReader reader = new StreamReader(File.OpenRead(spanFile)))
                {
                    string jsonString = reader.ReadToEnd();
                    ElasticSearchSpans traceData = JsonUtility.FromJson<ElasticSearchSpans>(jsonString);
                    List<Span> spans = SpanMapper.ToSpans(traceData);
                    spans.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
                    useCases.Add(spanFile.Split('/').Last(), spans);
                }
            }

            currentUseCaseIndex = 0;
            FillPOIs();

            usecaseLabel = labelCreator.CreateLabel(GetUseCaseLabel());
            DrawUseCaseLines();
        }

        private void FillPOIs()
        {
            currentPOIIndex = 0;
            if (stepThroughEnabled)
            {
                foreach (Span span in useCases.ElementAt(currentUseCaseIndex).Value)
                {
                    currentPOIs.Add(span.Timestamp);
                    currentPOIs.Add(span.Timestamp + span.Duration + 1);
                }
            }
            else
            {
                currentPOIs.Add(useCases.ElementAt(currentUseCaseIndex).Value.Min(span => span.Timestamp));
                currentPOIs.Add(useCases.ElementAt(currentUseCaseIndex).Value
                    .Max(span => span.Timestamp + span.Duration + 1));
            }
        }

        public void DisplayNextUseCase()
        {
            currentUseCaseIndex = currentUseCaseIndex < useCases.Count - 1 ? currentUseCaseIndex + 1 : 0;
            FillPOIs();
            DrawUseCaseLines();
            labelCreator.UpdateLabel(usecaseLabel, GetUseCaseLabel());
        }

        public void DisplayPreviousUseCase()
        {
            currentUseCaseIndex = currentUseCaseIndex > 0 ? currentUseCaseIndex - 1 : useCases.Count - 1;
            FillPOIs();
            DrawUseCaseLines();
            labelCreator.UpdateLabel(usecaseLabel, GetUseCaseLabel());
        }

        public void DisplayPreviousPOI()
        {
            currentPOIIndex = currentPOIIndex > 0 ? currentPOIIndex - 1 : 0;
            DrawUseCaseLines();
        }

        public void DisplayNextPOI()
        {
            currentPOIIndex = currentPOIIndex < currentPOIs.Count - 1 ? currentPOIIndex + 1 : currentPOIIndex;
            DrawUseCaseLines();
        }

        private string GetUseCaseLabel()
        {
            return useCases.ElementAt(currentUseCaseIndex).Key.Split('.')[0];
        }

        private void DrawUseCaseLines()
        {
            foreach (GameObject useCaseLine in useCaseLines)
            {
                GameObject.Destroy(useCaseLine);
            }

            List<Span> currentActiveSpans = useCases.ElementAt(currentUseCaseIndex).Value;
            if (stepThroughEnabled)
            {
                currentActiveSpans = currentActiveSpans.FindAll(span =>
                    currentPOIs.ElementAt(currentPOIIndex) >= span.Timestamp &&
                    currentPOIs.ElementAt(currentPOIIndex) <= span.Timestamp + span.Duration);
            }

            foreach (Span currentActiveSpan in currentActiveSpans)
            {
                if (string.IsNullOrEmpty(currentActiveSpan.ParentName)) continue;
                GameObject from = gameObjects.Find(go =>
                    go.transform.parent.name.Contains(currentActiveSpan.ParentName));
                GameObject to = gameObjects.Find(go =>
                    go.transform.parent.name.Contains(currentActiveSpan.ComponentName));
                if (from == null || to == null) continue;
                GameObject line = DependencyDrawer.DrawArc(from, to, Material.usecaseTube, radius: 0.5f, parent: lineParent);
                useCaseLines.Add(line);
            }
        }
    }
}