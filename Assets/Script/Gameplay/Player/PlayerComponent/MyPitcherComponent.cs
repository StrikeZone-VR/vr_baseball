using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MyPitcherComponent : PitcherComponent
{
    [SerializeField] private XRBaseInteractor handInteractor; // 직접 잡는 손 (XR Direct Interactor right)

    protected override void Update()
    {
        //일부로 이렇게 함
    }
    
    void OnCollisionEnter(Collision collision)
    {
        //일부로 이렇게 함
    }
    public void ForceGrab()
    {
        //_ball
        // 1. 만약 손에 이미 다른 걸 들고 있다면? -> 먼저 강제로 놓게 만듭니다.
        if (handInteractor.hasSelection)
        {
            // 구버전/신버전 호환성을 위해 인터랙터가 잡고 있는 첫 번째 물건을 놓게 함
            List<IXRSelectInteractable> currentItems = handInteractor.interactablesSelected;

            foreach (var item in currentItems)
            {
                handInteractor.interactionManager.SelectExit(handInteractor, item);
            }
        }

        // 2. XR Interaction Manager를 통해 손과 공을 강제로 연결(SelectEnter) 시킵니다!
        handInteractor.interactionManager.SelectEnter(handInteractor, player.GetBaseBall().GrabInteractable);

        //Debug.Log("⚾ B버튼 클릭: 야구공을 강제로 잡았습니다!");
        SetMyBall(player.GetBaseBall());
    }

    // 1루 2루 3루 홈
    public void ThrowBall(Vector3 position)
    {
        if (!player.GetBaseBall().MyDefenderComponent)
        {
            return;
        }
        
        if (handInteractor.hasSelection)
        {
            // 구버전/신버전 호환성을 위해 인터랙터가 잡고 있는 첫 번째 물건을 놓게 함
            List<IXRSelectInteractable> currentItems = handInteractor.interactablesSelected;
            
            for (int i = currentItems.Count - 1; i >= 0; i--)
            {
                handInteractor.interactionManager.SelectExit(handInteractor, currentItems[i]);
            }
        }

        _myBall.CurrentState = BallState.Thrown;
        
        // 🔥 4. 변경된 부분: 던지는 함수를 바로 부르지 않고, 코루틴에게 토스합니다!
        StartCoroutine(DelayedThrowRoutine(position, 45f));
    }
    
    /// <summary>
    ///물리 엔진 1틱(0.02초)을 기다려주는 함수 
    /// </summary>
    /// <param name="velocity"></param>
    /// <returns></returns>
    private IEnumerator DelayedThrowRoutine(Vector3 targetPosition, float velocity)
    {
        yield return new WaitForFixedUpdate(); // 물리 기준 1 프레임
    
        //todo 근데 왜 transform.postion이지? myball로 해야하는 거 아닌가
        _myBall.ThrowBall(transform.position, targetPosition, velocity, false);
    }
}
