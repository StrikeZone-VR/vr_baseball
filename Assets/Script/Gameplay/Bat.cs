using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

public class Bat : MonoBehaviour
{
    [SerializeField] private Transform topBatPos;
    [SerializeField] private Transform axis;
    [SerializeField] private StrikeZone _strikeZone;
    private Vector3 startPos = Vector3.zero;

    private FarNearGrab _farNearGrab;
    private Rigidbody _rigidbody;

    private float currentSwingSpeed;
    private bool isSwing = false;

    public float ROTATION_TIME = 0.125f; //0.25
    const float AXIS_DISTANCE = 0.5f;
    float elapsed = 0f;

    float startBatAngle = -45f;
    float totalOrbitAngle = -270f; // 공전 각도 (원하는 값으로) //270

    
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Start()
    {
        _farNearGrab = GetComponent<FarNearGrab>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    
    void FixedUpdate()
    {
        //todo : 바꾸자
        float dis = Vector3.Distance(topBatPos.position, startPos);
        currentSwingSpeed = dis / Time.deltaTime;

        startPos = topBatPos.position;
    }

    
    //근데 이러면 모든 트리거 발동아닌가?
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SwingZone"))
        {
            isSwing = true;
        }

        // if (other.CompareTag("BallZone") || other.CompareTag("StrikeZone"))
        // {
        //     Debug.Log("너의 위치는 " + transform.position);
        // }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SwingZone"))
        {
            isSwing = false;
        }
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
        
        //Debug.LogWarning("Swing. 그러나 임시로 막아둠");
        StartCoroutine(Swing());
    }
    IEnumerator Swing()
    {
        bool debugCheck = true;
        Quaternion start_rotation;
        Quaternion current_rotation;
        Quaternion end_rotation;

        Vector3 start_pos;
        Vector3 current_pos;
        Vector3 end_pos;


        Vector3 xWorld = axis.transform.TransformDirection(Vector3.right);
        Vector3 zWorld = axis.transform.TransformDirection(Vector3.forward);
        
        //float batAngle = -45.0f; //y

        Vector3 orbitYAxis = axis.transform.up; // axis의 로컬 Y축을 월드로 
        Vector3 orbitZAxis = axis.transform.forward;

        //zWorld * sin, xWorld * cos
        start_pos = axis.transform.position 
            + xWorld * (Mathf.Cos(startBatAngle * Mathf.Deg2Rad) * AXIS_DISTANCE)
            + zWorld * (-Mathf.Sin(startBatAngle * Mathf.Deg2Rad) * AXIS_DISTANCE);

        //zWorld * sin, xWorld * cos
        end_pos = axis.transform.position
            + xWorld * (Mathf.Cos((startBatAngle + totalOrbitAngle) * Mathf.Deg2Rad) * AXIS_DISTANCE)
            + zWorld * (-Mathf.Sin((startBatAngle + totalOrbitAngle) * Mathf.Deg2Rad) * AXIS_DISTANCE);

        start_rotation = Quaternion.AngleAxis(startBatAngle, orbitYAxis);
        end_rotation = Quaternion.AngleAxis(startBatAngle + totalOrbitAngle, orbitYAxis);

        //z축 기준으로 90도
        Quaternion zRotateQuaternion = Quaternion.AngleAxis(axis.localEulerAngles.z -90f, orbitZAxis);
        start_rotation *= zRotateQuaternion;
        end_rotation *= zRotateQuaternion; 

        _rigidbody.transform.position = start_pos;
        _rigidbody.transform.localRotation = start_rotation;  //rotation
        //Quaternion end = Quaternion.AngleAxis(180f, axis.transform.up) * start; 

        while (elapsed < ROTATION_TIME)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / ROTATION_TIME;
            
            ////각도만 추가하자
            float batAngle = startBatAngle + totalOrbitAngle * progress;

            if (progress >= 0.5f && debugCheck)
            {
                debugCheck = false;
                //Debug.Break();
            }
            
            //Debug.Log("hit time : (" +Time.time + ") : " + batAngle);
            // if (-190f <= batAngle && batAngle <= -170f)
            // {
            //     Debug.Log("real hit time : (" +Time.time + ")"+ batAngle + "도");
            // }
            
            //pos
            current_pos = axis.transform.position
                + xWorld * (Mathf.Cos(batAngle * Mathf.Deg2Rad) * AXIS_DISTANCE)
                + zWorld * (-Mathf.Sin(batAngle * Mathf.Deg2Rad) * AXIS_DISTANCE);
            
            //rotation
            current_rotation = Quaternion.AngleAxis(batAngle, orbitYAxis);
            current_rotation *= zRotateQuaternion; //기울어라 => 계산 순서는 -90 -45

            _rigidbody.transform.position = current_pos;
            _rigidbody.transform.localRotation = current_rotation;
            yield return null;
        }

        isSwing = false;
        elapsed = 0;
        _rigidbody.transform.position = end_pos;
        _rigidbody.transform.localRotation = end_rotation;
    }

    public float RotationTime
    {
        get => ROTATION_TIME;
    }


    public void MoveAxis(float angle)
    {
        axis.transform.localEulerAngles = new Vector3(
            axis.transform.localEulerAngles.x, 
            axis.transform.localEulerAngles.y, 
            angle
        );
        
        Vector3 start_pos;
        Quaternion start_rotation;
        
        Vector3 xWorld = axis.transform.TransformDirection(Vector3.right);
        Vector3 zWorld = axis.transform.TransformDirection(Vector3.forward);
        Vector3 orbitYAxis = axis.transform.up; // axis의 로컬 Y축을 월드로 

        start_rotation = Quaternion.AngleAxis(startBatAngle, orbitYAxis);
        Vector3 orbitZAxis = axis.transform.forward;

        start_pos = axis.transform.position 
                    + xWorld * (Mathf.Cos(startBatAngle * Mathf.Deg2Rad) * AXIS_DISTANCE)
                    + zWorld * (-Mathf.Sin(startBatAngle * Mathf.Deg2Rad) * AXIS_DISTANCE);
        
        transform.position = start_pos;
        
        //z축 기준으로 90도
        Quaternion zRotateQuaternion = Quaternion.AngleAxis(axis.localEulerAngles.z -90f, orbitZAxis);
        start_rotation *= zRotateQuaternion;
        transform.localRotation = start_rotation;  //rotation
    }

    //**핵심 함수**
    //공 오는 각도에 맞춰서 계산
    public void MoveAxis(Vector3 targetPosition)
    {
        float x = Vector3.Distance(axis.transform.position, new Vector3(targetPosition.x, axis.transform.position.y, targetPosition.z));
        float y = axis.transform.position.y - targetPosition.y; //음수 반대로 해야 원하는 대로 나옴
        
        // 1. 라디안(Radian) 값으로 각도 구하기 (주의: y가 먼저 들어갑니다!)
        float radian = Mathf.Atan2(y, x); 

        // 2. 우리가 흔히 아는 360도(Degree) 체계로 변환하기
        float angle = radian * Mathf.Rad2Deg;
        //Debug.Log(" [AI Batter] - x : " + x + ", y : " + y +  ",각도 : " + angle);
        MoveAxis(angle);
        //기울기만큼 angle 수정
    }
}
