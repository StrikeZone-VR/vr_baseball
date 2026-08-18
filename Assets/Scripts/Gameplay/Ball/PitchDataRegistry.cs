/// <summary>
/// 모든 PitchTypeSO asset을 모아 PitchType enum으로 조회하게 해주는 레지스트리.
/// 인스펙터에서 entries 배열에 PitchTypeSO asset 4개(FastBall/Curve/Slider/ForkBall) 드롭.
/// </summary>

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PitchDataRegistry", menuName = "Baseball/Pitch Data Registry", order = 1)]
public class PitchDataRegistry : ScriptableObject
{
    [SerializeField] private PitchTypeSO[] entries;

    private Dictionary<PitchType, PitchTypeSO> _cache;

    private void BuildCache()
    {
        _cache = new Dictionary<PitchType, PitchTypeSO>();
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null) continue;
            _cache[entries[i].pitchType] = entries[i];
        }
    }

    //가져올때마다 혹시 cache가 없다면 mapping
    public PitchTypeSO Get(PitchType type)
    {
        if (_cache == null) BuildCache(); 

        if (_cache.TryGetValue(type, out var data))
            return data;

        Debug.LogWarning($"[PitchDataRegistry] {type}에 해당하는 PitchTypeSO가 없음. entries 인스펙터 확인 요망.");
        return entries != null && entries.Length > 0 ? entries[0] : null;
    }

    public PitchTypeSO[] All => entries;

    private void OnValidate()
    {
        _cache = null; // 인스펙터 수정시 캐시 무효화
    }
}
