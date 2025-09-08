using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Catcher : Baseman
{
    [SerializeField] private Pitcher pitcher;
    
    [Header("Listening to Event")]
    [SerializeField] private VoidEventSO strikeEvent;
    public override void SetMyBall(Baseball myBall)
    {
        base.SetMyBall(myBall);
        
        StartCoroutine(WaitThrowToPitcher());
    }
    
    IEnumerator WaitThrowToPitcher()
    {
        LookAtPlayer(pitcher.transform.position);
        strikeEvent.RaiseEvent();
        
        yield return new WaitForSeconds(4.0f);
        
        //ball to pitcher
        ThrowBall(pitcher.transform.position);
    }
    
    
}
