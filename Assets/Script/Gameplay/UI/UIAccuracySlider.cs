using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAccuracySlider : MonoBehaviour
{
    [SerializeField] private Baseball _baseball;
    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void ChangedValue()
    {
        _baseball.Ball_Accuracy_Weight = slider.value;
    }
}
