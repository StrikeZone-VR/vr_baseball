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
    [SerializeField] protected int base_index = 0; // 1 2 3 => 0은 1루 가기 전 상태
   
    protected Transform[] bases;
    [SerializeField] private Bat bat;

    [Header("Listening to Events")]
    [SerializeField] protected VoidEventSO startPitchEvent; //From GameManager
    [SerializeField] protected VoidEventSO addScore; //From GameManager
    [SerializeField] protected VoidEventSO strikeEvent; //From GameManager
    [SerializeField] protected IntEventSO addIsBaseStatus; //From GameManager
    [SerializeField] protected VoidEventSO changedBaseStatus;

    
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
        //Debug.Log("움 직." + base_index.ToString());
        MovePlayer(bases[base_index].position);
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Base"))
        {
            if(IsIntoBase(collision))
            {
                //수비수와 공 거리가 10f 이하면 가지마라
                if (_ball.DefenderDis <= 10.0f)
                {
                    IsMove = false; //일단 움직임을 멈춰야 debug에 찍힘
                }
            }
        }

        if (collision.gameObject.CompareTag("BatterPos"))
        {
            if (base_index == 0)
            {
                StopMove();
            }
        }
    }

    protected bool IsIntoBase(Collider collision)
    {
        string s = collision.name;
        int a = Convert.ToInt32(s[s.Length - 1]);

        //is same going to the next base index
        if (a - '0' == base_index + 1 || (a - '0' == 0 && base_index == 3))
        {
            //혻시 BaseIndex를 IsMove 아래로 둔 이유가 있을까? 
            BaseIndex++;
            return true;
        }
        //1 2 3만
        if (a - '0' == base_index && base_index > 0)
        {
            return true;
        }
        
        return false;
    }

    protected virtual void StopMove()
    {
        nav.ResetPath();
        startPitchEvent.RaiseEvent();
        LookAtPlayer(new Vector3(-1, 0, -1));
    }

    public virtual void OutPlayer(bool isMove = true)
    {
        nav.ResetPath();
        Destroy(this.gameObject);
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
                MoveBase();
            }
            else
            {
                if (nav != null && nav.isActiveAndEnabled && nav.isOnNavMesh)
                {
                    nav.ResetPath();
                }
            }
            changedBaseStatus.RaiseEvent(); 
        }
    }

    // want to go base index => baseindex를 먼저 설정하고 IsMove를 설정하자
    public virtual int BaseIndex
    {
        get => base_index;
        set
        {
            if (value < 0)
            {
                return;
            }

            //arrive home
            if (value >= bases.Length)
            {
                addScore.RaiseEvent(); 
                //IsMove = false; => this will be null
                
                return;
            }
            base_index = value;
            //change base status => else, goto 1base 
            if (0 < value && value < bases.Length)
            {
                //addIsBaseStatus.RaiseEvent(value - 1);
                
                //일단 IsMove로 해서 오류만 생긴다
                //changedBaseStatus.RaiseEvent(); 
            }
        }
    }


    public void SetBases(Transform[] bases)
    {
        this.bases = bases;
    }

    public void SetBaseIndex(int baseIndex)
    {
        BaseIndex = baseIndex;
        IsMove = false;
        //1 => 
        if (base_index == 0)
        {
            Debug.LogError("베이스index가 0인데?");
            return;
        }

        transform.position = bases[baseIndex - 1].position;
        
    }

    
    #endregion
}
