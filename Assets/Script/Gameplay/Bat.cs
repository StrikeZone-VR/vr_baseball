using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Bat : MonoBehaviour
{
    [SerializeField] private Transform topBatPos;
    [SerializeField] private Transform axis;
    private Vector3 startPos = Vector3.zero;

    private FarNearGrab _farNearGrab;

    private float currentSwingSpeed;
    private bool isSwing = false;

    private float swingDuration = 0.125f; // 스윙 지속 시간
    private float swingAngle = -360f; // 스윙 각도
    
    const float rotationTime = 0.25f;
    float elapsed = 0f;
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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
        List<XRBaseController> controllers = _farNearGrab.GetController();
        for (int i = 0; i < controllers.Count; i++)
        {
            controllers[i].SendHapticImpulse(0.7f, 0.1f);
        }
    }
    
    public void StartSwing()
    {
        if(elapsed != 0) return;
        
        isSwing = true;
        StartCoroutine(Swing());
        //StartCoroutine(RotateWithCurveSwing(start, new Vector3(-65, -135, -120)));
    }
    IEnumerator Swing()
    {
        Vector3 xWorld = axis.transform.TransformDirection(Vector3.right);
        
        Vector3 pivot = axis.transform.position;
        Vector3 orbitAxis = axis.transform.up; // axis의 로컬 Y축을 월드로 

        transform.position = axis.transform.position+ xWorld * 0.5f;
        transform.localRotation = Quaternion.Euler(0,0,-90f); //기울어라
        
        float prevCurve = 0f;
        float totalOrbitAngle = -270f; // 공전 각도 (원하는 값으로)
        
        Quaternion start = transform.localRotation; 
        //Quaternion end = Quaternion.AngleAxis(180f, axis.transform.up) * start; 

        while (elapsed < rotationTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / rotationTime;
            float curveValue = rotationCurve.Evaluate(progress);
            
            // 1) 공전: 커브의 증가분만큼 RotateAround
            float deltaCurve = curveValue - prevCurve;
            float deltaAngle = deltaCurve * totalOrbitAngle;
            prevCurve = curveValue;
            
            transform.RotateAround(pivot, orbitAxis, deltaAngle);
            //transform.localRotation = Quaternion.Slerp(start, end, curveValue); ; // 회전 누적
            yield return null;
        }
        
        isSwing = false;
        elapsed = 0;
        //transform.rotation = endRot;
        //transform.localRotation = end;
    }
}
