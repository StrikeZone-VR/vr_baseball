using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class MyXROriginManager : MonoBehaviour
{
    [SerializeField] private Vector3EventSO moveOriginEvent;
    [SerializeField] private Vector3EventSO rotateOriginEvent;
    [SerializeField] private XROrigin _origin;
    private void OnEnable()
    {
        moveOriginEvent.onEventRaised += MoveOrigin;
        rotateOriginEvent.onEventRaised += RotateOrigin;
    }
    private void OnDisable()
    {
        moveOriginEvent.onEventRaised -= MoveOrigin;
        rotateOriginEvent.onEventRaised -= RotateOrigin;
    }

    private void MoveOrigin(Vector3 vector3)
    {
        //move
        _origin.MoveCameraToWorldLocation(vector3);
        
    }
    private void RotateOrigin(Vector3 vector3)
    {
        //move
        _origin.transform.rotation = Quaternion.Euler(vector3);
        
    }
}
