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
    [SerializeField] private VoidEventSO debugSwingEvent; //BaseballDebugger에서 raise

    [Header("플레이어 이동 컨트롤러")]
    [SerializeField] private ActionBasedContinuousMoveProvider moveProvider;
    [SerializeField] private float playerMoveSpeed = 4.5f; //AI 타자 NavMeshAgent(3.5) 살짝 위. prefab moveSpeed 덮어씀
    [Tooltip("스폰/이동 직후 이 시간 동안만 중력을 '즉시' 적용해 바닥에 안착시킨다. 그 뒤엔 AttemptingMove로 복귀.")]
    [SerializeField] private float groundSettleDuration = 0.5f;
    [SerializeField] private XROrigin _origin;
    [SerializeField] private Transform rightHand;

    [Header("Debug Swing")]
    [Tooltip("스윙 공전 축. 비워두면 XROrigin의 카메라를 사용한다.")]
    [SerializeField] private Transform swingAxis;
    [SerializeField] private float axisDistance = 0.5f;
    [SerializeField] private float swingStartAngle;
    [SerializeField] private float swingTotalOrbitAngle;
    [SerializeField] private float swingDuration = 0.25f;
    [SerializeField] private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("스윙 시간 중 몇 % 지점에서 공을 놓을지 (0=시작 직후, 1=끝)")]
    [SerializeField, Range(0f, 1f)] private float releaseProgress = 0.5f;

    private bool swingActive;
    private Coroutine settleRoutine;

    private void OnEnable()
    {
        moveOriginEvent.onEventRaised += MoveOrigin;
        rotateOriginEvent.onEventRaised += RotateOrigin;
        bodyEvent.onEventRaised += SetPlayer;
        setPlayerMoveMode.onEventRaised += SetPlayerMoveMode;
        if (debugSwingEvent != null) debugSwingEvent.onEventRaised += OnDebugSwingRaised;

        if (moveProvider != null)
        {
            moveProvider.moveSpeed = playerMoveSpeed;
            //평소엔 AttemptingMove 유지. Immediately로 두면 가만히 서 있어도 매 프레임 MoveRig가 호출돼
            //locomotionPhase가 Moving에 고정 → TunnelingVignette가 계속 닫혀(주변 시야 까맣게) 버린다.
            //'떠 있어도 바로 바닥에 안착'은 MoveOrigin 직후 BeginGroundSettle에서 잠깐만 Immediately로 처리한다.
            moveProvider.gravityApplicationMode = ContinuousMoveProviderBase.GravityApplicationMode.AttemptingMove;
        }
    }
    private void OnDisable()
    {
        moveOriginEvent.onEventRaised -= MoveOrigin;
        rotateOriginEvent.onEventRaised -= RotateOrigin;
        bodyEvent.onEventRaised -= SetPlayer;
        setPlayerMoveMode.onEventRaised -= SetPlayerMoveMode;
        if (debugSwingEvent != null) debugSwingEvent.onEventRaised -= OnDebugSwingRaised;
    }

    private void OnDebugSwingRaised()
    {
        StartCoroutine(DebugDoSwing());
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

        // X축(pivot.right) 공전: Y-Z 평면에서 수직 아크
        Vector3 yWorld = pivot.TransformDirection(Vector3.up);
        Vector3 zWorld = pivot.TransformDirection(Vector3.forward);
        Vector3 orbitXAxis = pivot.right;

        Quaternion baseRot = rightHand.rotation; //원래 손 방향을 기준으로 회전

        bool released = false;
        float elapsed = 0f;
        try
        {
            while (elapsed < swingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / swingDuration);
                float progress = swingCurve.Evaluate(t);
                float angle = swingStartAngle + swingTotalOrbitAngle * progress;

                //스윙 진행도가 releaseProgress 넘는 순간 한 번만 손 selection 해제
                //공의 OnRelease가 알아서 ThrowPlayerBall 호출함
                if (!released && t >= releaseProgress)
                {
                    released = true;
                    ReleaseRightHandSelection();
                }

                Vector3 pos = pivot.position
                    + yWorld * (Mathf.Cos(angle * Mathf.Deg2Rad) * axisDistance)
                    + zWorld * (-Mathf.Sin(angle * Mathf.Deg2Rad) * axisDistance);

                //시작각으로부터의 변화량 기준으로 회전 (절대 angle을 쓰면 swingStartAngle 바꿀 때 시작 자세가 같이 틀어짐)
                Quaternion rot = Quaternion.AngleAxis(angle - swingStartAngle, orbitXAxis) * baseRot;

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

    /// <summary>
    /// 오른손 컨트롤러의 XR Interactor가 잡고 있는 모든 것을 SelectExit로 해제.
    /// 잡힌 게 없으면 no-op. 공의 경우 Baseball.OnRelease가 자동으로 ThrowPlayerBall을 호출.
    /// </summary>
    private void ReleaseRightHandSelection()
    {
        if (rightHand == null) return;

        //GetComponentInChildren는 첫 매칭 하나만 반환 → Ray/Direct interactor 중 selection 없는 게 먼저 잡힐 수 있음
        //자식 트리 + 부모 트리 다 훑어서 hasSelection 인 놈을 찾자
        XRBaseInteractor interactor = FindInteractorWithSelection(rightHand);
        if (interactor == null)
        {
            return;
        }

        var manager = interactor.interactionManager;
        if (manager == null) return;

        //역순 순회: SelectExit가 리스트를 수정해도 안전하도록
        var items = interactor.interactablesSelected;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            manager.SelectExit((IXRSelectInteractor)interactor, items[i]);
        }
    }

    //자식과 부모에서 오브젝트 선택한 interactor 찾기
    private static XRBaseInteractor FindInteractorWithSelection(Transform root)
    {
        var children = root.GetComponentsInChildren<XRBaseInteractor>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].hasSelection) return children[i];
        }
        var parents = root.GetComponentsInParent<XRBaseInteractor>(true);
        for (int i = 0; i < parents.Length; i++)
        {
            if (parents[i] != null && parents[i].hasSelection) return parents[i];
        }
        return null;
    }

    private void MoveOrigin(Vector3 vector3)
    {
        //move
        //MoveCameraToWorldLocation은 '카메라(머리)'를 그 좌표에 맞추는 함수라
        //y를 직접 주면 트래킹된 머리 높이와 충돌해 극초반/이후 스폰 높이가 들쭉날쭉(y2/y1)해진다.
        //→ 수평(x,z)만 목표로 맞추고 높이는 현재 카메라 높이를 유지(중력이 발을 바닥에 안착시킴)
        Camera cam = _origin != null ? _origin.Camera : null;
        float keepY = cam != null ? cam.transform.position.y : vector3.y;
        _origin.MoveCameraToWorldLocation(new Vector3(vector3.x, keepY, vector3.z));

        //방금 수평 이동으로 공중에 떴을 수 있으니 잠깐만 중력 즉시 적용해 바닥에 안착시킨다.
        BeginGroundSettle();
    }

    //스폰/이동 직후 짧게만 Immediately로 바꿔 바닥에 안착시키고 다시 AttemptingMove로 복귀.
    //(계속 Immediately면 서 있어도 비네트가 닫히므로 잠깐만)
    private void BeginGroundSettle()
    {
        if (moveProvider == null) return;
        if (settleRoutine != null) StopCoroutine(settleRoutine);
        settleRoutine = StartCoroutine(GroundSettleRoutine());
    }

    private IEnumerator GroundSettleRoutine()
    {
        moveProvider.gravityApplicationMode = ContinuousMoveProviderBase.GravityApplicationMode.Immediately;
        yield return new WaitForSeconds(groundSettleDuration);
        moveProvider.gravityApplicationMode = ContinuousMoveProviderBase.GravityApplicationMode.AttemptingMove;
        settleRoutine = null;
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
