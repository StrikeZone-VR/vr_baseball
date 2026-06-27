using System.Text;
using UnityEngine;

public partial class GamePlayManager
{

#if UNITY_EDITOR
    void DebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("1루 던지기");
            myBody.GetMyPitcherComponent().ThrowBall(
                bases[0].position + new Vector3(0, 0.5f, 0)
            );
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("2루 던지기");
            myBody.GetMyPitcherComponent().ThrowBall(
                bases[1].position + new Vector3(0, 0.5f, 0)
            );
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("3루 던지기");
            myBody.GetMyPitcherComponent().ThrowBall(
                bases[2].position + new Vector3(0, 0.5f, 0)
            );
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (gamePlayModel.PlayerIsBatterMode())
                DebugHitting();
            // else
            //     DebugThrowBall();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            Inning++;
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            //DebugRunners();

            BallCount++;
            //OutCount--;
            //AddOut();

            //DebugSwing();


            //스윙해라
            //currentBatter.Swing();
            //MoveOneBase();
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (gamePlayModel.PlayerIsBatterMode())
            {
                Debug.Log("투수 스토프");
                _aiPitcherComponent.IsThrowBallStop = !_aiPitcherComponent.IsThrowBallStop;
            }
            else
                DebugHitting();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (!gamePlayModel.PlayerIsBatterMode())
            {
                //Player 투수 공 받기
                myBody.GetMyPitcherComponent().ForceGrab();
            }
        }

    }
#endif

    void DebugBaseStatus()
    {
        gamePlayModel.DebugBaseStatus(isFlyingOut);

    }

    //gamePlayModel에서 현재 주자들의 정보(베이스 인덱스, 이름, 이동 여부)를 가져와 로그로 출력하는 디버깅 함수
    void DebugRunners()
    {
        var runners = gamePlayModel.GetRunners();
        Debug.Log($"[Runner] 현재 주자 수 : {runners.Count}, RunningIndex : {gamePlayModel.RunningIndex()}");
        for (int i = 0; i < runners.Count; i++)
        {
            Debug.Log($"[Runner] {i} : base={runners[i].BaseIndex}, name={runners[i].name}, isMove={runners[i].IsMove}");
        }
    }

#if UNITY_EDITOR
    void DebugHitting()
    {
        Debug.Log("디버깅용 타자 안타 함수 - 강제 타격 실행!");

        // 1. 랜덤 속력 계산
        float x = Random.Range(-1.0f, 0f);
        float y = 0.5f;
        float z = Random.Range(-1.0f, 0f);
        float power = Random.Range(25f, 25f);  //50이 홈런

        // 2. 기존 매니저의 투수 및 코루틴 제어 (이건 매니저의 일이 맞음!)
        _aiPitcherComponent.StopPitching();
        // _ball.RemoveDefender(); => DebugHit

        if(waitPitcherCoroutine != null)
            StopCoroutine(waitPitcherCoroutine);

        Vector3 targetSpawnPos = batterPosition.position + new Vector3(0, 2.0f, 0);
        Vector3 targetVelocity = new Vector3(x, y, z) * power;

        _ball.DebugHit(targetSpawnPos, targetVelocity);


        //physics time 가져와서 signal 보내기
    }

    private void DebugThrowBall()
    {
        _ball.CurrentState = BallState.Dead; //PitcherGetBall(); //공을 가져옴 근데 가져오는 시간이 꽤 될텐데
        _ball.DebugPitching();
    }

    //베이스 이동 디버그
    void DebugMoveBase(int index)
    {
        RunRunner();
        MovePlayer(bases[index].position + new Vector3(0, player_y, 0));
    }
