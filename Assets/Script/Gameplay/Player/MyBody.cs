using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


//body
public class MyBody : Player
{
    [Header("연결할 오브젝트")]
    //대체로 playerComponent가 batter 
    [SerializeField] private PlayerComponent subComponent; //pitcher 
    [SerializeField] private XRBaseInteractor handInteractor; // 직접 잡는 손 (XR Direct Interactor right)

    [SerializeField] private Camera _camera;

    

    public void SetCamera(Camera camera)
    {
        _camera = camera;
    }
    
    void Update()
    {
        if(_camera)
            transform.position = _camera.transform.position + new Vector3(0, -1.23f, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Base"))
        {
            if(IsIntoBase(other))
            {
                SetIsMove(false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("Base"))
        {
            SetIsMove(true);
        }
    }
    

    //보면 BatterComponent에 IsMove하고 BaseIndex가 있을거다
    //해결법은 간단하다. => UnityAction으로 넣으면 되지 않을까?

    public void SetIsMove(bool isMove)
    {
        BatterComponent batterComponent = _playerComponent as BatterComponent;
        batterComponent.IsMove = isMove;
    }
    
    /// <summary>
    /// 이거는 베이스 인덱스만 설정하는 거다. 포지션도 바꿀거면 SetBaseIndexPosition
    /// </summary>
    /// <param name="index"></param>
    public void SetBaseIndex(int index)
    {
        BatterComponent batterComponent = _playerComponent as BatterComponent;
        batterComponent.BaseIndex = index;
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
        handInteractor.interactionManager.SelectEnter(handInteractor, ball.GrabInteractable);
        
        Debug.Log("⚾ B버튼 클릭: 야구공을 강제로 잡았습니다!");
    }

    private bool IsIntoBase(Collider other)
    {
        BatterComponent batterComponent = _playerComponent as BatterComponent;
        return batterComponent.IsIntoBase(other);
    }
    
    // 1루 2루 3루 홈
    public void ThrowBase(int index)
    {
        if (!ball.MyDefenderComponent)
        {
            return;
        }
        //bases[index]
    }
}
