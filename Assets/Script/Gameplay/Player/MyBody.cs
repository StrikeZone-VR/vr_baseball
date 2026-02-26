using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//body
public class MyBody : Batter
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Batter prefabBatter;
    [SerializeField] private Transform _parent;

    //GamePlayManager's onCanBackBatterEvent
    [SerializeField] private VoidEventSO moveBatterEvent; 
    
    void Update()
    {
        transform.position = _camera.transform.position + new Vector3(0, -1.23f, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Base"))
        {
            if(IsIntoBase(other))
            {
                IsMove = false;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("Base"))
        {
            IsMove = true;
        }
    }
    
    public override void OutPlayer(bool isMove = true)
    {
        Debug.Log("[Batter] : My body out : " + isMove);
        
        if (isMove)
            moveBatterEvent.RaiseEvent(); 
    }

    

    
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
            //     if(base_index != 0) //너무 많이 출력된다
            //         Debug.Log("stop : " + (base_index));
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
}