#endif

    private void DebugSwing()
    {
        currentBatterComponent.Swing();
    }

    //리플레이/디버그 패널 공용으로 쓰는 게임 상태 스냅샷.
    //transform과 달리 거의 안 바뀌므로 레코더 쪽에서 "변할 때만" 기록한다.
    [System.Serializable]
    public struct StatusSnapshot
    {
        // ===== Ball =====
        public bool      hasBall;
        public BallState ballState;
        public bool      isInGamePlay, isGroundBall, isZone, isStrike, useGravity;
        public float     defenderDis;
        public string    defenderName;     // MyDefenderComponent
        public Vector3   ballPos, ballVel;
        // ===== Game =====
        public bool      playerIsHome;
        public int       beforeScore;      //파울/플라잉아웃 롤백 디버그용
        // ===== Flags =====
        public bool      isFlyingOut;
        public string    throwBallStop;    // "true"/"false"/"null"
        // ===== Runners =====
        public int       runningIndex;
        public RunnerSnapshot[] runners;
        public string    batterName;
    }

    [System.Serializable]
    public struct RunnerSnapshot
    {
        public int    baseIndex;
        public bool   isMove;
        public string name;
    }

    //private 필드들(_ball, gamePlayModel, isFlyingOut, _aiPitcherComponent)에 접근해야 해서
    //매니저 안에서 현재 상태를 스냅샷 구조체로 채운다. (라이브 패널/리플레이 레코더 공용)
    public StatusSnapshot CaptureStatus()
    {
        StatusSnapshot s = new StatusSnapshot();

        // ===== Ball =====
        s.hasBall = _ball != null;
        if (s.hasBall)
        {
            s.ballState    = _ball.CurrentState;
            s.isInGamePlay = _ball.IsInGamePlay;
            s.isGroundBall = _ball.IsGroundBall;
            s.isZone       = _ball.IsZone;
            s.isStrike     = _ball.IsStrike;
            s.defenderDis  = _ball.DefenderDis;
            s.defenderName = $"{_ball.MyDefenderComponent}";
            s.ballPos      = _ball.PhysicsPosition;
            s.ballVel      = _ball.Velocity;
            s.useGravity   = _ball.UseGravity;
        }

        // ===== Game =====
        s.playerIsHome = gamePlayModel.PlayerIsHome;
        s.beforeScore  = gamePlayModel.BeforeScore; //파울/플라잉아웃 롤백 디버그용

        // ===== Flags =====
        s.isFlyingOut   = isFlyingOut;
        s.throwBallStop = _aiPitcherComponent != null ? _aiPitcherComponent.IsThrowBallStop.ToString() : "null";

        // ===== Runners =====
        var runners = gamePlayModel.GetRunners();
        s.runningIndex = gamePlayModel.RunningIndex();
        s.runners = new RunnerSnapshot[runners.Count];
        for (int i = 0; i < runners.Count; i++)
        {
            BatterComponent r = runners[i];
            s.runners[i] = new RunnerSnapshot { baseIndex = r.BaseIndex, isMove = r.IsMove, name = r.name };
        }

        s.batterName = $"{currentBatterComponent}";

        return s;
    }

    //상태 스냅샷을 디버그 패널 텍스트로 변환. richText(<b>) 사용. 라이브/리플레이 공용이라 static.
    public static void FormatStatus(StringBuilder sb, StatusSnapshot s)
    {
        // ===== Ball =====
        sb.AppendLine("<b>[ BALL ]</b>");
        if (s.hasBall)
        {
            sb.AppendLine($"State : {s.ballState}");
            sb.AppendLine($"IsInGamePlay : {s.isInGamePlay}");
            sb.AppendLine($"IsGroundBall : {s.isGroundBall}");
            sb.AppendLine($"IsZone : {s.isZone}");
            sb.AppendLine($"IsStrike : {s.isStrike}");
            sb.AppendLine($"DefenderDis : {s.defenderDis}");
            sb.AppendLine($"MyDefenderComponent : {s.defenderName}");

            Vector3 pos = s.ballPos;
            Vector3 vel = s.ballVel;
            sb.AppendLine($"Pos : ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
            sb.AppendLine($"Vel : ({vel.x:F2}, {vel.y:F2}, {vel.z:F2})");


            sb.AppendLine($"Speed : {vel.magnitude * 3.6f:F1} km/h"); //m/s → km/h
            sb.AppendLine($"UseGravity : {s.useGravity}");
        }
        else
        {
            sb.AppendLine("(_ball null)");
        }

        // ===== Game =====
        sb.AppendLine();
        sb.AppendLine("<b>[ GAME ]</b>");
        sb.AppendLine($" (PlayerIsHome={s.playerIsHome})");
        sb.AppendLine($"Before : score={s.beforeScore}"); //파울/플라잉아웃 롤백 디버그용

        // ===== Flags =====
        sb.AppendLine();
        sb.AppendLine("<b>[ FLAGS ]</b>");
        sb.AppendLine($"isFlyingOut     : {s.isFlyingOut}");
        sb.AppendLine($"IsThrowBallStop : {s.throwBallStop}");

        // ===== Runners =====
        sb.AppendLine();
        int runnerCount = s.runners != null ? s.runners.Length : 0;
        sb.AppendLine($"<b>[ RUNNERS ]</b>  count={runnerCount}, running={s.runningIndex}");
        if (runnerCount == 0)
        {
            sb.AppendLine("(없음)");
        }
        else
        {
            for (int i = 0; i < runnerCount; i++)
            {
                RunnerSnapshot r = s.runners[i];
                string moving = r.isMove ? "→달림" : "정지 ";
                sb.AppendLine($"  base {r.baseIndex} | {moving} | {r.name}");
            }
        }

        sb.AppendLine($"타석 : {s.batterName}");
    }

#if UNITY_EDITOR
    //오른쪽 디버그 오버레이(DebugStatusPanel)가 매 프레임 읽어가는 상태 스냅샷.
    //private 필드들(_ball, baseballModel, gamePlayModel, isFlyingOut, _aiPitcherComponent)에
    //접근해야 해서 매니저 안에서 문자열을 만들어 넘긴다. richText(<b>) 사용.
    public void BuildDebugStatus(StringBuilder sb) => FormatStatus(sb, CaptureStatus());
#endif
}
