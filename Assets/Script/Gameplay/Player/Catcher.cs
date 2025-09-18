using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Catcher : Baseman
{
    [SerializeField] private Pitcher pitcher;
    
    [Header("Listening to Event")]
    [SerializeField] private VoidEventSO strikeEvent;
    [SerializeField] private VoidEventSO backToPitcherEvent;
    
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
            backToPitcherEvent.RaiseEvent();
            //_ball.GetComponent<PitchingBallController>().ResetBall();
        }
    }
    
    
}
