using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Bat : MonoBehaviour
{
    [SerializeField] private Transform topBatPos;
    private Vector3 startPos = Vector3.zero;

    private FarNearGrab _farNearGrab;

    private float currentSwingSpeed;
    private bool isSwing = false;

    private float swingDuration = 0.125f; // 스윙 지속 시간
    private float swingAngle = -360f; // 스윙 각도
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
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(SwingBat());
        }
        //중력무시
        if (Input.GetKeyDown(KeyCode.L))
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.useGravity = false;
            rb.freezeRotation = true;
            transform.position = new Vector3(0.297f, 0.92f, -0.832f);
            transform.rotation = Quaternion.Euler(-60f, 0f, 0f);
            //회전값
        }
        //중력무시
        if (Input.GetKeyDown(KeyCode.M))
        {
            swingAngle *= -1;
        }
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
        List<XRBaseController> controllers = _farNearGrab.GetController();
        for (int i = 0; i < controllers.Count; i++)
        {
            controllers[i].SendHapticImpulse(0.7f, 0.1f);
        }
    }

    public void Swing()
    {
        isSwing = true;
        //StartCoroutine();
    }
    
    IEnumerator SwingBat()
    {
        isSwing = true;
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 0, swingAngle);
    
        // 스윙 전진
        float elapsedTime = 0f;
        while (elapsedTime < swingDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / swingDuration;
            Quaternion swingRotation = startRotation * Quaternion.Euler(0, 0, t * swingAngle);

            transform.rotation = swingRotation; 
            yield return null;
        }
    
        transform.rotation = endRotation;
        isSwing = false;
    }
}
