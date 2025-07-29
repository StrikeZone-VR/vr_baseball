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

    private const float BALL_DISTANCE = 0.5f;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        nav = GetComponent<NavMeshAgent>();
    }

    protected virtual void Update()
    {
        if (!_myBall)
        {
            nav.SetDestination(_ball.transform.position);
            LookAtPlayer(_ball.transform.position);
        }
        else
        {
            float x = Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.PI / 180);
            float z = Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.PI / 180);

            //player angle
            _myBall.transform.position = transform.position + new Vector3(BALL_DISTANCE * x, 0, BALL_DISTANCE * z);
        }
    }


    //on
    void OnCollisionEnter(Collision collision)
    {
        //touch ball
        if (collision.gameObject.CompareTag("Ball"))
        {
            SetMyBall(collision.gameObject);
            Baseball baseball = _myBall.GetComponent<Baseball>();
            collision.rigidbody.velocity = Vector3.zero;

            bool isGroundball = baseball.IsGroundBall;
            baseball.MyPlayer = this;

            if (!isGroundball)
            {
                outEventSO.Raised();
            }
        }
    }

    public void LookAtPlayer(Vector3 target)
    {
        transform.LookAt(target, Vector3.up);

        //x, z => zero because prevent superconductor phenomenon
        transform.rotation = Quaternion.Euler(0.0f, transform.rotation.eulerAngles.y, 0.0f);
    }

    public void SetMyBall(GameObject myBall)
    {
        _myBall = myBall;

        transform.LookAt(_ball.transform, Vector3.up);
        nav.ResetPath();
    }

    public void RemoveBall()
    {
        _myBall = null;
    }
}