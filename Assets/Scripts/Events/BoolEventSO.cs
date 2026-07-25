using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "BoolEventSO", menuName = "Event/BoolEventSO")]
public class BoolEventSO : ScriptableObject
{
    public event UnityAction<bool> onEventRaised;

    public void RaiseEvent(bool value)
    {
        if (onEventRaised != null)
            onEventRaised.Invoke(value);
    }
}
