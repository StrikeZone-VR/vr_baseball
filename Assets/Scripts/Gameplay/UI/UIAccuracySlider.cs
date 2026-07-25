using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAccuracySlider : MonoBehaviour
{
    [SerializeField] private BaseballPhysics _physics;
    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void ChangedValue()
    {
        _physics.Ball_Accuracy_Weight = slider.value;
    }
}
