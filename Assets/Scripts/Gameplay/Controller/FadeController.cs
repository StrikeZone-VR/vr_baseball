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
    }

    private void InitiateFade(bool fadeIn, float duration, Color desiredColor)
    {
        _imageComponent.DOBlendableColor(desiredColor, duration);
    }
}
