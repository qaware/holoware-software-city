using UnityEngine;
using UnityEngine.UI;

namespace SoftwareCities.unityadapter
{
    public class LabelCreator
    {
        private Color textColor;
        private string imageBackgroundPath;
        private LabelPosition labelPosition;
        private bool resizeTextForBestFit;

        private enum LabelPosition
        {
            UpperRight,
            LowerCenter
        }

        private LabelCreator(){}

        public static LabelCreator ForClassNames()
        {
            return new LabelCreator
            {
                textColor = Color.black,
                imageBackgroundPath = "class-label",
                labelPosition = LabelPosition.LowerCenter,
                resizeTextForBestFit = true
            };
        }

        public static LabelCreator ForViewDescription()
        {
            return new LabelCreator
            {
                textColor = Color.white,
                imageBackgroundPath = null,
                labelPosition = LabelPosition.UpperRight,
                resizeTextForBestFit = false
            };
        }
        
        public GameObject CreateLabel(string labelText)
        {
            // Canvas
            Canvas infoCanvas = CreateCanvas();

            // Panel
            GameObject panel = CreatePanel(infoCanvas);

            // Text
            CreateText(panel, labelText);

            return infoCanvas.gameObject;
        }

        public void UpdateLabel(GameObject label, string labelText)
        {
            Transform panelTransform = label.transform.GetChild(0);
            Text text = panelTransform.GetChild(0).GetComponent<Text>();
            text.text = labelText;
        }

        /// <summary>
        /// Add a text to the given panel, center alignment
        /// </summary>
        /// <param name="panel">the panel to which the text will be added</param>
        /// <param name="labelText">that should be shown in the canvas</param>
        private void CreateText(GameObject panel, string labelText)
        {
            GameObject myText = new GameObject();
            myText.transform.SetParent(panel.transform);
            myText.name = "Component name";
            Text text = myText.AddComponent<Text>();
            text.font = Resources.Load<Font>("Fonts/Montserrat-Medium");
            text.text = labelText;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.resizeTextForBestFit = resizeTextForBestFit;

            // Text position
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.localPosition = new Vector3(0, 0, 0);
            textRect.sizeDelta = panel.GetComponent<RectTransform>().sizeDelta;
            textRect.transform.SetParent(panel.transform);
        }

        /// <summary>
        /// Add a panel to a given canvas
        /// </summary>
        /// <param name="canvas">the canvas to which the panel will be added</param>
        /// <returns>the created panel</returns>
        private GameObject CreatePanel(Component canvas)
        {
            GameObject panel = new GameObject();
            panel.AddComponent<CanvasRenderer>();
            panel.AddComponent<RectTransform>();
            panel.transform.SetParent(canvas.transform);

            if (imageBackgroundPath != null)
            {
                panel.AddComponent<Image>();
                Image image = panel.GetComponent<Image>();
                image.color = new Color(255, 255, 255, 0.6F);
                image.sprite = Resources.Load<Sprite>(imageBackgroundPath);
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
            }

            // Panel position 
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.transform.SetParent(panel.transform);
            switch (labelPosition)
            {
                case LabelPosition.LowerCenter:
                    panelRect.anchorMin = new Vector2(0.5F, 0);
                    panelRect.anchorMax = new Vector2(0.5F, 0);
                    panelRect.anchoredPosition = new Vector3(0, 150, 0);
                    panelRect.sizeDelta = new Vector2(300, 150);
                    break;
                case LabelPosition.UpperRight:
                    panelRect.anchorMin = new Vector2(1, 1);
                    panelRect.anchorMax = new Vector2(1, 1);
                    panelRect.anchoredPosition = new Vector2(-100, -50);
                    panelRect.sizeDelta = new Vector2(200, 100);
                    break;
            }
            return panel;
        }

        /// <summary>
        /// Creates a new canvas as a container for the label
        /// </summary>
        /// <returns>the created canvas</returns>
        private Canvas CreateCanvas()
        {
            GameObject labelRoot = new GameObject {name = "Label"};
            labelRoot.AddComponent<Canvas>();
            if (Camera.main != null)
            {
                Transform cameraTransform = Camera.main.transform;
                labelRoot.transform.position = cameraTransform.position + cameraTransform.forward * 0.5F;
            }

            Canvas infoCanvas = labelRoot.GetComponent<Canvas>();
            infoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            labelRoot.AddComponent<CanvasScaler>();
            labelRoot.AddComponent<GraphicRaycaster>();
            return infoCanvas;
        }
    }
}