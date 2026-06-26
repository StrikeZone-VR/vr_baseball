using System.Text;
using UnityEngine;

public partial class GamePlayManager
{
    #region DEBUG

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
            DebugRunners();

            //BallCount++;
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
        float power = Random.Range(15f, 15f);  //50이 홈런

        x *= -1;
        z *= -1;
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

#if UNITY_EDITOR
    //오른쪽 디버그 오버레이(DebugStatusPanel)가 매 프레임 읽어가는 상태 스냅샷.
    //private 필드들(_ball, baseballModel, gamePlayModel, isFlyingOut, _aiPitcherComponent)에
    //접근해야 해서 매니저 안에서 문자열을 만들어 넘긴다. richText(<b>) 사용.
    public void BuildDebugStatus(StringBuilder sb)
    {
        // ===== Ball =====
        sb.AppendLine("<b>[ BALL ]</b>");
        if (_ball != null)
        {
            sb.AppendLine($"State : {_ball.CurrentState}");
            sb.AppendLine($"IsInGamePlay : {_ball.IsInGamePlay}");
            sb.AppendLine($"IsGroundBall : {_ball.IsGroundBall}");
            sb.AppendLine($"IsZone : {_ball.IsZone}");
            sb.AppendLine($"IsStrike : {_ball.IsStrike}");
            sb.AppendLine($"DefenderDis : {_ball.DefenderDis}");
            sb.AppendLine($"MyDefenderComponent : {_ball.MyDefenderComponent}");

            Vector3 pos = _ball.PhysicsPosition;
            Vector3 vel = _ball.Velocity;
            sb.AppendLine($"Pos : ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
            sb.AppendLine($"Vel : ({vel.x:F2}, {vel.y:F2}, {vel.z:F2})");
            sb.AppendLine($"Speed : {vel.magnitude * 3.6f:F1} km/h"); //m/s → km/h
        }
        else
        {
            sb.AppendLine("(_ball null)");
        }

        // ===== Game =====
        sb.AppendLine();
        sb.AppendLine("<b>[ GAME ]</b>");
        sb.AppendLine($" (PlayerIsHome={gamePlayModel.PlayerIsHome})");
        sb.AppendLine($"Before : score={gamePlayModel.BeforeScore}"); //파울/플라잉아웃 롤백 디버그용

        // ===== Flags =====
        sb.AppendLine();
        sb.AppendLine("<b>[ FLAGS ]</b>");
        sb.AppendLine($"isFlyingOut     : {isFlyingOut}");
        sb.AppendLine($"IsThrowBallStop : {(_aiPitcherComponent != null ? _aiPitcherComponent.IsThrowBallStop.ToString() : "null")}");

        // ===== Runners =====
        sb.AppendLine();
        var runners = gamePlayModel.GetRunners();
        sb.AppendLine($"<b>[ RUNNERS ]</b>  count={runners.Count}, running={gamePlayModel.RunningIndex()}");
        if (runners.Count == 0)
        {
            sb.AppendLine("(없음)");
        }
        else
        {
            for (int i = 0; i < runners.Count; i++)
            {
                BatterComponent r = runners[i];
                string moving = r.IsMove ? "→달림" : "정지 ";
                sb.AppendLine($"  base {r.BaseIndex} | {moving} | {r.name}");
            }
        }

        sb.AppendLine($"타석 : {currentBatterComponent}");
    }
#endif

    #endregion
}
