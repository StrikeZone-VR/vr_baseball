using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Batter : Player
{
    //[SerializeField] private Baseball _ball;
    private int base_index = 0;
    [SerializeField] private Transform[] bases;

    private bool isMove = false;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("잉잉");
            IsMove = !IsMove;
        }
        InBase();
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

    private void InBase()
    {
        if (!isMove)
        {
            return;
        }
        if (base_index >= bases.Length)
        {
            return;
        }
        if (Vector3.Distance(bases[base_index].position, transform.position) <= 0.3f)
        {
            base_index++;
            MoveBase();
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

}
