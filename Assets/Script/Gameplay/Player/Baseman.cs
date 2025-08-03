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
            nav.SetDestination(myBase.position);
            LookAtPlayer(myBase.position);
        }
    }
}