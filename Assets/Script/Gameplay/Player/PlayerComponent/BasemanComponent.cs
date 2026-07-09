using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasemanComponent : DefenderComponent
{
    [SerializeField] private int base_index; //1 2 3 4
    [SerializeField] private IntEventSO outRunnerEvent;
    [Tooltip("베이스 중심(defenderTransform)에서 이 거리 안이면 '제자리'로 본다. 큰 베이스 트리거 대신 씀(B-2).")]
    [SerializeField] private float inPositionRange = 0.6f;


    private GamePlayModel _gamePlayModel;

    // protected override void Update()
    // {
    //     base.Update();
    // }

    //B-2: IsInPosition을 큰 베이스 트리거가 아니라 '베이스 중심(defenderTransform)과의 거리'로 판정한다.
    //ㄴ 트리거가 커서 가장자리에서도 IsInPosition=true였음 → 중심으로 온 송구를 못 받던 버그.
    //   거리로 판정하면 루수가 중심 근처에 서야만 '제자리'가 되어 실제로 중심까지 걸어가 공을 받는다.
    //ㄴ 트리거 자체는 주자 도착 판정에 계속 쓰이므로 크기는 그대로 두고, 여기선 트리거로 IsInPosition을 안 건드린다.
    protected override void Update()
    {
        UpdateInPositionByDistance();
        base.Update();
    }

    protected virtual void UpdateInPositionByDistance()
    {
        if (defenderTransform == null) return;
        bool now = Vector3.Distance(transform.position, defenderTransform.position) <= inPositionRange;
        if (now != isInPosition) IsInPosition = now; //변할 때만 → IsInPosition setter의 OutRunner 중복 호출 방지
    }

    protected virtual void OnTriggerEnter(Collider collision)
    {
        //B-2: 루수 IsInPosition은 이제 거리 기반(UpdateInPositionByDistance)이라 트리거로 안 켠다.
        //ㄴ 메서드는 CatcherComponent(포수)가 override해서 자기 트리거 방식으로 쓰므로 남겨둔다.
        // if (collision.gameObject.CompareTag("Base"))
        // {
        //     IsInPosition = true;
        // }
    }

    protected virtual void OnTriggerExit(Collider collision)
    {
        //B-2: 위와 동일 — 루수는 거리 기반이라 트리거로 IsInPosition을 끄지 않는다.
        // if (collision.gameObject.CompareTag("Base"))
        // {
        //     IsInPosition = false;
        // }
    }

    //공을 잡은 케이스에만 생김, 만약 공 잡고 베이스 가면 아웃이 안됨
    protected override void OutRunner()
    {
        base.OutRunner(); //isFlying out
        
        if (!_myBall)
        {
            return;
        }
        //베이스 밟은 경우 => 판단은 저쪽에서
        outRunnerEvent.RaiseEvent(base_index - 1); //베이스 이전 값 아웃
    }
    
    
    
    /// <summary>
    /// 대체로 베이스 안에서 true.
    /// </summary>
    public override bool IsInPosition
    {
        get => isInPosition;
        set
        {
            isInPosition = value;

            //어차피 플라잉 아웃은 그 전에 된 게 아닐까
            OutRunner();
        }
    }
}