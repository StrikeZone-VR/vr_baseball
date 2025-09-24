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
//        Debug.Log(IsSwing());
        // if (currentSwingSpeed >= 0.01f)
        // {
        //     Debug.Log(currentSwingSpeed+ " distance :" + dis);
        // }
        startPos = topBatPos.position;
    }

    public bool IsSwing()
    {
        if (transform.localEulerAngles.y <= 100)
        {
            return true;
        }
        return false;
    }
    public float GetSwingSpeed()
    {
        return currentSwingSpeed;
    }
}
