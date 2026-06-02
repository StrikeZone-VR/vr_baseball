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
    
    [SerializeField] private AssetReference GameMenuScene;
    //[SerializeField] private AssetReference BatterScene;
    //[SerializeField] private AssetReference PitcherScene;

    private Scene currentScene;

    private void Start()
    {
        if (HasOtherSceneLoaded())
        {
            currentScene = FindFirstNonPersistentScene();
            return;
        }

        LoadScene(GameMenuScene);
    }

    private bool HasOtherSceneLoaded()
    {
        string mySceneName = gameObject.scene.name;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name != mySceneName) return true;
        }
        return false;
    }

    private Scene FindFirstNonPersistentScene()
    {
        string mySceneName = gameObject.scene.name;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name != mySceneName) return s;
        }
        return default;
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
        
        //unload
        if (currentScene != default)
        {
            SceneManager.UnloadSceneAsync(currentScene);
        }
        // AssetReference가 이미 로드되었는지 확인
        if (scene.OperationHandle.IsValid() && scene.OperationHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("씬이 이미 로드됨 - 기존 씬 참조 사용");

            // 이미 로드된 SceneInstance 가져오기
            SceneInstance sceneInstance = (SceneInstance)scene.OperationHandle.Result;
            Scene op = sceneInstance.Scene;
        
            // 씬이 비활성화되어 있다면 활성화
            if (!op.isLoaded)
            {
                Debug.LogWarning("씬이 언로드된 상태입니다.");
                return;
            }

            currentScene = op;
            // 필요한 후처리 (씬 활성화, 카메라 설정 등)
            Debug.Log($"씬 준비 완료: {currentScene.name}");
        }
        else
        {
            Debug.Log("새로운 씬 로드 시작");
        
            // 새로 로드
            scene.LoadSceneAsync(LoadSceneMode.Additive, true).Completed += (AsyncOperationHandle<SceneInstance> op) => 
            {
                currentScene = op.Result.Scene;
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"씬 준비 완료: {currentScene.name}");
                }
            };
        
        }
        
        // if (currentScene != default)
        // {
        //     SceneManager.UnloadSceneAsync(currentScene);
        // }
        // scene.LoadSceneAsync(LoadSceneMode.Additive, true).Completed += (AsyncOperationHandle<SceneInstance> op) => 
        // {
        //     currentScene = op.Result.Scene;
        // };
    }

    public void QuitGame()
    {
        Application.Quit();        
    }
}
