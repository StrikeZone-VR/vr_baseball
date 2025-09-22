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
    [SerializeField] private int base_index = 0;
    private Transform[] bases;
    GameObject bat;

    [FormerlySerializedAs("pitchStartEvent")]
    [Header("Listening to Events")]
    [SerializeField] private VoidEventSO startPitchEvent; //From GameManager
    [SerializeField] private VoidEventSO addScore; //From GameManager
    [SerializeField] private VoidEventSO strikeEvent; //From GameManager
    [SerializeField] private IntEventSO addIsBaseStatus; //From GameManager

    
    //debug serializeField
    [SerializeField] private bool isMove = false;
    //private bool isInBase = false;
    
    const float rotationTime = 0.25f;
    float elapsed = 0f;

    public void SetBat(GameObject bat)
    {
        this.bat = bat;
    }

    public void StartSwing()
    {
        if(elapsed != 0) return;

        Vector3 start = new Vector3(0, 0, -120);
        Quaternion startRotation = Quaternion.Euler(start);
        bat.transform.rotation = startRotation;

        StartCoroutine(RotateWithCurveSwing(start, new Vector3(-65, -135, -120)));
    }

    IEnumerator RotateWithCurveSwing(Vector3 start, Vector3 end)
    {
        Quaternion startRotation = Quaternion.Euler(start);
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
        
        //스윙했는데 만약 공에 안 닿았다면
        if (!_ball.IsBatTouch)
        {
            Debug.Log("스트라이크 막아놓음");
            //strikeEvent.RaiseEvent();
        }
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

    private void StopMove()
    {
        nav.ResetPath();
        startPitchEvent.RaiseEvent();
        LookAtPlayer(new Vector3(-1, 0, -1));
    }

    #region PROPERTYS
    public bool IsMove
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
    #endregion
}
