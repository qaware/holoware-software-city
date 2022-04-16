using System.Linq;
using UnityEngine;

namespace SoftwareCities.unityadapter
{
    public class MouseClick : MonoBehaviour
    {
        private GameObject labelGo;

        private LabelCreator labelCreator = LabelCreator.ForClassNames();
        
        public delegate void ComponentClick(string componentName);

        public static event ComponentClick OnComponentClick;
        
        public delegate void ComponentRelease();

        public static event ComponentRelease OnComponentRelease;

        /// <summary>
        /// Show the class label when the mouse is pressed on a gameObject (class or package)
        /// </summary>
        private void OnMouseDown()
        {
            string componentName = gameObject.transform.parent.gameObject.name.Split('.').Last();
            labelGo = labelCreator.CreateLabel(componentName);
            OnComponentClick?.Invoke(componentName);
        }

        /// <summary>
        /// Destroy the class label when the mouse is raised 
        /// </summary>
        private void OnMouseUp()
        {
            Destroy(labelGo);
            OnComponentRelease?.Invoke();
        }
    }
}