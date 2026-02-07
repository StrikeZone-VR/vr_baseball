using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//body
public class MyBody : Batter
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Batter prefabBatter;
    [SerializeField] private Transform _parent;

    [SerializeField] private VoidEventSO moveBatterEvent;
    
    void Update()
    {
        transform.position = _camera.transform.position + new Vector3(0, -1.23f, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Base"))
        {
            string s = other.name;
            int a = Convert.ToInt32(s[s.Length - 1]);

            //is same going to the next base index
            if (a - '0' == base_index)
            {
                BaseIndex++; 
            }

            IsMove = false;
            //Debug.Log("베이스를 밟아버렷" + other.transform.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("Base"))
        {
            IsMove = true;
        }
    }
    
    public override void OutPlayer()
    {
        //페이드 아웃
        //이동
        moveBatterEvent.RaiseEvent();
    }

    
    public override bool IsMove
    {
        get => isMove;
        set
        {
            isMove = value;
        }
    }
    
    public override int BaseIndex
    {
        get => base_index;
        set
        {
            if (value < 0)
            {
                return;
            }

            //change base status => else, goto 1base 
            if (0 < value && value < bases.Length)
            {
                moveBatterEvent.RaiseEvent();

                
                //다시 타석으로 => 페이드아웃 + moveEvent.
                //ㄴ GamePlayManager에 있는 ThrowBallAlgorithm가 -1이어야지 출력하는게 나은듯

                //todo : 홈런인 경우 어떡하지
                return;
            }
            
            
            //arrive home
            if (value >= bases.Length)
            {
                addScore.RaiseEvent(); 
                //IsMove = false; => this will be null
                
                return;
            }
            //base_index = 0;
            
            base_index = value;
        }
    }
}
