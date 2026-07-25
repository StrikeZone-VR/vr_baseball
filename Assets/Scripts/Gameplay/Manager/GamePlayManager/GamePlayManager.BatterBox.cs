using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

//좌/우 타석 전환 (타자 모드 전용).
//타석에선 자유 이동이 잠기므로(TranslateBattingView 끝의 SetPlayerMoveMode(false)),
//반대 타석으로 가고 싶으면 왼손 조이스틱을 그쪽으로 튕겨서(flick) 스냅 이동한다.
//- 좌/우는 이산 선택이라 연속 이동보다 판정이 안전하고, 타석 안 미세 조정은 실제 몸 움직임으로 충분.
//- 좌타석 앵커는 따로 두지 않는다: 우타석(rightBatterPositionTemp 평균)을
//  홈플레이트(bases[3])→2루(bases[1]) 중심선에 대해 XZ 반사시켜 계산한다.
public partial class GamePlayManager
{
    const float BOX_SWITCH_THRESHOLD = 0.7f; //플릭 인식 임계값(조이스틱 X)
    const float BOX_SWITCH_RESET = 0.3f;     //중립 복귀 판정. 이 아래로 내려와야 다음 플릭 인식

    private bool _isLeftBatterBox;              //false = 우타석(기본). 다음 타석에도 선택 유지
    private bool _boxSwitchArmed = true;        //플릭 1회 = 전환 1회 (중립 복귀 후 재장전)
    private Coroutine _boxSwitchRoutine;
    private MyXROriginManager _xrOriginManager; //배트 안 놓치는 이동용. 다른 씬(Persistent)이라 런타임 검색

    //Update에서 매 프레임 호출된다.
    private void UpdateBatterBoxSwitch()
    {
#if UNITY_EDITOR
        //헤드셋 없이 테스트: Tab = 반대 타석으로 전환
        if (Input.GetKeyDown(KeyCode.Tab) && CanSwitchBatterBox())
        {
            _isLeftBatterBox = !_isLeftBatterBox;
            _boxSwitchRoutine = StartCoroutine(SwitchBatterBoxRoutine());
            return;
        }
#endif
        ActionBasedContinuousMoveProvider provider = ResolveMoveProvider();
        if (provider == null) return;
        var action = provider.leftHandMoveAction.action;
        if (action == null) return;
        if (!action.enabled) action.Enable(); //이동 잠금(provider off) 중에도 조이스틱 값은 읽어야 하므로

        float x = action.ReadValue<Vector2>().x;

        //중립 복귀 체크: 스틱을 놨다가 다시 밀어야 다음 전환이 된다(꾹 밀고 있어도 1회만)
        if (Mathf.Abs(x) < BOX_SWITCH_RESET)
        {
            _boxSwitchArmed = true;
            return;
        }
        if (!_boxSwitchArmed || Mathf.Abs(x) < BOX_SWITCH_THRESHOLD) return;
        if (!CanSwitchBatterBox()) return;

        //반대 타석이 시야 기준 좌/우 어느 쪽인지 보고, 그쪽으로 민 경우에만 전환
        //(스탠스/서 있는 방향과 무관하게 "보이는 쪽으로 밀면 간다"가 되도록)
        GetBatterBoxPose(!_isLeftBatterBox, out Vector3 otherPos, out _);
        Camera cam = Camera.main;
        bool otherIsRight = cam != null
            ? Vector3.Dot(otherPos - cam.transform.position, cam.transform.right) > 0f
            : _isLeftBatterBox; //카메라 못 찾으면: 우타석에 있으면 반대(좌타석)는 왼쪽이라고 가정
        if ((x > 0f) != otherIsRight) return;

        _boxSwitchArmed = false;
        _isLeftBatterBox = !_isLeftBatterBox;
        _boxSwitchRoutine = StartCoroutine(SwitchBatterBoxRoutine());
    }

    //전환 가능 조건: 타자 모드 + 내가 타석에 서 있음 + 공이 날아오거나 인플레이 중이 아님 + 전환 중 아님
    private bool CanSwitchBatterBox()
    {
        if (_boxSwitchRoutine != null) return false; //페이드 전환 중
        if (gamePlayModel == null || !gamePlayModel.PlayerIsBatterMode()) return false;
        if (myBody == null || currentBatterComponent == null ||
            currentBatterComponent != myBody.GetMyBatterComponent()) return false; //타석에 선 게 나일 때만
        if (_ball != null)
        {
            if (_ball.IsInGamePlay) return false; //타구 인플레이(주자 상황)
            if (_ball.CurrentState == BallState.Pitched ||
                _ball.CurrentState == BallState.Thrown ||
                _ball.CurrentState == BallState.FreeBall) return false; //공이 살아서 날아다니는 중
        }
        return true;
    }

    //페이드 후 반대 타석으로 스냅 이동. TranslateBattingView와 같은 리듬(FadeOut→이동→FadeIn).
    private IEnumerator SwitchBatterBoxRoutine()
    {
        fadeEvent.FadeOut(FADE_WAIT_TIME);
        yield return new WaitForSeconds(FADE_WAIT_TIME);

        GetBatterBoxPose(_isLeftBatterBox, out Vector3 movePosition, out Vector3 rotateVector);
        MovePlayerKeepingHeld(movePosition); //MovePlayer 경로는 배트를 놓쳐버림(MoveOrigin이 selection 해제)
        RotatePlayer(rotateVector);

        fadeEvent.FadeIn(FADE_WAIT_TIME);
        _boxSwitchRoutine = null;
    }

