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
            collision.rigidbody.velocity = Vector3.zero;
            _myBall = collision.gameObject;
        }
    }
}