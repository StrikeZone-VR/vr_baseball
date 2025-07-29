using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    [Tooltip("null")]
    [SerializeField] protected Baseball _myBall = null;

    [SerializeField] protected Baseball _ball;

    protected NavMeshAgent nav;


    // Start is called before the first frame update
    protected virtual void Start()
    {
        nav = GetComponent<NavMeshAgent>();
    }




    public void LookAtPlayer(Vector3 target)
    {
        transform.LookAt(target, Vector3.up);

        //x, z => zero because prevent superconductor phenomenon
        transform.rotation = Quaternion.Euler(0.0f, transform.rotation.eulerAngles.y, 0.0f);
    }

    public void RemoveBall()
    {
        _myBall = null;
    }
}