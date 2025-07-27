using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SceneEventSO", menuName = "Event/SceneEventSO")]
public class SceneEventSO : ScriptableObject
{
    public event UnityAction<AssetReference> onEventRaised;

    public void RaiseEvent(AssetReference asset)
    {
        if (onEventRaised != null)
            onEventRaised.Invoke(asset);
    }
}
