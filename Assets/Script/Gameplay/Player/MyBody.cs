using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//body
public class MyBody : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    
    // Update is called once per frame
    void Update()
    {
        transform.position = _camera.transform.position + new Vector3(0, -1.23f, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Base"))
        {
            //Debug.Log("베이스를 밟아버렷" + other.transform.name);
        }
    }
}
