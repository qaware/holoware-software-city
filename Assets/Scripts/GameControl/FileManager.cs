using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameControl
{
    public class FileManager : MonoBehaviour
    {
        private InputField inputField;
        public void Awake()
        {
            inputField = gameObject.GetComponentInChildren<InputField>();
        }

        public void OpenFileBrowser()
        {
            string path = EditorUtility.OpenFilePanel("Choose file", Directory.GetCurrentDirectory(), "dot");
            inputField.text = path;
        }

        public void OpenFolderBrowser()
        {
            string path = EditorUtility.OpenFolderPanel("Choose folder", Directory.GetCurrentDirectory(), "");
            inputField.text = path;
        }
    }
}