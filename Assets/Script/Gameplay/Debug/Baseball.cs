using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baseball : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Defender myDefender; //handling player

    bool isGroundBall = false; 
    bool isBatTouch = false;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        
        if(collision.collider.tag == "Ground")
        {
            isGroundBall = true;
        }
    }

    public void ThrowBall(Vector3 force)
    {
        RemovePlayer();
        
        //rotation zero
        //_rigidbody.useGravity = false;
        _rigidbody.velocity = Vector3.zero;
        // _rigidbody.angularVelocity = Vector3.zero;
        // _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        //_rigidbody.AddForce(force, ForceMode.Impulse);
        
        _rigidbody.velocity = force;
        //_rigidbody.constraints = RigidbodyConstraints.None;
        //_rigidbody.AddTorque(force * 1000f, ForceMode.Impulse);
        
        isBatTouch = false;
        isGroundBall = false;
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
        set => myDefender = value;
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
