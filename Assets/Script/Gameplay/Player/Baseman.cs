using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baseman : Defender
{
    [SerializeField] private Transform myBase;

    protected override void Update()
    {
        base.Update();

        if (!IsTracking)
        {
            //dis >= 0.1f => go to base
            if (Vector3.Distance(myBase.position, transform.position) >= 0.2f)
            {
                nav.SetDestination(myBase.position);
                LookAtPlayer(myBase.position);
            }
        }
    }
}