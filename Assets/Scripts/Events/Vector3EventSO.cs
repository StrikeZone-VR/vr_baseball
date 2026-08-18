using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "Vector3EventSO", menuName = "Event/Vector3EventSO")]
public class Vector3EventSO : ScriptableObject
{
    public event UnityAction<Vector3> onEventRaised;
    
    public void RaiseEvent(Vector3 vector3)
    {
        if(onEventRaised != null) onEventRaised.Invoke(vector3);
    }
}
