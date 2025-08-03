using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Baseball : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Defender myDefender; //handling player

    [SerializeField] bool isGroundBall = false; 
    [SerializeField] bool isBatTouch = false;
    [SerializeField] bool isPassing = false;
    [SerializeField] private VoidEventSO allTrackingOffEvent;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.CompareTag("Ground"))
        {
            IsGroundBall = true;
            IsPassing = false;
        }
    }

    public void ThrowBall(Vector3 force)
    {
        RemovePlayer();
        isPassing = true;
        
        //rotation zero
        //_rigidbody.useGravity = false;
        _rigidbody.velocity = Vector3.zero;
        // _rigidbody.angularVelocity = Vector3.zero;
        // _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        //_rigidbody.AddForce(force, ForceMode.Impulse);
        _rigidbody.velocity = force;
        //_rigidbody.constraints = RigidbodyConstraints.None;
        //_rigidbody.AddTorque(force * 1000f, ForceMode.Impulse);
        
        //isBatTouch = false;
        //isGroundBall = false;
    }

    public bool IsPassing
    {
        get => isPassing;
        set => isPassing = value;
    }
    public bool IsGroundBall 
    {
        get => isGroundBall;
        set => isGroundBall = value;
    }
    public bool IsBatTouch 
    {
        get => isBatTouch;
        set => isBatTouch = value;
    }

    public Defender MyDefender
    {
        get => myDefender;
        set
        {
            myDefender = value;
            if (myDefender)
            {
                isPassing = false;
                allTrackingOffEvent.Raised();
            }
        }
    }

    public void RemovePlayer()
    {
        if (!myDefender)
        {
            return;
        }
        myDefender.RemoveBall();
        myDefender = null;
    }
}
