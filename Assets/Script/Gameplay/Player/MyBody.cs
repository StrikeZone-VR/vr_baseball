using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


//body
public class MyBody : BatterComponent
{
    //투수면서 타자지만 투수의 기능으로는 던지기 기능밖에 없으나 그 던지기마저 컨트롤러가 제어할 수 있어서 Batter만 상속받았다
    [Header("연결할 오브젝트")]
    [SerializeField] private XRBaseInteractor handInteractor; // 직접 잡는 손 (XR Direct Interactor right)
    [SerializeField] private Camera _camera;

    //GamePlayManager's onCanBackBatterEvent
    [SerializeField] private VoidEventSO moveBatterEvent;
    private bool isOut = false;

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
                IsMove = false;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("Base"))
        {
            IsMove = true;
        }
    }
    
    public override void OutPlayer(bool isMove = true)
    {
        Debug.Log("[Batter] : My body out : " + isMove);
        IsOut = true;
        BaseIndex = 0;
        if (isMove)
            moveBatterEvent.RaiseEvent(); 
    }

    

    
    public override bool IsMove
    {
        get => isMove;
        set
        {
            isMove = value;
            // if (isMove)
            // {
            //     Debug.Log("run : " + base_index);
            // }
            // else //어 근데 아마 전 값 때문에 +1을 해야할지도?
            // {
            //     Debug.Log("stop : " + (base_index));
            // }
            changedBaseStatus.RaiseEvent();
        }
    }
    
    public override int BaseIndex
    {
        get => base_index;
        set
        {
            if (value < 0)
            {
                return;
            }

            //arrive home
            if (value >= bases.Length)
            {
                addScore.RaiseEvent(); 
                //IsMove = false; => this will be null
                
                return;
            }
            
            base_index = value;
            //change base status => else, goto 1base 
            if (0 < value && value < bases.Length)
            {
                //Debug.Log("성공 : " + base_index);
                //다시 타석으로 => 페이드아웃 + moveEvent.
                //addIsBaseStatus.RaiseEvent(value - 1);
                moveBatterEvent.RaiseEvent();
                //changedBaseStatus.RaiseEvent();
                
                //ㄴ GamePlayManager에 있는 ThrowBallAlgorithm가 -1이어야지 출력하는게 나은듯

                //todo : 홈런인 경우 어떡하지
                return;
            }
            //base_index = 0;
        }
    }
    

    public bool IsOut
    {
        get => isOut;
        set => isOut = value;
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
        handInteractor.interactionManager.SelectEnter(handInteractor, _ball.GrabInteractable);
        
        Debug.Log("⚾ B버튼 클릭: 야구공을 강제로 잡았습니다!");
    }

    
    // 1루 2루 3루 홈
    public void ThrowBase(int index)
    {
        if (!_ball.MyDefender)
        {
            return;
        }
        //bases[index]
    }
}
