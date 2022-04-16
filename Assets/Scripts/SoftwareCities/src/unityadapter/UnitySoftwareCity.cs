using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SoftwareCities.figures;
using SoftwareCities.holoware.city;
using SoftwareCities.holoware.lsm;
using SoftwareCities.unityadapter.views;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoftwareCities.unityadapter
{
    public class UnitySoftwareCity : MonoBehaviour
    {
        public string dotFilePath = "Assets/Resources/Dot/spring-boot-realworld-example-app.dot";
        public CityView view = CityView.Night;
        private Figure rootElement;
        private bool initFinished;
        private Dictionary<CityView, View> SceneComponents;

        public List<KeyValuePair<string, string>> cycles;
        private DependencyGraph graph;

        public static UnitySoftwareCity Instance;

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                throw new Exception("Multiple instances of UnitySoftwareCity not allowed!");
            }
        }

        /// <summary>
        /// Loads scene and adds its View component to SceneComponents.
        /// </summary>
        /// <param name="sceneName">name of the scene as in Assets</param>
        /// <param name="forceLoad">force load scene if is not added in the project</param>
        /// <returns></returns>
        private IEnumerator LoadAsyncScene(string sceneName, bool forceLoad = false)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() && !forceLoad)
            {
                yield break;
            }
            if (!scene.IsValid())
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

                while (!asyncLoad.isDone)
                {
                    yield return null;
                }

                scene = SceneManager.GetSceneByName(sceneName);
            }

            View sceneView = null;
            foreach (GameObject o in scene.GetRootGameObjects())
            {
                if (o.name == "Pivot")
                {
                    sceneView = o.GetComponent<View>();
                    break;
                }
            }

            SceneComponents.Add(sceneView.GetCityView(), sceneView);
            sceneView.SetActive(false);
        }

        /// <summary>
        /// Loads scenes and marks isFinished as true when done.
        /// </summary>
        /// <returns>IEnumerator to be called as Coroutine</returns>
        private IEnumerator AddScenes()
        {
            SceneComponents = new Dictionary<CityView, View>();

            yield return StartCoroutine(LoadAsyncScene("Day Scene", true));
            yield return StartCoroutine(LoadAsyncScene("Night Scene", false));
            yield return StartCoroutine(LoadAsyncScene("Error Scene", false));

            initFinished = true;
        }

        public void Start()
        {
            StartCoroutine(AddScenes());
            SetView(view);

            // Avoid blocking. Load file async.
            Task.Factory.StartNew(() => ProcessDotfile());
        }

        /// <summary>
        /// Creates a graph from the Dotfile, builds LSM and assigns the root node.
        /// </summary>
        private void ProcessDotfile()
        {
            try
            {
                Stream dotFile = File.OpenRead(dotFilePath);
                Debug.Log("Loading .dot file: " + dotFilePath + " ...");
                graph = DependencyGraph.FromDot(dotFile);
                Debug.Log("Constructing LSM ...");
                LsmBuilder builder = LsmBuilder.LsmFromGraph(graph);
                cycles = builder.edgesWrongDirection;
                Lsm2CityVisitor visitor = new Lsm2CityVisitor(graph);
                Debug.Log("Constructing UI elements ...");
                builder.RootNode.Accept(visitor);
                rootElement = visitor.GetResult();

                Debug.Log("Success.");
            }
            catch (IOException e)
            {
                Debug.LogError(e);
            }
        }

        public void Update()
        {
            if (rootElement != null && initFinished)
            {
                SetView(view);
                foreach (KeyValuePair<CityView, View> pair in SceneComponents)
                {
                    pair.Value.jobname = dotFilePath.Split('/').Last();
                    pair.Value.cycles = cycles;
                    pair.Value.rootToDraw = rootElement;
                }

                rootElement = null;
            }
        }

        public bool SetView(CityView newView)
        {
            if (!SceneComponents.ContainsKey(newView))
            {
                return false;
            }

            SceneComponents[newView].SetMaterial();
            foreach (KeyValuePair<CityView,View> pair in SceneComponents)
            {
                if (pair.Key == newView)
                {
                    pair.Value.SetActive(true);
                    SceneManager.SetActiveScene(pair.Value.gameObject.scene);
                }
                else
                {
                    pair.Value.SetActive(false);
                }
            }
            view = newView;

            return true;
        }
    }
}