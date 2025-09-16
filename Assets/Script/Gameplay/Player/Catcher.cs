using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Catcher : Baseman
{
    [SerializeField] private Pitcher pitcher;
    [SerializeField] private PitchingManager debug_pm;

    
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
        
        //batterMode
        if (pitcher.gameObject.activeSelf)
            ThrowBall(pitcher.transform.position);
        else
        {
            debug_pm.ResetBall();
            //_ball.GetComponent<PitchingBallController>().ResetBall();
        }
    }
    
    
}
