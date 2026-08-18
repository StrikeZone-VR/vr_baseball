using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "FloatEventSO", menuName = "Event/FloatEventSO")]
public class FloatEventSO : ScriptableObject
{
    public event UnityAction<float> onEventRaised;

    public void RaiseEvent(float value)
    {
        if (onEventRaised != null)
            onEventRaised.Invoke(value);
    }
}
