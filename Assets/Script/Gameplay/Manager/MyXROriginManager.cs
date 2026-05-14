using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class MyXROriginManager : MonoBehaviour
{
    [SerializeField] private Vector3EventSO moveOriginEvent;
    [SerializeField] private Vector3EventSO rotateOriginEvent;
    [SerializeField] private MyBodyEventSO bodyEvent;
    [SerializeField] private BoolEventSO setPlayerMoveMode;

    [Header("플레이어 이동 컨트롤러")]
    [SerializeField] private ActionBasedContinuousMoveProvider moveProvider;
    [SerializeField] private XROrigin _origin;
    [SerializeField] private Transform rightHand;

    [Header("Debug Swing")]
    [SerializeField] private Vector3 swingStartEuler = new Vector3(-30f, 0f, 0f);
    [SerializeField] private Vector3 swingEndEuler   = new Vector3( 20f, 0f, 0f);
    [SerializeField] private float   swingDuration   = 0.25f;
    [SerializeField] private AnimationCurve swingEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool swingActive;
    private Quaternion currentSwingRot = Quaternion.identity;
    
    private void OnEnable()
    {
        moveOriginEvent.onEventRaised += MoveOrigin;
        rotateOriginEvent.onEventRaised += RotateOrigin;
        bodyEvent.onEventRaised += SetPlayer;
        setPlayerMoveMode.onEventRaised += SetPlayerMoveMode;
    }
    private void OnDisable()
    {
        moveOriginEvent.onEventRaised -= MoveOrigin;
        rotateOriginEvent.onEventRaised -= RotateOrigin;
        bodyEvent.onEventRaised -= SetPlayer;
        setPlayerMoveMode.onEventRaised -= SetPlayerMoveMode;
    }

    private void Update()
    {
        //키입력 (New Input System)
        if (Keyboard.current != null && Keyboard.current[Key.Z].wasPressedThisFrame)
        {
            Debug.Log("Z 클릭 - 팔 휘두르기");
            StartCoroutine(DebugDoSwing());
        }
    }

    /// <summary>
    /// 컨트롤러를 카메라(=머리) 위치를 중심으로 시작 각도→끝 각도까지 공전시킨다.
    /// 던지는 모션을 흉내내는 디버그 함수.
    /// </summary>
    private IEnumerator DebugDoSwing()
    {
        //일단 이거 계속 잘 안되가지고 스윙 다시 깊게 볼듯함
        
        yield return null; 
    }

    private void MoveOrigin(Vector3 vector3)
    {
        //move
        _origin.MoveCameraToWorldLocation(vector3);
        
    }
    private void RotateOrigin(Vector3 vector3)
    {
        _origin.MatchOriginUpCameraForward(Vector3.up, vector3);
    }

    private void SetPlayer(MyBody body)
    {
        body.SetCamera(_origin.Camera);
    }
    
    private void SetPlayerMoveMode(bool isMove)
    {
        moveProvider.enabled = isMove;
    } 
    
}
