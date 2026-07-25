using UnityEngine;
using UnityEngine.UI;

public class UISpeedWeightSlider : MonoBehaviour
{
    [SerializeField] private BaseballPhysics _physics;
    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void ChangedValue()
    {
        _physics.SpeedWeight = slider.value;
    }
}
