using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baseman : Defender
{
    [SerializeField] private Transform myBase;
    [SerializeField] private int base_index; //0 : 1baseman, 3 : catcher
    [SerializeField] private bool isInbase = false;
    protected override void Update()
    {
        base.Update();

        if (!IsTracking)
        {
            float dis = Vector3.Distance(myBase.position, transform.position);

            //long base dis => go to the base
            if (dis >= 1f)
            {
                nav.SetDestination(myBase.position);
                LookAtPlayer(myBase.position);
            }
            
            //in base
            else
            {
                bool isInBase = true;
            }
        }
    }

    protected override void OutRunner()
    {
        base.OutRunner(); //isFlying out
        
        if (!_ball.IsBatTouch && !isInbase)
        {
            return;
        }
        outBatterEventSO.RaiseEvent(base_index);
    }
}