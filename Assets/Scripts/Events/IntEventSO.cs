using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "IntEventSO", menuName = "Event/IntEventSO")]
public class IntEventSO : ScriptableObject
{
    public event UnityAction<int> onEventRaised;

    public void RaiseEvent(int value)
    {
        if (onEventRaised != null)
            onEventRaised.Invoke(value);
    }
}
