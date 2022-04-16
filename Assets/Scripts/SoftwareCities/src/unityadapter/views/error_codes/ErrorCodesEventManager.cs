using UnityEngine;

namespace SoftwareCities.unityadapter.views
{
    public class ErrorCodesEventManager : MonoBehaviour
    {
        public delegate void NextTimeFrame();

        public static event NextTimeFrame OnNextTimeFrame;

        public delegate void PreviousTimeFrame();

        public static event PreviousTimeFrame OnPreviousTimeFrame;

        private void OnEnable()
        {
            // Default settings? 
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                OnNextTimeFrame?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                OnPreviousTimeFrame?.Invoke();
            }
        }
    }
}