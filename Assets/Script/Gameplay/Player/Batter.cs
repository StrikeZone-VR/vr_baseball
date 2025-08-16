using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Batter : Player
{
    //[SerializeField] private Baseball _ball;
    private int base_index = 0;
    private Transform[] bases;

    private bool isMove = false;
    //private bool isInBase = false;
    
    [SerializeField] private VoidEventSO addScore; //From GameManager
    [SerializeField] private IntEventSO addIsBaseStatus; //From GameManager

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

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            string s = collision.name; 
            int a = Convert.ToInt32(s[s.Length - 1]);

            //is same going to the next base index
            if (a - '0' == base_index)
            {
                BaseIndex++; 
                if (_ball.DefenderDis <= 10.0f)
                {
                    isMove = false;
                    return;
                }
            }
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
            else
            {
                //stop moving
                nav.ResetPath();
            }
        }
    }

    // want to go base index
    public int BaseIndex
    {
        get => base_index;
        set
        {
            if (value < 0 )
            {
                return;
            }

            //change base status => else, goto 1base 
            if (0 < value && value < bases.Length)
                addIsBaseStatus.RaiseEvent(value - 1);
            //arrive home
            if (value >= bases.Length)
            {
                addScore.RaiseEvent(); 
                //IsMove = false; => this will be null
                
                return;
            }
            base_index = value;
        }
    }

    protected override void FrontBall()
    {
        //don't play
    }

    public void SetBases(Transform[] bases)
    {
        this.bases = bases;
    }
}
