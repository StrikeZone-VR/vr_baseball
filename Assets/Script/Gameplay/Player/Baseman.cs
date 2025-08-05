using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baseman : Defender
{
    [SerializeField] private Transform myBase;
    [SerializeField] private int index; //0은 1루, 1은 2루, 2는 3루, 3은 홈

    protected override void Update()
    {
        base.Update();

        if (!IsTracking)
        {
            //long base dis => go to base
            if (Vector3.Distance(myBase.position, transform.position) >= 0.2f)
            {
                nav.SetDestination(myBase.position);
                LookAtPlayer(myBase.position);
            }
        }
    }


    public void IsOut(Batter runner)
    {
        if (!_ball.IsBatTouch)
        {
            return;
        }

        //던진 공이 베이스에 있고
        
        //runners[index].BaseIndex = 0;

        //주자가 달리는 중이라면 => 이게 문제
        //runners.IsMove && runners.base_index



    }
}