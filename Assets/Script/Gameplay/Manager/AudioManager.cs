using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField] private AudioClip [] audioClips;
    
    [SerializeField] private IntEventSO playAudioEvent;

    private void OnEnable()
    {
        playAudioEvent.onEventRaised += PlayAudioClip;
    }

    private void OnDisable()
    {
        playAudioEvent.onEventRaised -= PlayAudioClip;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    //(click, throw) hit start strike swing
    private void PlayAudioClip(int index)
    {
        audioSource.PlayOneShot(audioClips[index]);
    }
}
