using UnityEngine;

public partial class GamePlayManager
{
    /// <summary>
    /// 트래킹 알고리즘
    /// </summary>
    private void TrackingBall()
    {
        if (_ball.MyDefenderComponent)
        {
            return;
        }
        //tracking => 혹시 포수가 못 잡을 수 있으니 isBatTouch는 넣지말자
        //if (!_ball.IsPassing && _ball.IsGroundBall && !_ball.IsThrown)

        //인 게임 플레이
        if(_ball.IsInGamePlay)
        {
            //떨어지는 공 위치중에서 가장 가까운 수비수
            int index = FindClosestDefenderIndex();
            OnlyOneTrackingOn(index);

            //Debug.Log("엄준식2 + " + GetDefenderComponent(index).name);

            //closestDefender set tracking
            if (index == -1)
            {
                return;
            }

            //Debug.Log("트래킹 잠시 무효화");
            GetDefenderComponent(index).IsTracking = true;
        }
    }


    private void BeforePitcherGetBall()
    {
        //수비수에게 공 던지는 함수 => 복귀 알고리즘
        // thinking => 여기서 IsZone을 뺐다.
        if (_ball.MyDefenderComponent && _ball.IsInGamePlay)
        {
            bool canThrow = ThrowBallAlgorithm();

            //던질 곳 없으면 다시 투수 복귀
            if (!canThrow)
            {
                //어차피 근데 타자모드일때만 이라고 해도 canBackRunner자체가 여기에서만 나올듯
                //위치를 여기다 둔 이유. 피쳐가 수비하면 타자 복귀가 안됨
                //안타나 플라잉아웃이면 나중에 복귀
                if (canBackRunner)
                {
                    //일단 여기라인이 떠야함
                    canBackRunner = false;

                    //안타
                    if (!isFlyingOut && !myBody.GetMyBatterComponent().IsOut)
                    {
                        //돌아오시고
                        TransformMyBodyToBatter();
                    }

                    StartCoroutine(TranslateBattingView());
                    DebugBaseStatus();
                }

                //AI투수가 공을 가지고 있다면
                if (_aiPitcherComponent && _ball.MyDefenderComponent == _aiPitcherComponent)
                {
                    return;
                }

                //돌아오는 함수
                if (canGetBall)
                {
                    GameLog.BallLog("[Baseball] 상태 : " + _ball.CurrentState);
                    _ball.CurrentState = BallState.Dead; //죽으면 결국 다시 생기지 않나
                    //PitcherGetBall();
                }
            }
            else
            {
                GameLog.Defend("2차 허걱 => 던질 곳이 있다고?");
                DebugRunners();
            }
            //플레이어 투수가 공을 잡은 경우
        }
    }

    private void FlyingOut()
    {
        GameLog.Defend("[Batting] : 플라잉 아웃");

        //여기에 AddOut을 넣으면 이닝이 바뀌었는데 주자를 제거하고 있음
        isFlyingOut = true;
        gamePlayModel.RemoveLastRunner(); //RemoveRunner로 하면 baseindex = 1이 두명이고 앞선 주자가 아웃이 된다.

        //Destroy();
        currentBatterComponent.OutPlayer(true);
        currentBatterComponent = null;

        //되돌아가자
        ReverseMoveBase();
        AddOut();
    }


