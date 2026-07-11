using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyBatterComponent : BatterComponent
{
    //GamePlayManager's onCanBackBatterEvent
    [SerializeField] private VoidEventSO moveBatterEvent;
    
    private bool isOut = false;

    
    public override bool IsMove
    {
        get => isMove;
        set
        {
            isMove = value;
            // if (isMove)
            // {
            //     Debug.Log("run : " + base_index);
            // }
            // else //어 근데 아마 전 값 때문에 +1을 해야할지도?
            // {
            //     Debug.Log("stop : " + (base_index));
            // }
            changedBaseStatus.RaiseEvent();
        }
    }

    public override int BaseIndex
    {
        get => base_index;
        set
        {
            if (value < 0)
            {
                return;
            }

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
                //Debug.Log("성공 : " + base_index);
                //다시 타석으로 => 페이드아웃 + moveEvent.
                //addIsBaseStatus.RaiseEvent(value - 1);
                moveBatterEvent.RaiseEvent();
                //changedBaseStatus.RaiseEvent();
                
                //ㄴ GamePlayManager에 있는 ThrowBallAlgorithm가 -1이어야지 출력하는게 나은듯

                //todo : 홈런인 경우 어떡하지
                return;
            }
            //base_index = 0;
        }
    }

    public override void OutPlayer(bool isMove = true)
    {
        Debug.Log("[Batter] : My body out : " + isMove);
        IsOut = true;
        BaseIndex =0;
        if (isMove)
            moveBatterEvent.RaiseEvent(); 
    }
    //플레이어 본인은 자동 진루하지 않는다 — XR로 직접 뛰는 것이 곧 진루 판단.
    //ㄴ base가 true를 리턴하면 베이스 위에서 '달리는 중'으로 마킹돼 포스아웃 대상이 되므로 반드시 차단.
    protected override bool TryExtraBase() => false;

    public bool IsOut
    {
        get => isOut;
        set => isOut = value;
    }
}
