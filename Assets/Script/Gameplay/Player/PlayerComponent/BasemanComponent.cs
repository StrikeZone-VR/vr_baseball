using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasemanComponent : DefenderComponent
{
    [SerializeField] private int base_index; //1 2 3 4
    [SerializeField] private IntEventSO outRunnerEvent;
    
    
    private GamePlayModel _gamePlayModel;

    // protected override void Update()
    // {
    //     base.Update();
    // }

    protected virtual void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            IsInPosition = true;
        }
    }

    protected virtual void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            IsInPosition = false;
        }
    }

    //공을 잡은 케이스에만 생김, 만약 공 잡고 베이스 가면 아웃이 안됨
    protected override void OutRunner()
    {
        base.OutRunner(); //isFlying out
        
        if (!_myBall)
        {
            return;
        }
        //베이스 밟은 경우 => 판단은 저쪽에서
        outRunnerEvent.RaiseEvent(base_index - 1); //베이스 이전 값 아웃
    }
    
    
    //디버깅용 함수
    public override bool IsInPosition
    {
        get => isInPosition;
        set
        {
            if (base_index == 4)
            {
                Debug.Log("[catchman] isInposition : " + value);
            }
            isInPosition = value;

            //어차피 플라잉 아웃은 그 전에 된 게 아닐까
            OutRunner();
        }
    }
}