    /// <summary>
    /// 아웃하는 함수지만 베이스 아웃 판단하는 것도 넣음
    /// </summary>
    /// <param name="base_index">아웃 될 주자의 base_index임.</param>
    private void OutRunner(int base_index)
    {
        //주자의 base_index0 1 2 3
        //1루가기전 2루가기전 3루가기전 홈으로가기전
        //수비수 : 1 2 3 4

        // 기본적으로 공을 가지고있는 상태
        if (!_ball.IsInGamePlay || !GetDefenderComponent(base_index + 1).IsInPosition)
        {
            //Debug.LogError("1탄 : " + _ball.IsBatTouch + ", " + GetDefenderComponent(base_index + 1).IsInPosition);
            return;
        }
        //debug
        if (base_index > 3)
        {
            Debug.LogError("베이스 index가 넘으면 안된다 : " + base_index);
        }

        //이미 베이스 인덱스는
        if (gamePlayModel.IsEmptyRunner(base_index))
        {
            //Debug.LogError("2탄");
            return;
        }

        BatterComponent runner = gamePlayModel.GetRunner(base_index);

        //만약 1루면 1루 전 러너가 있는지 확인. 러너가 달리지 않는다면
        if (!runner.IsMove)
        {
            //Debug.LogError("3탄");
            return;
        }
        //주자 배열을 먼저 제거
        gamePlayModel.RemoveRunner(base_index);
        AddOut(); //그리고 아웃 카운트


        //Destroy();
        runner.OutPlayer();
        DebugBaseStatus();
    }

    private void AllTrackingOff()
    {
        OnlyOneTrackingOn();
    }

    private void OnlyOneTrackingOn(int ignore_index = -1)
    {
        for (int i = 0; i < defenders.Length; i++)
        {
            if (ignore_index == i)
            {
                continue;
            }
            if (defenders[i].gameObject.activeSelf)
            {
                GetDefenderComponent(i).IsTracking = false;
            }
        }
    }

    private bool ThrowToBase(int index)
    {
        //0 1 2 3
        //1루까지 2루까지 3루까지 홈까지

        if (_ball.MyDefenderComponent)
        {
            if (index < 0 || 4 <= index) //1루수 ~ 4루수가 아니라면
            {
                return false;
            }

            //내 베이스맨이 주자가 있는 상태에서 공을 가진 경우. => 트래킹
            //1 2 3 4
            if (_ball.MyDefenderComponent != defenders[index + 1].GetPlayerComponent())
            {
                _ball.MyDefenderComponent.ThrowBall(bases[index].position + new Vector3(0, 0.5f, 0));
                return true;
            }
            //근데 자기 자신에게 있으면
            //베이스 밖이면 자기 베이스로 복귀시키고 플레이 유지
            //도착하면 OnTriggerEnter → IsInPosition → OutRunner로 포스아웃 처리됨
            if (!_ball.MyDefenderComponent.IsInPosition)
            {
                _ball.MyDefenderComponent.IsTracking = false; //세터가 defenderTransform으로 MovePlayer 해줌
                return true;
            }
            //이미 베이스 위면 아웃 처리는 트리거 쪽에서 끝났으니 false로 떨어져 플레이 종료
        }

        return false;
    }


    /// <summary>
    /// 가장 가까운 수비수를 찾아라
    /// </summary>
    /// <returns></returns>
    private int FindClosestDefenderIndex()
    {
        float min = float.MaxValue;
        int index = -1;

        for (int i = 0; i < defenders.Length; i++)
        {
            //투수모드인 경우
            if (!gamePlayModel.PlayerIsBatterMode())
            {
                continue;
            }
            float dis = GetDistanceBetween(_trajectoryBaseBallData.GetLandingPoint(), defenders[i].transform.position);

            if (min > dis)
            {
                min = dis;
                index = i;
            }
        }


        _ball.DefenderDis = min;
        return index;
    }

    private bool ThrowBallAlgorithm() //SO
    {
        int index = gamePlayModel.RunningIndex();

        //던질 곳 없음
        if (index == -1)
        {
            return false;
        }

        //플레이어 투수인 경우. 어차피 자기가 던지니까
        if (_ball.MyDefenderComponent == myBody.GetMyPitcherComponent())
        {
            GameLog.Defend("3차 : 설마 이것때문이니?");
            return true;
        }

        //타자의 0 1 2 3
        //던지기 => 만약 공 mydefender와 index가 같은 경우 => 무조건 자기 베이스로 돌아가야함
        if (ThrowToBase(index))
        {
            GameLog.Defend("4차 : 그냥 자기 자신에게 패스" + index);

            return true;
        }
        return false;
    }

    private float GetDistanceBetween(Vector3 a, Vector3 b)
    {
        float result = Vector3.Distance(a, b);
        return result;
    }
}
