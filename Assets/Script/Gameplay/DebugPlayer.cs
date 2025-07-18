using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DebugPlayer : MonoBehaviour
{
    private GameObject _ball = null;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Update()
    {
        if(_ball)
            _ball.transform.position = transform.position + new Vector3(0, 0, -0.5f);
    }


    //on
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            collision.rigidbody.velocity = Vector3.zero;
            _ball = collision.gameObject;
        }
    }
}
