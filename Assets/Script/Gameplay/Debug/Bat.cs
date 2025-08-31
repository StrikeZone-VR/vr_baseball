using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bat : MonoBehaviour
{
    [SerializeField] private Transform topBatPos;
    private Vector3 startPos = Vector3.zero;

    private float currentSwingSpeed;

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float dis = Vector3.Distance(topBatPos.position, startPos);
        currentSwingSpeed = dis / Time.deltaTime;
        // if (currentSwingSpeed >= 0.01f)
        // {
        //     Debug.Log(currentSwingSpeed+ " distance :" + dis);
        // }
        startPos = topBatPos.position;
    }

    public float GetSwingSpeed()
    {
        return currentSwingSpeed;
    }

    public void GrabBat()
    {
        Debug.Log("잡혔어!");
    }
}
