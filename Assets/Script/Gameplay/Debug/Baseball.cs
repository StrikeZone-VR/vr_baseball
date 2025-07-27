using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baseball : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Player myPlayer; //handling player

    private const float ADDFORCE = 500.0f;
    bool isGroundBall = false; 
    bool isBatTouch = false; 



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
        
        //_rigidbody.AddForce(new Vector3(transform.rotation.eulerAngles.x, ADDFORCE, transform.rotation.eulerAngles.z));
        _rigidbody.AddForce(force);
        
        isBatTouch = false;
        isGroundBall = false;
    }

    public bool IsGroundBall 
    {
        get => isGroundBall;
        set => isGroundBall = value;
    }

    public Player MyPlayer
    {
        get => myPlayer;
        set => myPlayer = value;
    }

    public void RemovePlayer()
    {
        if (!myPlayer)
        {
            return;
        }
        myPlayer.RemoveBall();
        myPlayer = null;
    }
}
