using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResultManager : MonoBehaviour
{
    [SerializeField] private ResultPanelController resultPanel;

    [SerializeField] private Vector3 initPosition;
    [SerializeField] private Vector3 initRotation;

    [Header("Listening to EventChannels")]
    [SerializeField] private Vector3EventSO moveOriginEvent;
    [SerializeField] private Vector3EventSO rotateOriginEvent;
    void Start()
    {
        resultPanel.UpdateResultUI();

        //moveOriginEvent(=MoveOrigin)는 높이를 현재 카메라 높이로 유지해서 수평 이동만 한다.
        //ㄴ 바닥이 y=0인 다른 씬에선 맞지만, 이 씬은 바닥이 y=1이라 발이 바닥 아래로 들어가
        //   눈높이가 그만큼 낮아 보인다(170 → 140 체감). 그래서 리그 루트를 바닥에 정확히 올리는 경로를 쓴다.
        MyXROriginManager xrOrigin = FindAnyObjectByType<MyXROriginManager>();
        if (xrOrigin != null)
        {
            xrOrigin.MoveOriginToGround(initPosition, initPosition.y);
        }
        else
        {
            moveOriginEvent.RaiseEvent(initPosition); //폴백: 못 찾으면 기존 경로(높이 유지)
        }

        rotateOriginEvent.RaiseEvent(initRotation);
    }
}
