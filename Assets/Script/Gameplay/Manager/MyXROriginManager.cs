using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
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
    [Tooltip("스윙 공전 축. 비워두면 XROrigin의 카메라를 사용한다.")]
    [SerializeField] private Transform swingAxis;
    [SerializeField] private float axisDistance = 0.5f;
    [SerializeField] private float swingStartAngle = -45f;
    [SerializeField] private float swingTotalOrbitAngle = -270f;
    [SerializeField] private float swingDuration = 0.25f;
    [SerializeField] private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool swingActive;
    
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
    /// 컨트롤러(rightHand)를 swingAxis(없으면 카메라)를 중심으로 공전시킨다.
    /// Bat.cs의 Swing 코루틴과 동일한 cos/sin 공전식이지만,
    /// 컨트롤러는 ActionBasedController / TrackedPoseDriver가 매 프레임 포즈를 덮어쓰므로
    /// 스윙 동안만 그 컴포넌트들을 비활성화해서 transform 강제 적용을 가능하게 한다.
    /// </summary>
    private IEnumerator DebugDoSwing()
    {
        //일단 이거 계속 잘 안되가지고 스윙 다시 깊게 볼듯함
        //배트스윙을 참고하자
        if (swingActive || rightHand == null) yield break;
        swingActive = true;

        Transform pivot = swingAxis != null
            ? swingAxis
            : (_origin != null && _origin.Camera != null ? _origin.Camera.transform : transform);

        // 컨트롤러 포즈를 매 프레임 덮어쓰는 컴포넌트들 잠시 끄기
        List<Behaviour> suppressed = SuppressHandTrackers(rightHand);

        Vector3 xWorld = pivot.TransformDirection(Vector3.right);
        Vector3 zWorld = pivot.TransformDirection(Vector3.forward);
        Vector3 orbitYAxis = pivot.up;
        Vector3 orbitZAxis = pivot.forward;

        Quaternion zRotateQuaternion = Quaternion.AngleAxis(pivot.localEulerAngles.z - 90f, orbitZAxis);

        float elapsed = 0f;
        try
        {
            while (elapsed < swingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / swingDuration);
                float progress = swingCurve.Evaluate(t);
                float angle = swingStartAngle + swingTotalOrbitAngle * progress;

                Vector3 pos = pivot.position
                    + xWorld * (Mathf.Cos(angle * Mathf.Deg2Rad) * axisDistance)
                    + zWorld * (-Mathf.Sin(angle * Mathf.Deg2Rad) * axisDistance);

                Quaternion rot = Quaternion.AngleAxis(angle, orbitYAxis) * zRotateQuaternion;

                rightHand.SetPositionAndRotation(pos, rot);
                yield return null;
            }
        }
        finally
        {
            RestoreTrackers(suppressed);
            swingActive = false;
        }
    }

    private static List<Behaviour> SuppressHandTrackers(Transform hand)
    {
        var list = new List<Behaviour>();
        CollectAndDisable<ActionBasedController>(hand, list);
        CollectAndDisable<TrackedPoseDriver>(hand, list);
        return list;
    }

    private static void CollectAndDisable<T>(Transform hand, List<Behaviour> sink) where T : Behaviour
    {
        foreach (var c in hand.GetComponentsInParent<T>(true))
        {
            if (c.enabled) { c.enabled = false; sink.Add(c); }
        }
        foreach (var c in hand.GetComponentsInChildren<T>(true))
        {
            if (c.enabled) { c.enabled = false; sink.Add(c); }
        }
    }

    private static void RestoreTrackers(List<Behaviour> suppressed)
    {
        for (int i = 0; i < suppressed.Count; i++)
        {
            if (suppressed[i] != null) suppressed[i].enabled = true;
        }
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
