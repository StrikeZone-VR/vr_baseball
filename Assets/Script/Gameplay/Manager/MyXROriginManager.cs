using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MyXROriginManager : MonoBehaviour
{
    [SerializeField] private Vector3EventSO moveOriginEvent;
    [SerializeField] private Vector3EventSO rotateOriginEvent;
    [SerializeField] private MyBodyEventSO bodyEvent;
    [SerializeField] private BoolEventSO setPlayerMoveMode;
    
    [Header("플레이어 이동 컨트롤러")]
    [SerializeField] private ActionBasedContinuousMoveProvider moveProvider;
    [SerializeField] private XROrigin _origin;
    

    private void OnEnable()
    {
        moveOriginEvent.onEventRaised += MoveOrigin;
        rotateOriginEvent.onEventRaised += RotateOrigin;
        bodyEvent.onEventRaised += SetPlayer;
        setPlayerMoveMode.onEventRaised += SetPlayerMoveMode;
    }
    private void OnDisable()
    {
        moveOriginEvent.onEventRaised -= MoveOrigin;
        rotateOriginEvent.onEventRaised -= RotateOrigin;
        bodyEvent.onEventRaised -= SetPlayer;
        setPlayerMoveMode.onEventRaised -= SetPlayerMoveMode;
    }

    private void MoveOrigin(Vector3 vector3)
    {
        //move
        _origin.MoveCameraToWorldLocation(vector3);
        
    }
    private void RotateOrigin(Vector3 vector3)
    {
        Debug.Log(vector3);
        //move
        _origin.MatchOriginUpCameraForward(Vector3.up, vector3);
    }

    private void SetPlayer(MyBody body)
    {
        body.SetCamera(_origin.Camera);
    }
    
    private void SetPlayerMoveMode(bool isMove)
    {
        moveProvider.enabled = isMove;
    } 
}
