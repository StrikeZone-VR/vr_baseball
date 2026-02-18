using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baseman : Defender
{
    [SerializeField] private int base_index; //1 2 3 4
    [SerializeField] private IntEventSO outRunnerEvent;

    // protected override void Update()
    // {
    //     base.Update();
    // }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            IsInPosition = true;
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            IsInPosition = false;
        }
    }

    protected override void OutRunner()
    {
        base.OutRunner(); //isFlying out
        
        if (!_ball.IsBatTouch || !IsInPosition)
        {
            return;
        }

        //베이스 밟은 경우
        outRunnerEvent.RaiseEvent(base_index - 1); //베이스 이전 값 아웃
    }
    
    
    //디버깅용 함수
    // protected override bool IsInPosition
    // {
    //     get => isInPosition;
    //     set
    //     {
    //         isInPosition = value;
    //     }
    // }
}