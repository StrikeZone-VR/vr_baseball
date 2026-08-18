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
    [SerializeField] private float weight = 1;

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
    public float Weight { get { return weight; } set { weight = value; } }

    /// <summary>
    /// correctZone 가중치로 존 인덱스를 하나 뽑는다.
    /// 합이 100이 아니어도 총합으로 나누므로 상관없다.
    /// </summary>
    /// <returns>StrikeZone.GetZone에 넣을 인덱스. 데이터가 없으면 -1</returns>
    public int PickZoneIndex()
    {
        if (correctZone == null || correctZone.Length == 0)
        {
            return -1;
        }
        return WeightedRandom.Pick(correctZone.Length, i => correctZone[i]);
    }

    //구속 분포의 뾰족한 정도. 올리면 가운데에 더 몰리고, 1로 내리면 균등 분포가 된다. 3정도가 정규 분포
    private const int VELOCITY_SAMPLE_COUNT = 3;

    /// <summary>
    /// min~max 사이에서 구속을 뽑는다. 가운데가 자주, 양 끝이 드물게 나오는 정규분포 모양.
    ///
    /// ㄴ 균등난수 여러 개를 평균내면 종 모양이 된다(중심극한정리).
    ///    3개일 때 표준편차가 정확히 (max-min)/6 이라서, min~max 가 평균 ±3σ 구간이 된다.
    ///    정규분포에서 ±3σ 안에 99.7%가 들어오는 것과 같은 범위라 의도한 분포와 맞아떨어진다.
    ///    예) 122~125 => 평균 123.5, σ 0.5
    ///
    /// ㄴ Box-Muller 같은 "교과서" 정규분포는 꼬리가 무한해서 clamp가 필요한데,
    ///    그러면 잘린 값들이 min/max 경계에 뭉쳐서 오히려 끝값이 자주 나온다.
    ///    이 방식은 범위를 절대 안 벗어나므로 그 문제가 없다.
    /// </summary>
    /// <returns>km/h. 구속을 입력 안 했으면 0 (호출부가 폴백하도록)</returns>
    public float PickVelocity()
    {
        if (min_velocity <= 0f && max_velocity <= 0f)
        {
            return 0f;
        }

        //인스펙터에서 min > max 로 넣어도 터지지 않게 정렬
        float lo = Mathf.Min(min_velocity, max_velocity);
        float hi = Mathf.Max(min_velocity, max_velocity);

        float t = 0f;
        for (int i = 0; i < VELOCITY_SAMPLE_COUNT; i++)
        {
            t += UnityEngine.Random.value;
        }
        t /= VELOCITY_SAMPLE_COUNT;

        return Mathf.Lerp(lo, hi, t);
    }
}
