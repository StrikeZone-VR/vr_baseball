using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baseman : Defender
{
    [SerializeField] private int base_index; //0 : 1baseman, 3 : catcher
    

    // protected override void Update()
    // {
    //     base.Update();
    // }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            isInPosition = true;
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            isInPosition = false;
        }
    }

    protected override void OutRunner()
    {
        base.OutRunner(); //isFlying out
        
        if (!_ball.IsBatTouch && !isInPosition)
        {
            return;
        }
        outBatterEventSO.RaiseEvent(base_index);
    }
}