using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GetGameOBjectEventSO", menuName = "Event/GetGameOBjectEventSO")]
public class GetGameObjectSetIntEventSO : ScriptableObject
{
    public event Func<int, GameObject> onEventRaised;

    public GameObject RaiseEvent(int index)
    {
        if (onEventRaised != null)
            return onEventRaised.Invoke(index);
        return null;
    }
}
