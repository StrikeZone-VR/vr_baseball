using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugBaseball : MonoBehaviour
{
    private Rigidbody _rigidbody;

    private const float ADDFORCE = 500.0f;
    bool isGroundBall = false; //¶¥º¼
    bool isBatTouch = false; //¹æ¸ÁÀÌ°¡ °Çµé¾ú´ÂÁö

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

    private void OnCollisionEnter(Collision collision)
    {
        //¸¸¾à ´êÀº °÷ÀÌ ¶¥ÀÌ¶ó¸é => ¶¥º¼
        if(collision.collider.tag == "Ground")
        {
            isGroundBall = true;
            Debug.Log("¶¥¿¡ ´êÀ½");
        }
    }

    public bool IsGroundBall 
    {
        get => isGroundBall;
        set => isGroundBall = value;
    }
}
