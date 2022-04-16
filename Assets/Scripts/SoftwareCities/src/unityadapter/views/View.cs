using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SoftwareCities.figures;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Material = SoftwareCities.figures.Material;

namespace SoftwareCities.unityadapter.views
{
    public abstract class View : MonoBehaviour
    {
        public bool export = true;
        public string exportPath = "gameObjectPositions.json";
        public Figure rootToDraw { get; set; }
        public List<KeyValuePair<string, string>> cycles;
        protected GameObject rootArcsGO;
        protected UnityVisitor unity;
        public string jobname;

        protected bool gameobjectsFinished;
        protected GameObject rootGO;
        protected bool isNightView;

        public static bool reset = false;

        /// <summary>
        /// Returns CityView instance corresponding to this particular View
        /// </summary>
        /// <returns></returns>
        public abstract CityView GetCityView();

        /// <summary>
        /// Sets the default materials for creating new GameObjects to those fitting to this view.
        /// </summary>
        public abstract void SetMaterial();


        /// <summary>
        /// Sets all objects in the scene as active / inactive.
        /// </summary>
        /// <param name="active"></param>
        public void SetActive(bool active)
        {
            Scene scene = gameObject.scene;
            foreach (GameObject o in scene.GetRootGameObjects())
            {
                o.SetActive(active);
            }
        }

        /// <summary>
        /// Renders LSM according to the root Figure.
        /// </summary>
        /// <returns>root object of LSM</returns>
        public GameObject DrawBuildings()
        {
            unity = new UnityVisitor(rootToDraw);
            unity.Run();
            foreach (GameObject o in gameObject.scene.GetRootGameObjects())
            {
                if (o.name == "root")
                {
                    return o;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates arches, meant to be overwritten by each view.
        /// </summary>
        public abstract void DrawArches();

        /// <summary>
        /// Creates a block with description in the root district.
        /// </summary>
        /// <param name="root">Root of the LSM</param>
        /// <returns>Description GameObject</returns>
        public GameObject DrawDescription(GameObject root)
        {
            GameObject description = GameObject.CreatePrimitive(PrimitiveType.Cube);
            description.name = "Description";
            Transform cuboid = root.transform.GetChild(root.transform.childCount - 1);
            float width = Math.Max(5, cuboid.localScale.z / 100);
            description.transform.localScale = new Vector3(cuboid.localScale.x, 1, width);
            description.transform.position = new Vector3(cuboid.localScale.x / 2, 0, -width / 2);

            Renderer renderer = description.GetComponent<Renderer>();
            renderer.material =
                Resources.Load<UnityEngine.Material>("Materials/" + Material.viewDescription);

            GameObject childObj = new GameObject();
            TextMeshPro textObj = childObj.AddComponent<TextMeshPro>();
            childObj.transform.position = description.transform.position + new Vector3(0, 0.55f, 0);
            childObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            textObj.text = "root area | " + jobname;
            RectTransform tr = childObj.GetComponent<RectTransform>();
            tr.sizeDelta = new Vector2(cuboid.localScale.x - 4, width);
            textObj.font = TMP_FontAsset.CreateFontAsset(Resources.Load<Font>("Fonts/Montserrat-Medium"));
            textObj.alignment = TextAlignmentOptions.Center;
            textObj.enableAutoSizing = true;
            textObj.color = isNightView ? Color.white : Color.black;
            textObj.enableWordWrapping = false;

            childObj.transform.SetParent(description.transform, true);

            return description;
        }

        public void Update()
        {
            if (rootToDraw != null)
            {
                rootGO = DrawBuildings();
                DrawArches();
                DrawDescription(rootGO);
                rootToDraw = null;
                reset = true;

                Transform rootCuboid = rootGO.transform.GetChild(rootGO.transform.childCount - 1);
                GameObject.Find("Pivot").transform.position = rootCuboid.localScale / 2;

                if (export)
                {
                    Export(exportPath);
                }
            }
        }

        public void Export(string exportPath)
        {
            if (!gameobjectsFinished)
            {
                return;
            }

            Collection<ExportPosition> exportPositions = new Collection<ExportPosition>();
            foreach (GameObject unityGameObject in unity.GameObjects)
            {
                exportPositions.Add(new ExportPosition(unityGameObject.transform.parent.name,
                    unityGameObject.transform.position, unityGameObject.transform.localScale));
            }

            using (StreamWriter writer = new StreamWriter(File.OpenWrite(exportPath)))
            {
                string json = JsonUtility.ToJson(new ExportPositions(exportPositions.ToArray()), true);
                writer.Write(json);
            }
        }

        [Serializable]
        public class ExportPositions
        {
            [SerializeField] public ExportPosition[] gameObjectPositions;

            public ExportPositions(ExportPosition[] gameObjectPositions)
            {
                this.gameObjectPositions = gameObjectPositions;
            }
        }

        [Serializable]
        public class ExportPosition
        {
            public string name;
            public Vector3 position;
            public Vector3 scale;

            public ExportPosition(string name, Vector3 position, Vector3 scale)
            {
                this.name = name;
                this.position = position;
                this.scale = scale;
            }
        }
    }
}