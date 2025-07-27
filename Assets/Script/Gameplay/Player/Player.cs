using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    [SerializeField] protected GameObject _myBall = null;

    [SerializeField] protected Baseball _ball;
    [SerializeField] private VoidEventSO outEventSO;

    private NavMeshAgent nav;


    // Start is called before the first frame update
    protected virtual void Start()
    {
        nav = GetComponent<NavMeshAgent>();
    }

    protected virtual void Update()
    {
        if (_myBall)
        {
            _myBall.transform.position = transform.position + new Vector3(0, 0, 0.5f);
            transform.LookAt(_ball.transform, Vector3.up);
            nav.ResetPath();
        }
        else
        {
            nav.SetDestination(_ball.transform.position);
            transform.LookAt(_ball.transform, Vector3.up);

            //x, z => zero because prevent superconductor phenomenon
            transform.rotation = Quaternion.Euler(0.0f, transform.rotation.eulerAngles.y, 0.0f);
        }
    }


    //on
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            Baseball baseball = _myBall.GetComponent<Baseball>();
            collision.rigidbody.velocity = Vector3.zero;
            _myBall = collision.gameObject;

            bool isGroundball = baseball.IsGroundBall;
            baseball.MyPlayer = this;

            if (!isGroundball)
            {
                outEventSO.Raised();
            }
        }
    }

    public void RemoveBall()
    {
        _myBall = null;
    }
}