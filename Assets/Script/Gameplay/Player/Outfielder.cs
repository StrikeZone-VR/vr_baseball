using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Outfielder : Defender
{
    protected override void Update()
    {
        float dis = Vector3.Distance(defenderTransform.position, transform.position);
            
        if (dis <= 1.0f)
        {
            isInPosition = true;
        }
        else
        {
            isInPosition = false;
        }
        base.Update();
    }
}
