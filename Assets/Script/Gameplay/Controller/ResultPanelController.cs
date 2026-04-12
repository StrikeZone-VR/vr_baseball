using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ResultPanelController : MonoBehaviour
{
    [SerializeField] private AssetReference gameMenu;
    [SerializeField] private AssetReference gameReadyScene;
    
    [SerializeField] private SceneEventSO sceneEvent;

    public void OnGameMenu()
    {
        sceneEvent.RaiseEvent(gameMenu);
    }
    public void OnGameReady()
    {
        sceneEvent.RaiseEvent(gameReadyScene);
    }
}
