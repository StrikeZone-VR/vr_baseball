using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;


public class SceneLoader : MonoBehaviour
{
    [SerializeField] private SceneEventSO sceneEventSO;
    
    [SerializeField] private AssetReference GameReadyScene;
    //[SerializeField] private AssetReference BatterScene;
    //[SerializeField] private AssetReference PitcherScene;

    private Scene currentScene;

    private void Start()
    {
        currentScene = SceneManager.GetActiveScene();
    }

    private void OnEnable()
    {
        sceneEventSO.onEventRaised += LoadScene;
    }

    private void OnDisable()
    {
        sceneEventSO.onEventRaised -= LoadScene;
    }


    private void LoadScene(AssetReference scene)
    {
        scene.LoadSceneAsync(LoadSceneMode.Additive, true).Completed += (AsyncOperationHandle<SceneInstance> op) => 
        {
            if(currentScene != default)
            {
                SceneManager.UnloadSceneAsync(currentScene);
            }
            currentScene = op.Result.Scene;
        };
    }

    public void QuitGame()
    {
        Application.Quit();        
    }
}