    // ===== 주자 레일 (베이스라인 구속) =====================================
    //달리기 중엔 조이스틱을 정확히 안 밀어도 베이스라인을 따라가도록, XR 오리진을
    //현재 구간(직전 베이스 → 목표 베이스) 복도에 구속한다. 실제 구속은 MyXROriginManager.LateUpdate.
    //Update에서 매 프레임 호출된다.
    private void UpdateRunnerRail()
    {
        if (_xrOriginManager == null)
        {
            _xrOriginManager = FindAnyObjectByType<MyXROriginManager>();
            if (_xrOriginManager == null) return;
        }

        MyBatterComponent mine = myBody != null ? myBody.GetMyBatterComponent() : null;

        //레일 조건: 타자 모드 + 내가 달리는 중 + 타구 인플레이 + 이동 잠금 아님(잠기면 어차피 못 움직임)
        //ㄴ provider.enabled 게이트가 중요: 아웃/득점 후 타석 복귀(TranslateBattingView) 직후엔
        //   IsMove가 true로 남는 경우가 있는데, 복귀 시 이동이 잠기므로 레일이 타석의 플레이어를
        //   베이스라인으로 끌어당기는 사고를 막아준다.
        bool running = gamePlayModel != null && gamePlayModel.PlayerIsBatterMode()
                    && mine != null && mine.IsMove && !mine.IsOut
                    && mine.BaseIndex < bases.Length
                    && _ball != null && _ball.IsInGamePlay;
        if (running)
        {
            ActionBasedContinuousMoveProvider provider = ResolveMoveProvider();
            running = provider != null && provider.enabled;
        }

        if (!running)
        {
            _xrOriginManager.ClearMoveRail();
            return;
        }

        //구간: BaseIndex 0(아직 1루 전)이면 홈(bases[3])→1루, 그 외엔 직전 베이스→다음 베이스.
        //bases = 0:1루 1:2루 2:3루 3:홈. BatterComponent.MoveBase의 목표(bases[base_index])와 동일 규칙.
        Vector3 a = mine.BaseIndex == 0 ? bases[3].position : bases[mine.BaseIndex - 1].position;
        Vector3 b = bases[mine.BaseIndex].position;
        _xrOriginManager.SetMoveRail(a, b);
    }

    //조이스틱 액션을 들고 있는 move provider를 찾는다.
    //GamePlayManager의 moveProvider(debug용)가 비어 있으면 MyXROriginManager 것으로 폴백.
    private ActionBasedContinuousMoveProvider ResolveMoveProvider()
    {
        if (moveProvider != null) return moveProvider;
        if (_xrOriginManager == null) _xrOriginManager = FindAnyObjectByType<MyXROriginManager>();
        return _xrOriginManager != null ? _xrOriginManager.MoveProvider : null;
    }

    //배트를 쥔 채로 XR 오리진만 이동. (moveOriginEvent 경로는 손 selection을 강제 해제한다)
    private void MovePlayerKeepingHeld(Vector3 position)
    {
        if (_xrOriginManager == null) _xrOriginManager = FindAnyObjectByType<MyXROriginManager>();
        if (_xrOriginManager != null)
            _xrOriginManager.MoveOriginKeepingHeld(position);
        else
            MovePlayer(position); //못 찾으면 기존 경로(배트는 놓치지만 이동은 된다)
    }

    //좌/우 타석의 위치·시선 계산.
    //우타석 = rightBatterPositionTemp 4개 평균 (기존 TranslateBattingView 계산과 동일).
    //좌타석 = 우타석을 홈(bases[3])→2루(bases[1]) 중심선에 대해 XZ 평면 반사.
    private void GetBatterBoxPose(bool leftBox, out Vector3 position, out Vector3 forward)
    {
        Vector3 rightPos = Vector3.zero;
        for (int i = 0; i < rightBatterPositionTemp.Length; i++)
        {
            rightPos += rightBatterPositionTemp[i].position;
        }
        rightPos /= rightBatterPositionTemp.Length;
        rightPos.y = player_y;

        Vector3 rightForward = new Vector3(-1, 0, -1); //기존 TranslateBattingView의 시선 방향

        if (!leftBox)
        {
            position = rightPos;
            forward = rightForward;
            return;
        }

        //반사 축: 홈플레이트 → 2루 (구장의 좌우 대칭선)
        Vector3 home = bases[3].position;
        Vector3 axis = bases[1].position - home;
        axis.y = 0f;
        axis.Normalize();

        //점 반사: home 기준 상대좌표 d를 축에 반사 (r = 2(d·â)â - d). 높이는 player_y 유지.
        Vector3 d = rightPos - home;
        d.y = 0f;
        Vector3 mirrored = 2f * Vector3.Dot(d, axis) * axis - d;
        position = home + mirrored;
        position.y = player_y;

        //시선도 같은 축에 반사 (방향 벡터라 평행이동 없음)
        forward = 2f * Vector3.Dot(rightForward, axis) * axis - rightForward;
    }
}
