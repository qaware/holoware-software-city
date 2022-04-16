using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameControl
{
    public class MenuController : MonoBehaviour
    {
        private GameObject menu;
        private void Start()
        {
            menu = GameObject.Find("MenuCanvas");
            Button button = GameObject.Find("ButtonClose").GetComponent<Button>();
            button.onClick.AddListener(DeactivateMenu);
        }

        private void DeactivateMenu()
        {
            menu.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                menu.SetActive(!menu.activeInHierarchy);
            }
        }

        public void ApplyChanges()
        {
            Debug.LogWarning("TODO: \"Apply changes\" is not implemented yet");
        }
    }
    
}