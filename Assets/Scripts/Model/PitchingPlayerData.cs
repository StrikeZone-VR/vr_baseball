using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
//피칭 구종 데이터
public class PitchingPlayerData 
{
    //[SerializeField]가 없으면 private 필드는 유니티가 직렬화하지 않는다 => 저장도 인스펙터 표시도 안 됨
    [SerializeField] private PitchType pitchType;
    //구속이 고민 (최소값 최대값)
    [SerializeField] private float min_velocity;
    [SerializeField] private float max_velocity;

    /// <summary>
    /// 백분율
    /// </summary>
    [SerializeField] private float []correctZone = new float[25];

    public float[] CorrectZone
    {
        get { return correctZone; }
        set { correctZone = value; }
    }
    //명중률 [25] %?
    // 아니면 editor로 PitchingPlayerData를 넣으면 알아서 가중치 100 단위로 하고 해주는 기능
    // 수정이 쉽게쉽게 하는 게 목표

    public PitchType Type => pitchType;
    public float MinVelocity => min_velocity;
    public float MaxVelocity => max_velocity;
}
