using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent( typeof(PitchingBallController))]
public class Baseball : MonoBehaviour
{
    private Rigidbody _rigidbody;
    [SerializeField] private Defender myDefender; //handling player

    bool isGroundBall = false; 
    bool isBatTouch = false;
    bool isPassing = false;
    bool isSZ = false;
    private float defenderDis = 0.0f;
    
    [Header("Listening to Events")]
    [SerializeField] private VoidEventSO allTrackingOffEvent;
    [SerializeField] private VoidEventSO PaulEvent;
    [SerializeField] private VoidEventSO HomerunEvent;
    [SerializeField] private VoidEventSO backToPitcherEvent; //from gamemanager


    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider collider)
    {
        //Strike Zone
        if (collider.gameObject.CompareTag("SZ") && !isSZ)
        {
            Debug.Log("스트라이크~");
            isSZ = true;
        }
        //homerun
        else if (collider.CompareTag("Homerun"))
        {
            //HomerunEvent.event
            Debug.Log("homerun");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        
        if(collision.collider.CompareTag("Ground"))
        {
            if (!isBatTouch)
            {
                return;
            }
            //paul, homerun check
            if (!isGroundBall)
            {
                IsGroundBall = true;
                IsPassing = false;
                //paul
                if (transform.position.x > 0.0f || transform.position.z > 0.0f)
                {
                    Debug.Log("paul");
                }
                else //other => in play game
                {
                    Debug.Log("in play game");
                }
            }
            
        }
        if (collision.collider.CompareTag("Bat"))
        {
            IsBatTouch = true;

            // 공의 Rigidbody 컴포넌트 가져오기
            Rigidbody batRb = collision.gameObject.GetComponent<Rigidbody>();

            if (batRb != null)
            {
                //계산이 잘못된 듯?
                // 충돌 방향 계산 (배트에서 공으로의 방향)
                Vector3 hitDirection = (collision.GetContact(0).point - transform.position).normalized;

                float speed = collision.transform.GetComponent<Bat>().GetSwingSpeed();
                // Debug.Log("방향 :" + hitDirection);
                Debug.Log("스피드 :" +speed);

                this._rigidbody.AddForce(hitDirection * speed * 2.5f, ForceMode.Impulse);
            }
            //force ball
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.collider.CompareTag("Bat"))
        {
            Debug.Log("공 속도 :" + transform.GetComponent<Rigidbody>().velocity);
        }

    }

    public void ThrowBall(Vector3 force)
    {
        RemovePlayer();
        isPassing = true;
        
        //rotation zero
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.velocity = force;
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
                DefenderDis = 0;
                isPassing = false;
                allTrackingOffEvent.RaiseEvent();
            }
        }
    }

    public float DefenderDis
    {
        get => defenderDis;
        set => defenderDis = value;
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

    public bool IsSZ
    {
        get => isSZ;
        set => isSZ = value;
    }
}
