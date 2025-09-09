using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Batter : Player
{

    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float rotationTime = 1f;

    //[SerializeField] private Baseball _ball;
    private int base_index = 0;
    private Transform[] bases;
    [SerializeField] private GameObject bat;

    private bool isMove = false;
    //private bool isInBase = false;
    
    [SerializeField] private VoidEventSO addScore; //From GameManager
    [SerializeField] private IntEventSO addIsBaseStatus; //From GameManager

    public void DebugHitting()
    {

        //방망이 휘두르는 함수
        bat.transform.rotation = Quaternion.Euler(new Vector3(90f , 0, 0)); //x 90 z 0 =
        
        bat.transform.Rotate(new Vector3(0, 0, 90f));

        //_ball.RemovePlayer();

        //_myBall.transform.position = transform.position + new Vector3(0.0f, 0.5f, 0.0f);
        //IsMove = true;
    }


    public void StartRotation()
    {
        StartCoroutine(RotateWithCurve());
    }

    IEnumerator RotateWithCurve()
    {
        Quaternion startRotation = Quaternion.Euler(90, 0, 0);
        Quaternion endRotation = Quaternion.Euler(0, 0, 90);

        float elapsed = 0f;

        while (elapsed < rotationTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / rotationTime;

            // Animation Curve 적용
            float curveValue = rotationCurve.Evaluate(progress);

            transform.rotation = Quaternion.Lerp(startRotation, endRotation, curveValue);
            yield return null;
        }

        transform.rotation = endRotation;
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
