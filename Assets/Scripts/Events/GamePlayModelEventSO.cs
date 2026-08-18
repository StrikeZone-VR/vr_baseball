using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "GamePlayModelEventSO", menuName = "Event/GamePlayModelEventSO")]
public class GamePlayModelEventSO : ScriptableObject
{
    public event UnityAction<GamePlayModel> onEventRaised;
    
    public void RaiseEvent(GamePlayModel model)
    {
        if(onEventRaised != null) 
            onEventRaised.Invoke(model);
    }
}
