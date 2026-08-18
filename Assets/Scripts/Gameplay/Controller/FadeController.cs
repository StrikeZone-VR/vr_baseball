using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class FadeController : MonoBehaviour
{
    [SerializeField] private FadeChannelSO _fadeChannelSO;
    [SerializeField] private Image _imageComponent;

    [Tooltip("씬이 바뀌었을 때 남아있는 검은 화면을 걷어내는 시간. 0이면 즉시.")]
    [SerializeField] private float _sceneLoadClearDuration = 0.25f;

    private void OnEnable()
    {
        _fadeChannelSO.OnEventRaised += InitiateFade;

        //두 개 다 거는 이유: SceneLoader는 새로 로드하는 경로(sceneLoaded 발생)와
        //이미 로드된 씬을 재사용하는 경로(로드가 없어 sceneLoaded 미발생)가 있다.
        //둘 다 SetActiveScene은 호출하므로 activeSceneChanged로 나머지를 덮는다. (ClearFade는 여러 번 불려도 무해)
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        _fadeChannelSO.OnEventRaised -= InitiateFade;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void Update()
    {
    }

    private void InitiateFade(bool fadeIn, float duration, Color desiredColor)
    {
        //진행 중인 트윈을 먼저 정리한다. DOBlendableColor는 '호출 시점의 색'에서 목표까지의 차이를 계산해
        //그만큼 더하는 방식이라(DOTweenModuleUI: endValue -= target.color), 이전 트윈이 살아서 같이 색을
        //밀고 있으면 최종 색이 목표에서 어긋난다. 죽여도 현재 색은 그대로 남으므로 새 트윈이 정확히 도착한다.
        _imageComponent.DOKill();
        _imageComponent.DOBlendableColor(desiredColor, duration);
    }

    //씬이 바뀌면 무조건 화면을 걷어낸다.
    //ㄴ 페이드는 FadeOut → 대기 → FadeIn 순서로 씬 쪽 코루틴(GamePlayManager.TranslateBattingView 등)이 굴린다.
    //   그런데 마지막 이닝 3아웃이면 그 대기 도중 Inning++ → sceneEventSO로 결과씬 전환이 걸리고,
    //   Gameplay 씬이 언로드되면서 GamePlayManager와 함께 코루틴이 죽어 FadeIn이 영영 호출되지 않는다.
    //   FadeController(와 검은 Image)는 PersistentManager에 있어 살아남으므로 화면만 검은 채로 남는다.
    //   → GameResult로 넘어가면 까만 화면 그대로였던 버그.
    //씬 로드 = 화면이 보여야 하는 시점이므로, 남아있는 페이드를 여기서 정리한다.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearFade();
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        ClearFade();
    }

    private void ClearFade()
    {
        if (_imageComponent == null) return;

        //죽은 코루틴이 남긴 FadeOut 트윈이 아직 돌고 있을 수 있다(트윈은 씬 언로드와 무관하게 계속 돈다).
        _imageComponent.DOKill();

        if (_sceneLoadClearDuration <= 0f)
        {
            _imageComponent.color = Color.clear;
            return;
        }

        _imageComponent.DOBlendableColor(Color.clear, _sceneLoadClearDuration);
    }
}
