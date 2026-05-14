using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameBootstrap
{
    private const string PersistentSceneName = "PersistentManager";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePersistentManager()
    {
        var scene = SceneManager.GetSceneByName(PersistentSceneName);
        if (scene.IsValid() && scene.isLoaded) return;

        Debug.Log($"[GameBootstrap] '{PersistentSceneName}' 추가 로드");
        SceneManager.LoadScene(PersistentSceneName, LoadSceneMode.Additive);
    }
}
