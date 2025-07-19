using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugBaseball : MonoBehaviour
{
    private Rigidbody _rigidbody;

    private const float ADDFORCE = 500.0f;
    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            _rigidbody.AddForce(new Vector3(0.0f,ADDFORCE,ADDFORCE));

    }
}
