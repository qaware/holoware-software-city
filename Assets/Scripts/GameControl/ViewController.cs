using SoftwareCities.unityadapter;
using SoftwareCities.unityadapter.views;
using UnityEngine;

namespace GameControl
{
    public class ViewController : MonoBehaviour
    {
        public void Update()
        {
            if (UnitySoftwareCity.Instance == null) return;

            CityView view = UnitySoftwareCity.Instance.view;
            CityView newView = view;
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                newView = CityView.Day;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                newView = CityView.Night;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                newView = CityView.ErrorCodes;
            }

            if (newView != view)
            {
                UnitySoftwareCity.Instance.SetView(newView);
            }
        }
    }

}