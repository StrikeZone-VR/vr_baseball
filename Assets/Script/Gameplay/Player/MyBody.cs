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
    [SerializeField] private MyPitcherComponent subComponent; //pitcher 

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


    private bool IsIntoBase(Collider other)
    {
        BatterComponent batterComponent = _playerComponent as BatterComponent;
        return batterComponent.IsIntoBase(other);
    }
    

    public MyBatterComponent GetMyBatterComponent()
    {
        MyBatterComponent myBatterComponent = _playerComponent as MyBatterComponent;
        return myBatterComponent;
    }
    public MyPitcherComponent GetMyPitcherComponent()
    {
        return subComponent;
    }
}
