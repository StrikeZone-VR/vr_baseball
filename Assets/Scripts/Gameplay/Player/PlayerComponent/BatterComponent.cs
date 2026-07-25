using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class BatterComponent : PlayerComponent
{
    //[SerializeField] private Baseball _ball;
    [SerializeField] protected int base_index = 0; // 1 2 3 => 0은 1루 가기 전 상태
   
    [SerializeField] protected Transform[] bases;
    [SerializeField] protected Transform init_base;
    [SerializeField] private Bat bat;

    [Header("Listening to Events")]
    [SerializeField] protected VoidEventSO startPitchEvent; //From GameManager
    [SerializeField] protected VoidEventSO addScore; //From GameManager
    [SerializeField] protected VoidEventSO strikeEvent; //From GameManager
    [SerializeField] protected IntEventSO addIsBaseStatus; //From GameManager
    [SerializeField] protected VoidEventSO changedBaseStatus;

    [SerializeField] protected bool isMove = false;

    [Tooltip("추가 진루 판단 거리(m). 베이스 도착 시 '다음 베이스~공 현재 위치'가 이보다 멀면 계속 진루. AI 전용(플레이어는 항상 안착).")]
    [SerializeField] private float extraBaseDistance = 25f;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            FieldBase fieldBase = collision.GetComponent<FieldBase>();
            if (!fieldBase)
            {
                Debug.LogWarning("fieldBase가 없음");
                return;
            }
            
            if(IsIntoBase(fieldBase))
            {
                //홈 도착이면 BaseIndex setter가 이미 득점 처리(addScore → IntoHome → 제거)함. 이동 상태는 안 건드림
                if (fieldBase.BaseIndex == 0)
                {
                    return;
                }

                //수비수와 공 거리가 10f 이하면 가지마라
                //ㄴ 이 조건만으로 정지를 걸던 게 '베이스 위 + 달리는 중' 림보(뜬금 포스아웃)의 원인.
                //   도착하면 반드시 진루(true) 또는 안착(false) 중 하나를 대입한다 — 아무것도 안 하는 경로 금지.
                if (TryExtraBase())
                {
                    IsMove = true; //IsIntoBase가 BaseIndex를 이미 +1 → setter의 MoveBase가 다음 베이스로 재출발
                }
                else
                {
                    IsMove = false; //일단 움직임을 멈춰야 debug에 찍힘
                }
            }
        }

        if (collision.gameObject.CompareTag("BatterPos"))
        {

            if (base_index == 0)
            {
                ReadyBatting();
            }
        }
    }


    public void SetBat(Bat bat)
    {
        this.bat = bat;
    }

    public void Swing()
    {
        bat.StartSwing();
    }

    //호출하고 싶다면 ismove에서
    private void MoveBase()
    {
        player.MovePlayer(bases[base_index].position);
    }

    
    public bool IsIntoBase(FieldBase fieldBase)
    {
        //GameLog.RunnerLog("IsIntoBase : " + base_index);
        //is same going to the next base index
        //1 => 0 + 1, baseindex가 3인데 베이스가 0으로 간 경우
        if (fieldBase.BaseIndex == base_index + 1 || (fieldBase.BaseIndex == 0 && base_index == 3))
        {
            //혻시 BaseIndex를 IsMove 아래로 둔 이유가 있을까? 
            BaseIndex++;
            return true;
        }
        //fieldBase.BaseIndex가 1 2 3만
        if (fieldBase.BaseIndex == base_index && base_index > 0)
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 베이스 도착 시 추가 진루 판단. true = 다음 베이스로 재출발, false = 안착.
    /// 플레이어(MyBatterComponent)는 본인이 직접 뛰므로 항상 false로 오버라이드.
    /// </summary>
    protected virtual bool TryExtraBase()
    {
        Baseball ball = player.GetBaseBall();
        if (ball == null || !ball.IsInGamePlay) return false; //플레이가 끝난 공이면 정지
        if (ball.MyDefenderComponent != null) return false;   //수비수가 이미 공을 잡았으면 정지
        if (base_index >= bases.Length) return false;         //홈 득점 처리 후

        //'다음 목표 베이스'와 공의 실시간 거리로 판단.
        //DefenderDis(공 낙하지점~최근접 수비수)는 트래킹 시점 스냅샷이라 stale할 수 있어 쓰지 않는다.
        float dis = Vector3.Distance(ball.transform.position, bases[base_index].position);
        bool go = dis > extraBaseDistance && ball.CurrentState != BallState.Thrown; //패스중이라면 가지마라
        if (go)
        {
            GameLog.RunnerLog($"[진루] {name} → base{base_index} 재출발 (공~베이스 {dis:F1}m)");
        }
        return go;
    }

    //타석 대기
    protected virtual void ReadyBatting()
    {
        player.StopMove();
        startPitchEvent.RaiseEvent();
        player.LookAtPlayer(new Vector3(-1, 0, -1));
    }

    public virtual void OutPlayer(bool isMove = true)
    {
        player.StopMove();
        Destroy(this.gameObject);
    }

    // public override void MovePlayer(Vector3 pos)
    // {
    //     player.MovePlayer(pos);
    // }

    #region PROPERTYS
    public virtual bool IsMove
    {
        get => isMove;
        set
        {
            isMove = value;
            if (isMove)
            {
                MoveBase();
            }
            else
            {
                player.StopMove();
            }
            changedBaseStatus.RaiseEvent(); 
        }
    }

    

    // want to go base index => baseindex를 먼저 설정하고 IsMove를 설정하자
    public virtual int BaseIndex
    {
        get => base_index;
        set
        {
            if (value < 0)
            {
                return;
            }

            //Debug.Log("[Batter] base_index "+ name + ": " + value);
            //arrive home
            if (value >= bases.Length)
            {
                addScore.RaiseEvent(); 
                //IsMove = false; => this will be null
                
                return;
            }
            base_index = value;
            //change base status => else, goto 1base 
            if (0 < value && value < bases.Length)
            {
                //addIsBaseStatus.RaiseEvent(value - 1);
                
                //일단 IsMove로 해서 오류만 생긴다
                //changedBaseStatus.RaiseEvent(); 
            }
        }
    }

    public void SetBases(Transform[] bases,Transform init_base)
    {
        this.init_base = init_base;
        this.bases = bases;
    }

    public void SetBaseIndexPosition(int baseIndex)
    {
        if (baseIndex == 0)
        {
            //이거 안하면 괜히 이상한 스트라이크존에 AI가 서있는다.
            transform.position = init_base.position;
            BaseIndex = baseIndex;
            IsMove = false;

            return;
        }
        
        BaseIndex = baseIndex;
        IsMove = false;

        transform.position = bases[baseIndex - 1].position;
    }

    #endregion
}
