using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bat : MonoBehaviour
{
    [SerializeField] private Transform topBatPos;
    private Vector3 startPos = Vector3.zero;

    private FarNearGrab _farNearGrab;

    private float currentSwingSpeed;
    private bool isSwing = false;

    private void Start()
    {
        _farNearGrab = GetComponent<FarNearGrab>();
    }

    void Update()
    {
        float dis = Vector3.Distance(topBatPos.position, startPos);
        currentSwingSpeed = dis / Time.deltaTime;
        // Debug.Log(IsSwing());
        // if (currentSwingSpeed >= 0.01f)
        // {
        //     Debug.Log(currentSwingSpeed+ " distance :" + dis);
        // }
        startPos = topBatPos.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        isSwing = true;
    }

    private void OnTriggerExit(Collider other)
    {
        isSwing = false;
    }

    public bool IsSwing()
    {
        return isSwing;
    }
    public float GetSwingSpeed()
    {
        return currentSwingSpeed;
    }

    public void Vibrate()
    {
        //_farNearGrab.leftHandGrip
    }
}
