using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class Batter : Player
{
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    //[SerializeField] private Baseball _ball;
    [SerializeField] protected int base_index = 0;
    protected Transform[] bases;
    [SerializeField] private Bat bat;

    [Header("Listening to Events")]
    [SerializeField] protected VoidEventSO startPitchEvent; //From GameManager
    [SerializeField] protected VoidEventSO addScore; //From GameManager
    [SerializeField] protected VoidEventSO strikeEvent; //From GameManager
    [SerializeField] protected IntEventSO addIsBaseStatus; //From GameManager

    
    //debug serializeField
    [SerializeField] protected bool isMove = false;
    //private bool isInBase = false;
    
    public void SetBat(Bat bat)
    {
        this.bat = bat;
    }

    public void Swing()
    {
        bat.StartSwing();
    }

    private void MoveBase()
    {
        MovePlayer(bases[base_index].position);
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

        if (collision.gameObject.CompareTag("BatterPos"))
        {
            StopMove();
        }
    }

    protected virtual void StopMove()
    {
        nav.ResetPath();
        startPitchEvent.RaiseEvent();
        LookAtPlayer(new Vector3(-1, 0, -1));
    }

    #region PROPERTYS
    public virtual bool IsMove
    {
        get => isMove;
        set
        {
            isMove = value;
            if (isMove)
            {
                //Debug.Log("움직...");
                MoveBase();
            }
            else
            {
                //stop moving
                //Debug.Log("안 움직...");

                nav.ResetPath();
            }
        }
    }

    // want to go base index
    public virtual int BaseIndex
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

    
    #endregion
}
