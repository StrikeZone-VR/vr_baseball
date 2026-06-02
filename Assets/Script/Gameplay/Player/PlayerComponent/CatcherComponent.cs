using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CatcherComponent : BasemanComponent
{
    [SerializeField] private PitcherComponent pitcherComponent;
    [SerializeField] private Transform[] defenderTransforms; //2

    //0 => 공받기. 1 => 수비
    [SerializeField] private int defendIndex = 0;
    
    [Header("Listening to Event")]
    [SerializeField] private VoidEventSO backToPitcherEvent;
    
    protected override void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base") && defendIndex == 1)
        {
            Debug.LogWarning("[Catcher] : 이거 포수가 수비도중에 중간에 멈추면 이거를 봐라");
            IsInPosition = true;
        }
    }

    protected override void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base") && defendIndex == 1)
        {
            Debug.LogWarning("[Catcher] : 이거 포수가 수비도중에 중간에 멈추면 이거를 봐라");
            IsInPosition = false;
        }
    }
    
    public override void SetMyBall(Baseball myBall)
    {
        base.SetMyBall(myBall);
        
        //포수가 배트에 안 건드린 공을 만진 경우 
        if (!_myBall.IsInGamePlay)
        {
            myBall.PitchResult();            
        }
        
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
        //0이면 뒤로 가라 1이면 앞으로
        defenderTransform = defenderTransforms[defendIndex];
        
        IsInPosition = false;
        IsTracking = false; //공을 쫒아가지 마라 => 알아서 움직여야함
    }

    public int DefendIndex
    {
        get => defendIndex;
        set
        {
            defendIndex = value;
            GameLog.Defend("[Catcher] : "  + defendIndex);
            SwitchingMove(defendIndex);
        }
    }
}
