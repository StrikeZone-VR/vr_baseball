using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FadeController : MonoBehaviour
{
    [SerializeField] private FadeChannelSO _fadeChannelSO;
    [SerializeField] private Image _imageComponent;
    
    private void OnEnable()
    {
        _fadeChannelSO.OnEventRaised += InitiateFade;
    }

    private void OnDisable()
    {
        _fadeChannelSO.OnEventRaised -= InitiateFade;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            _fadeChannelSO.FadeIn(1.0f);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            _fadeChannelSO.FadeOut(1.0f);
        }
    }

    private void InitiateFade(bool fadeIn, float duration, Color desiredColor)
    {
        _imageComponent.DOBlendableColor(desiredColor, duration);
    }
}
