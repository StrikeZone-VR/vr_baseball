using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Batter : Player
{
    //[SerializeField] private Baseball _ball;
    private int base_index = 0;
    [SerializeField] private Transform[] bases;

    private bool isMove = false;
    private bool isInBase = false;
    
    [SerializeField] private VoidEventSO addScore; //From GameManager

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            IsMove = !IsMove;
        }
        GoToBase();
    }

    public void DebugHitting()
    {
        //_ball.RemovePlayer();

        //_myBall.transform.position = transform.position + new Vector3(0.0f, 0.5f, 0.0f);
        IsMove = true;
    }

    private void MoveBase()
    {
        nav.SetDestination(bases[base_index].position);
        LookAtPlayer(bases[base_index].position);
    }

    private void GoToBase()
    {
        if (!isMove)
        {
            return;
        }
        
        Vector3 base_pos = new Vector3(bases[base_index].position.x, 1f, bases[base_index].position.z);
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            Debug.Log("BaseIndex : " + BaseIndex);
            BaseIndex++;
        }
    }


    public bool IsMove
    {
        get => isMove;
        set
        {
            isMove = value;
            if (isMove)
            {
                MoveBase();
            }
        }
    }

    public int BaseIndex
    {
        get => base_index;
        set
        {
            if (value < 0 )
            {
                return;
            }
            //arrive home
            if (value >= bases.Length)
            {
                addScore.Raised();
                IsMove = false;
                
                return;
            }
            base_index = value;
            MoveBase();
        }
    }

}
