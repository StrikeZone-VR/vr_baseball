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
    
    const float rotationTime = 1.0f; //0.25
    const float AXIS_DISTANCE = 0.5f;
    float elapsed = 0f;
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Start()
    {
        _farNearGrab = GetComponent<FarNearGrab>();
    }

    void Update()
    {
        //todo : 바꾸자

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
        //스윙 진행중
        if(elapsed != 0) return;

        //index
        //방망이 거리 = 0.8 => 안해도 될듯
        //x = 0.83 + 0.33 * (index % 3);
        //y = 0.33 * (index % 3);
        //   y/x => tangent
        // 0 : 각도0
        // ㅁㅁㅁ => 0
        // 

        isSwing = true;
        StartCoroutine(Swing());
        //StartCoroutine(RotateWithCurveSwing(start, new Vector3(-65, -135, -120)));
    }
    IEnumerator Swing()
    {
        Quaternion start_rotation;
        Quaternion current_rotation;
        Quaternion end_rotation;

        Vector3 start_pos;
        Vector3 current_pos;
        Vector3 end_pos;

        float prevCurve = 0f;
        float totalOrbitAngle = -135f; // 공전 각도 (원하는 값으로) //270


        Vector3 xWorld = axis.transform.TransformDirection(Vector3.right);
        Vector3 zWorld = axis.transform.TransformDirection(Vector3.forward);

        float batAngle = -45.0f; //y

        Vector3 pivot = axis.transform.position;
        Vector3 orbitYAxis = axis.transform.up; // axis의 로컬 Y축을 월드로 
        Vector3 orbitZAxis = axis.transform.forward;

        //zWorld * sin, xWorld * cos
        start_pos = axis.transform.position 
            + xWorld * Mathf.Cos(batAngle) * AXIS_DISTANCE
            + zWorld * -Mathf.Sin(batAngle) * AXIS_DISTANCE;

        //zWorld * sin, xWorld * cos
        end_pos = axis.transform.position
            + xWorld * Mathf.Sin(batAngle + totalOrbitAngle) * AXIS_DISTANCE
            + zWorld * Mathf.Cos(batAngle + totalOrbitAngle) * AXIS_DISTANCE;

        Debug.Log(batAngle + totalOrbitAngle);

        start_rotation = Quaternion.AngleAxis(batAngle, orbitYAxis);
        end_rotation = Quaternion.AngleAxis(batAngle + totalOrbitAngle, orbitYAxis);

        Quaternion zRotateQuaternion = Quaternion.AngleAxis(-90f, orbitZAxis);
        start_rotation *= zRotateQuaternion; //기울어라 => 계산 순서는 -90 -45
        end_rotation *= zRotateQuaternion; //기울어라 => 계산 순서는 -90 -45

        transform.position = start_pos;
        transform.localRotation = start_rotation;  //rotation
        //Quaternion end = Quaternion.AngleAxis(180f, axis.transform.up) * start; 

        while (elapsed < rotationTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / rotationTime;
            
            ////각도만 추가하자
            //batAngle = -45.0f + totalOrbitAngle * progress;

            ////pos
            //current_pos = axis.transform.position
            //    + xWorld * Mathf.Cos(batAngle) * AXIS_DISTANCE
            //    + zWorld * Mathf.Sin(-batAngle) * AXIS_DISTANCE;
            
            ////rotation
            //current_rotation = Quaternion.AngleAxis(batAngle, orbitYAxis);
            //current_rotation *= zRotateQuaternion; //기울어라 => 계산 순서는 -90 -45

            //transform.position = current_pos;
            //transform.localRotation = current_rotation;
            yield return null;
        }

        Debug.Log("엄준식");
        isSwing = false;
        elapsed = 0;
        transform.position = end_pos;
        transform.localRotation = end_rotation;
    }
}
