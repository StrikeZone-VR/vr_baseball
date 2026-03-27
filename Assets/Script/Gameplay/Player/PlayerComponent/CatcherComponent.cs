using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CatcherComponent : BasemanComponent
{
    [SerializeField] private PitcherComponent pitcherComponent;
    [SerializeField] private Transform[] defenderTransforms; //2

    [SerializeField] private int defendIndex = 0;
    
    [Header("Listening to Event")]
    [SerializeField] private VoidEventSO backToPitcherEvent;
    
    public override void SetMyBall(Baseball myBall)
    {
        base.SetMyBall(myBall);
        
        //인플레이가 아닐때 받고 싶은데
        //StartCoroutine(WaitThrowToPitcher());
    }
    
    IEnumerator WaitThrowToPitcher()
    {
        LookAtPlayer(pitcherComponent.transform.position);
        
        yield return new WaitForSeconds(4.0f);
        
        //batterMode
        if (pitcherComponent.gameObject.activeSelf)
            ThrowBall(pitcherComponent.transform.position);
    }

    
    //IsBatTouch 기준
    private void SwitchingMove(int defendIndex)
    {
        //0이면 
        if (defendIndex == 0)
        {
            IsInPosition = false;
        }
        defenderTransform = defenderTransforms[defendIndex];
    }

    public int DefendIndex
    {
        get => defendIndex;
        set
        {
            defendIndex = value;
            SwitchingMove(defendIndex);
        }
    }
}
