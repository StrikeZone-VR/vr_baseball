using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Batter : Player
{
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    //[SerializeField] private Baseball _ball;
    private int base_index = 0;
    private Transform[] bases;
    [SerializeField] private GameObject bat;

    
    [SerializeField] private VoidEventSO addScore; //From GameManager
    [SerializeField] private IntEventSO addIsBaseStatus; //From GameManager

    
    private bool isMove = false;
    //private bool isInBase = false;
    
    const float rotationTime = 0.25f;
    float elapsed = 0f;
    
    public void DebugHitting()
    {
        Debug.Log("타자 타임 : " + Time.time);
        StartRotation();

        //_ball.RemovePlayer();

        //_myBall.transform.position = transform.position + new Vector3(0.0f, 0.5f, 0.0f);
        //IsMove = true;
    }


    public void StartRotation()
    {
        if(elapsed != 0) return;
        
        StartCoroutine(RotateWithCurve(new Vector3(0, 0, -120), new Vector3(135, 135, -120)));
    }

    IEnumerator RotateWithCurve(Vector3 start, Vector3 end)
    {
        // Quaternion startRotation = Quaternion.Euler(start);
        Quaternion endRotation = Quaternion.Euler(end);

        while (elapsed < rotationTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / rotationTime;

            // Animation Curve
            float curveValue = rotationCurve.Evaluate(progress);

            bat.transform.localRotation = Quaternion.Euler(start * (1 - curveValue) + end * curveValue);
            yield return null;
        }

        elapsed = 0;
        bat.transform.rotation = endRotation;
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
