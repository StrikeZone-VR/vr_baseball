using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class OutfielderComponent : DefenderComponent
{
    protected override void Update()
    {
        float dis = Vector3.Distance(defenderTransform.position, transform.position);
            
        if (dis <= 1.0f)
        {
            IsInPosition = true;
        }
        else
        {
            IsInPosition = false;
        }
        base.Update();
    }
}
