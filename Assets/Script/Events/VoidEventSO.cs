using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "VoidEventSO", menuName = "Event/VoidEventSO")]
public class VoidEventSO : ScriptableObject
{
    public event UnityAction onEventRaised;
    
    public void Raised()
    {
        if(onEventRaised != null) onEventRaised.Invoke();
    }
}
