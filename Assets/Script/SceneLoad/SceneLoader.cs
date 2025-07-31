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
    private Scene currentScene = default;

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
            //기존의 씬 제거
            if(currentScene != default)
            {
                SceneManager.UnloadSceneAsync(currentScene);
            }
            currentScene = op.Result.Scene;
        };
    }
}
