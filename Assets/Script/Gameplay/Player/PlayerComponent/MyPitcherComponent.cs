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
        handInteractor.interactionManager.SelectEnter(handInteractor, player.GetBall().GrabInteractable);

        SetMyBall(player.GetBall());
        Debug.Log("⚾ B버튼 클릭: 야구공을 강제로 잡았습니다!");
    }

    // 1루 2루 3루 홈
    public void ThrowBase(Vector3 throwTarget)
    {
        if (!player.GetBall().MyDefenderComponent)
        {
            return;
        }

        //던질때 제거
        ThrowBall(throwTarget);
    }
}
