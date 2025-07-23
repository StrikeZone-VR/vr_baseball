using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class DebugPlayer : MonoBehaviour
{
    private GameObject _myBall = null;
    [SerializeField] private GameObject _ball;
    private NavMeshAgent nav;


    // Start is called before the first frame update
void Start()
{
    nav = GetComponent<NavMeshAgent>();
}

    private void Update()
    {
        if(_myBall)
            _myBall.transform.position = transform.position + new Vector3(0, 0, 0.5f);

        else
        {

            //nav.SetDestination(_ball.transform.position);
            //transform.LookAt(new Vector3(_ball.transform.position.x, 1.0f, _ball.transform.position.z), Vector3.up);

            Vector3 pos = new Vector3(-10, 2, -10);
            nav.SetDestination(pos);
            transform.LookAt(pos);

        }
    }


    //on
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            collision.rigidbody.velocity = Vector3.zero;
            _myBall = collision.gameObject;
        }
    }
}
