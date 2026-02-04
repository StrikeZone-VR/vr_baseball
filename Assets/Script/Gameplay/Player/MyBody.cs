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
    
    // => 투수모드면 이거를 끄고
    // => 타자모드면 이거를 키고
    
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
                GameObject batter = Instantiate(prefabBatter.gameObject, _parent);
                
                batter.transform.position = transform.position;//프리펩 정보 이전
                GetComponent<Batter>().BaseIndex = base_index;
                GetComponent<Batter>().IsMove = false;
                    //일단 자기 자신에 투영. 이후 진짜 생성해서 currentBatter의 포지션, baseIndex에 넣고
                    //다시 baseIndex = 0, 포지션 원래자리 => 이거는 그냥 이동시키면 될듯
                    
                BaseIndex = 0;
                Debug.Log("움직임");
                //todo : 다시 타석으로 => 페이드아웃 + moveEvent
                
                addIsBaseStatus.RaiseEvent(value - 1);
            }
            
            
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
}
