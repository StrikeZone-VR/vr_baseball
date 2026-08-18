using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


//body
public class MyBody : Player
{
    [Header("VR 플레이어 전용 장비창")]
    // Sub, Main 같은 이름 대신 역할을 아주 명확하게 명시!
    [SerializeField] private MyBatterComponent batterRole;
    [SerializeField] private MyPitcherComponent pitcherRole;

    [SerializeField] private Camera _camera;

    
    //override
    protected override void Awake()
    {
        batterRole = GetComponentInParent<MyBatterComponent>();
        pitcherRole = GetComponentInParent<MyPitcherComponent>();
    }

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
            FieldBase fieldBase = other.GetComponent<FieldBase>();
            if (!fieldBase)
            {
                Debug.LogWarning("fieldBase가 없음");
                return;
            }

            if(IsIntoBase(fieldBase))
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
        
        if (!batterComponent)
        {
            Debug.LogWarning("Mode가 체인지 되면서 이 메세지가 뜰 수 있음");
            return;
        }
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


    private bool IsIntoBase(FieldBase fieldBase)
    {
        BatterComponent batterComponent = _playerComponent as BatterComponent;

        //왜 없지 스위칭 안했나
        if (!batterComponent)
        {
            if (_playerComponent == null)
            {
                Debug.LogError("batterComponent가 없다.");
            }
            return false;
        }
        
        return batterComponent.IsIntoBase(fieldBase);
    }

    public void SetPitcherComponent()
    {
        SetActiveRole(pitcherRole);
    }
    public void SetBatterComponent()
    {
        SetActiveRole(batterRole);
    }
    

    public MyBatterComponent GetMyBatterComponent()
    {
        MyBatterComponent myBatterComponent = _playerComponent as MyBatterComponent;
        return myBatterComponent;
    }
    public MyPitcherComponent GetMyPitcherComponent()
    {
        MyPitcherComponent myPitcherComponent = _playerComponent as MyPitcherComponent;
        return myPitcherComponent;
    }
}
