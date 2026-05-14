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

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        playAudioEvent.onEventRaised += PlayAudioClip;
    }

    private void OnDisable()
    {
        playAudioEvent.onEventRaised -= PlayAudioClip;
    }

    //(click, throw) hit start strike swing
    private void PlayAudioClip(int index)
    {
        audioSource.PlayOneShot(audioClips[index]);
    }

    public void SetVolume(float volume)
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.volume = Mathf.Clamp01(volume);
    }

    public float GetVolume()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        return audioSource != null ? audioSource.volume : 1f;
    }
}
