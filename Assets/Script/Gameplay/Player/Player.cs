using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    protected Baseball _myBall = null;

    [SerializeField] protected Baseball _ball;

    protected NavMeshAgent nav;
    private const float BALL_DISTANCE = 0.5f;


    // Start is called before the first frame update
    protected void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
    }




    public void LookAtPlayer(Vector3 target)
    {
        transform.LookAt(target, Vector3.up);

        //x, z => zero because prevent superconductor phenomenon
        transform.rotation = Quaternion.Euler(0.0f, transform.rotation.eulerAngles.y, 0.0f);

        FrontBall();
    }

    protected void FrontBall()
    {
        if (!_myBall)
        {
            return;
        }
        float x = Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        float z = Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.PI / 180);

        //player angle
        _myBall.transform.position = transform.position + new Vector3(BALL_DISTANCE * x, 0, BALL_DISTANCE * z);
    }

    public void RemoveBall()
    {
        _myBall = null;
    }

    public void SetBall(Baseball ball)
    {
        _myBall = ball;
    }
}