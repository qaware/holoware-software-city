using UnityEngine;

namespace SoftwareCities.unityadapter.views
{
    public class UseCaseEventManager : MonoBehaviour
    {
        public delegate void NextUseCase();

        public static event NextUseCase OnNextUseCase;

        public delegate void PreviousUseCase();

        public static event PreviousUseCase OnPreviousUseCase;

        public delegate void NextPOI();

        public static event NextPOI OnNextPOI;

        public delegate void PreviousPOI();

        public static event PreviousPOI OnPreviousPOI;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                OnNextPOI?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                OnPreviousPOI?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                OnNextUseCase?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                OnPreviousUseCase?.Invoke();
            }
        }
    }
}