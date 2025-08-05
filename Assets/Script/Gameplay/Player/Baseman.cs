using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baseman : Defender
{
    [SerializeField] private Transform myBase;
    [SerializeField] private int index; //0�� 1��, 1�� 2��, 2�� 3��, 3�� Ȩ

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

        //���� ���� ���̽��� �ְ�
        
        //runners[index].BaseIndex = 0;

        //���ڰ� �޸��� ���̶�� => �̰� ����
        //runners.IsMove && runners.base_index



    }
}