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
    [Tooltip("리플레이 녹화용(선택). 비워두면 왼손 고스트는 생략된다.")]
    [SerializeField] private Transform leftHand;

    //리플레이 시스템(다른 씬의 GamePlay)이 런타임에 손/머리 카메라를 찾아갈 수 있게 노출한다.
    public Transform RightHand => rightHand;
    public Transform LeftHand => leftHand;
    public Camera HeadCamera => _origin != null ? _origin.Camera : null;
    //타석 전환(GamePlayManager.BatterBox)이 왼손 조이스틱 값을 읽어갈 수 있게 노출한다.
    public ActionBasedContinuousMoveProvider MoveProvider => moveProvider;

    [Header("눈높이 캘리브레이션")]
    [Tooltip("X버튼을 누르면 현재 머리 높이를 이 눈높이로 재정렬한다. 게임 로직 가정(player_y)과 같은 1.7m 권장.")]
    [SerializeField] private float targetEyeHeight = 1.7f;
    [Tooltip("재정렬 입력. 비워두면 왼손 X버튼(primaryButton) + 에디터용 H키에 자동 바인딩.")]
    [SerializeField] private InputActionReference calibrateEyeHeightAction;

    private const string EyeHeightPrefKey = "settings.eyeHeightOffset"; //SettingsController 키 네이밍과 통일

    [Header("주자 레일 (베이스라인 구속)")]
    [Tooltip("베이스라인에서 좌우로 허용할 반폭(m). 머리 흔들림/실제 걸음은 이 안에서 자유.")]
    [SerializeField] private float railHalfWidth = 0.5f;
    [Tooltip("복도 밖으로 벗어났을 때 다시 끌어오는 속도(m/s). 순간이동 대신 미끄러지듯 보정. 이동속도(4.5)보다 커야 벽처럼 막힌다.")]
    [SerializeField] private float railCorrectionSpeed = 6f;

    private bool _railActive;
    private Vector3 _railA, _railB; //현재 베이스라인 선분(출발 베이스 → 목표 베이스)

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
    private Coroutine moveLockRoutine;

    //적용해야 할 눈높이 오프셋. XROrigin이 0으로 되돌려도 LateUpdate(EnforceEyeHeightOffset)가 이 값으로 복구한다.
    private float _desiredEyeOffset = 0f;

    //이동 잠금 시 locomotionPhase가 Idle로 내려오길 기다리는 최대 시간. (DisableMoveWhenSettled)
    private const float MOVE_LOCK_SETTLE_TIMEOUT = 0.5f;

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

        //눈높이 재정렬 입력 연결. 인스펙터에 액션이 없으면 X버튼(+에디터 H키)으로 자동 바인딩.
        InputAction calibrate = calibrateEyeHeightAction != null ? calibrateEyeHeightAction.action : null;
        if (calibrate == null)
        {
//             _fallbackCalibrateAction = new InputAction("CalibrateEyeHeight", InputActionType.Button,
//                 "<XRController>{LeftHand}/primaryButton");
// #if UNITY_EDITOR
//             _fallbackCalibrateAction.AddBinding("<Keyboard>/h"); //헤드셋 없이 테스트용
// #endif
//             calibrate = _fallbackCalibrateAction;
        }
        calibrate.performed += OnCalibrateEyeHeight;
        calibrate.Enable();
    }
    private void OnDisable()
    {
        //InputAction calibrate = calibrateEyeHeightAction != null ? calibrateEyeHeightAction.action : _fallbackCalibrateAction;
        InputAction calibrate = calibrateEyeHeightAction;
        if (calibrateEyeHeightAction != null)
        {
            calibrate.performed -= OnCalibrateEyeHeight;
            //if (calibrate == _fallbackCalibrateAction) calibrate.Disable(); //에셋 참조 액션은 다른 곳에서 쓸 수 있으니 끄지 않음
        }

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
        MoveOriginCore(vector3, releaseHeld: true);
    }

    //좌/우 타석 전환처럼 배트를 쥔 채로 이동해야 할 때 쓰는 공개 버전(selection 해제 없음).
    public void MoveOriginKeepingHeld(Vector3 vector3)
    {
        MoveOriginCore(vector3, releaseHeld: false);
    }

    private void MoveOriginCore(Vector3 vector3, bool releaseHeld)
    {
        //텔레포트 전에 손에 쥔 것(배트 등) 놓기.
        //안 놓으면 배트를 잡은 채 이닝 교체/시점 전환되면 배트가 손에 select된 채로 플레이어를 따라 끌려온다.
        if (releaseHeld)
            ReleaseRightHandSelection();

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

    // ===== 눈높이 캘리브레이션 ==============================================
    //X버튼을 누른 순간의 머리 높이를 targetEyeHeight로 재정렬한다.
    //원리: Floor 트래킹에서 카메라 높이 = 실제 키 그대로 → CameraFloorOffsetObject의 Y로
    //      (목표 눈높이 - 측정 눈높이)만큼 보정. 앉아서 눌러도 선 눈높이가 된다.
    //저장: PlayerPrefs에 오프셋을 남겨 다음 실행에서도 유지.
    private void Start()
    {
        StartCoroutine(ApplySavedEyeHeightRoutine());
    }

    private IEnumerator ApplySavedEyeHeightRoutine()
    {
        float saved = PlayerPrefs.GetFloat(EyeHeightPrefKey, 0f);
        if (Mathf.Approximately(saved, 0f)) yield break;

        //XROrigin이 Floor 트래킹 초기화 때 floor offset을 0으로 리셋하므로, 그 이후에 적용해야 한다.
        //트래킹이 살아나(카메라가 바닥에서 떨어짐) 초기화가 끝났다고 볼 수 있을 때까지 잠깐 대기.
        float waited = 0f;
        while (waited < 3f)
        {
            Camera cam = _origin != null ? _origin.Camera : null;
            if (cam != null && cam.transform.localPosition.y > 0.2f) break;
            waited += Time.deltaTime;
            yield return null;
        }
        yield return null; //초기화 리셋과의 순서 경합 방지로 한 프레임 더

        SetEyeHeightOffset(saved);
        //Debug.Log($"[EyeHeight] 저장된 눈높이 오프셋 적용: {saved:+0.00;-0.00}m");
    }

    private void OnCalibrateEyeHeight(InputAction.CallbackContext context)
    {
        CalibrateEyeHeight();
    }

    //현재 머리 높이를 측정해 targetEyeHeight가 되도록 floor offset을 조정하고 저장한다.
    public void CalibrateEyeHeight()
    {
        if (_origin == null || _origin.Camera == null) return;
        GameObject floorOffset = _origin.CameraFloorOffsetObject;
        if (floorOffset == null) return;

        float currentOffset = floorOffset.transform.localPosition.y;
        float eyeInOrigin = _origin.CameraInOriginSpaceHeight; //현재 오프셋이 포함된 origin 기준 눈높이
        float measured = eyeInOrigin - currentOffset;          //순수 실제 키(오프셋 제외)
        if (measured < 0.3f) return; //트래킹이 죽어 바닥에 붙어있는 값이면 엉뚱하게 보정되니 무시

        float newOffset = currentOffset + (targetEyeHeight - eyeInOrigin);
        SetEyeHeightOffset(newOffset);

        PlayerPrefs.SetFloat(EyeHeightPrefKey, newOffset);
        PlayerPrefs.Save();
        Debug.Log($"[EyeHeight] 재정렬: 측정 {measured:F2}m → 오프셋 {newOffset:+0.00;-0.00}m (목표 {targetEyeHeight}m)");
    }

    private void SetEyeHeightOffset(float offsetY)
    {
        _desiredEyeOffset = offsetY; //XROrigin이 0으로 되돌려도 LateUpdate가 이 값으로 되돌린다

        GameObject floorOffset = _origin != null ? _origin.CameraFloorOffsetObject : null;
        if (floorOffset == null) return;
        Vector3 lp = floorOffset.transform.localPosition;
        lp.y = offsetY;
        floorOffset.transform.localPosition = lp;
    }

    //눈높이 오프셋은 한 번 넣어두면 끝이 아니다. Floor 트래킹에서 XROrigin.MoveOffsetHeight()는
    //CameraFloorOffsetObject의 Y를 무조건 0으로 되돌리는데(XROrigin.cs, TrackingOriginModeFlags.Floor → MoveOffsetHeight(0f)),
    //이게 XR 런타임이 trackingOriginUpdated를 쏠 때마다(리센터/트래킹 원점 갱신) 다시 불린다.
    //ㄴ ApplySavedEyeHeightRoutine은 Start에서 딱 한 번만 도니까, 한 번 지워지면 영영 안 돌아온다.
    //   → 씬 전환(GameResult 등) 시점에 갑자기 실제 키로 떨어지던 증상. (보정 +0.3m가 날아가면 1.7m가 1.4m처럼 느껴짐)
    //그래서 매 프레임 값이 살아있는지 확인하고 되돌린다. 비용은 float 비교 하나.
    private void EnforceEyeHeightOffset()
    {
        if (Mathf.Approximately(_desiredEyeOffset, 0f)) return; //보정 안 한 상태면 건드릴 것 없음

        GameObject floorOffset = _origin != null ? _origin.CameraFloorOffsetObject : null;
        if (floorOffset == null) return;

        Vector3 lp = floorOffset.transform.localPosition;
        if (Mathf.Abs(lp.y - _desiredEyeOffset) < 0.0001f) return;

        lp.y = _desiredEyeOffset;
        floorOffset.transform.localPosition = lp;
        Debug.Log($"[EyeHeight] 오프셋이 초기화돼 복구함: {_desiredEyeOffset:+0.00;-0.00}m (XROrigin이 Floor 모드에서 0으로 되돌림)");
    }

    // ===== 주자 레일 =====================================================
    //주자 달리기 동안 XR 오리진을 베이스라인(a→b) 복도 안으로 구속한다.
    //조이스틱을 어느 방향으로 밀든 라인 방향 성분만 남고, 수직 이탈은 복도 벽에서 막힌다.
    //GamePlayManager.UpdateRunnerRail이 매 프레임 갱신/해제한다.
    public void SetMoveRail(Vector3 a, Vector3 b)
    {
        _railActive = true;
        _railA = a;
        _railB = b;
    }

    public void ClearMoveRail()
    {
        _railActive = false;
    }

    //move provider가 Update에서 이동시킨 "후"에 보정해야 해서 LateUpdate.
    private void LateUpdate()
    {
        //레일보다 먼저. 아래 early return에 걸리면 안 된다.
        EnforceEyeHeightOffset();

        if (!_railActive || _origin == null) return;
        Camera cam = _origin.Camera;
        if (cam == null) return;

        Vector3 p = cam.transform.position;

        //선분 A→B에 대한 XZ 최근접점 계산
        Vector3 ab = _railB - _railA;
        ab.y = 0f;
        float abLen = ab.magnitude;
        if (abLen < 0.01f) return;
        Vector3 abn = ab / abLen;

        Vector3 ap = p - _railA;
        ap.y = 0f;
        float along = Mathf.Clamp(Vector3.Dot(ap, abn), 0f, abLen); //선분 앞/뒤로는 못 나감(귀루는 선분 안에서 가능)
        Vector3 closest = _railA + abn * along;

        Vector3 offset = p - closest;
        offset.y = 0f;
        float dist = offset.magnitude;
        if (dist <= railHalfWidth) return; //복도 안 — 자유

        //복도 경계까지만 허용. 초과분은 보정 속도로 미끄러지듯 당긴다(레일 진입 순간 홱 끌리는 것 방지).
        Vector3 target = closest + offset / dist * railHalfWidth;
        Vector3 flatP = new Vector3(p.x, 0f, p.z);
        Vector3 flatT = new Vector3(target.x, 0f, target.z);
        Vector3 corrected = Vector3.MoveTowards(flatP, flatT, railCorrectionSpeed * Time.deltaTime);
        _origin.MoveCameraToWorldLocation(new Vector3(corrected.x, p.y, corrected.z));
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
        if (moveProvider == null) return;

        if (moveLockRoutine != null)
        {
            StopCoroutine(moveLockRoutine);
            moveLockRoutine = null;
        }

        if (isMove)
        {
            moveProvider.enabled = true;
            return;
        }

        if (!moveProvider.enabled) return; //이미 잠김

        moveLockRoutine = StartCoroutine(DisableMoveWhenSettled());
    }

    //이동 중에 provider를 그냥 꺼버리면 안 된다.
    //ㄴ ContinuousMoveProviderBase.Update()가 매 프레임 m_IsMovingXROrigin을 false로 리셋한 뒤
    //   MoveRig가 돌면 true로 세우고, 그 값으로 locomotionPhase를 Moving↔Done↔Idle로 굴린다.
    //   즉 phase를 내려주는 건 Update() 하나뿐이라, 달리는 중(phase == Moving)에 enabled = false를 하면
    //   Update가 멈춰 phase가 Moving에 영구히 고정된다.
    //   → LocomotionVignetteProvider가 계속 '이동 중'으로 보고 TunnelingVignette를 닫아둠
    //     (달리다가 아웃돼서 타석 이동 잠금이 걸리면 주변 시야가 까맣게 남던 버그).
    //   가만히 서서 아웃되면 phase가 이미 Idle이라 멀쩡했던 것도 같은 이유.
    //그래서 Update()가 Moving → Done → Idle 까지 내려온 걸 확인하고 끈다(보통 2프레임).
    private IEnumerator DisableMoveWhenSettled()
    {
        float elapsed = 0f;
        while (moveProvider.locomotionPhase != LocomotionPhase.Idle && elapsed < MOVE_LOCK_SETTLE_TIMEOUT)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        //타임아웃(조이스틱을 계속 밀고 있는 경우)이어도 잠그는 건 잠근다.
        //이땐 phase가 Moving으로 남지만, 다음 SetPlayerMoveMode(true)에서 Update가 재개되며 스스로 풀린다.
        moveProvider.enabled = false;
        moveLockRoutine = null;
    }
    
}
