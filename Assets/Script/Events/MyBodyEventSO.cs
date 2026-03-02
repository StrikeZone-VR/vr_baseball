using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "MyBodyEventSO", menuName = "Event/MyBodyEventSO")]
public class MyBodyEventSO : ScriptableObject
{
    public event UnityAction<MyBody> onEventRaised;
    
    public void RaiseEvent(MyBody mybody)
    {
        if(onEventRaised != null) onEventRaised.Invoke(mybody);
    }
}
