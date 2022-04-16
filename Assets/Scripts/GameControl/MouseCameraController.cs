using SoftwareCities.unityadapter.views;
using UnityEngine;

namespace GameControl
{
    public class MouseCameraController : MonoBehaviour
    {
        private Camera camera;


        [Header("Movement Settings")] [Tooltip("Exponential boost factor on translation.")]
        public float boost = 3.5f;

        [Header("Rotation Settings")]
        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve =
            new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        void Awake()
        {
            camera = Camera.main;
        }

        public void ViewToRoot()
        {
            GameObject root = GameObject.Find("root");
            if (root == null) return;
            Transform cuboid = root.transform.GetChild(root.transform.childCount-1); // The base cuboid is always the last child of root

            gameObject.transform.position = cuboid.position;
            gameObject.transform.rotation = Quaternion.Euler(45, 75, 0);

            gameObject.transform.position += transform.forward * -1.1f * cuboid.position.z;
        }

        private bool drag = false;
        private Vector3 dragOrigin;
        private Vector3 dragDiff;
        private Transform pivot;
        float zOffset = 100f;
        private void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            if (Input.GetKey(KeyCode.R) || View.reset)
            {
                ViewToRoot();
                View.reset = false;
            }
            
            // Rotate
            if (Input.GetMouseButton(1))
            {
                var mouseMovement =
                    new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * -1);

                var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);
                
                // rotate root around GameObject.Find("Pivot").transform.position
                if (pivot == null)
                {
                    pivot = GameObject.Find("Pivot").transform;
                }
                gameObject.transform.RotateAround(pivot.position, pivot.up, mouseMovement.x* mouseSensitivityFactor);
                gameObject.transform.RotateAround(pivot.position, gameObject.transform.right, mouseMovement.y* mouseSensitivityFactor);
                
            }

            // Translate
            if (Input.GetMouseButton(2))
            {
                dragDiff = camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zOffset)) - camera.transform.position;
                if (drag == false)
                {
                    drag = true;
                    dragOrigin = camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zOffset));
                }
                else
                {
                    camera.transform.position = dragOrigin - dragDiff;
                }
            }
            else
            {
                drag = false;
            }
            
            // Zoom
            camera.transform.position += Input.mouseScrollDelta.y * 10 * camera.transform.forward;
        }
    }
}