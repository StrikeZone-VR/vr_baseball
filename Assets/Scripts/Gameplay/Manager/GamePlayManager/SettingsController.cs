using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    private const string LanguagePrefKey = "settings.language";
    private const string VolumePrefKey = "settings.volume";
    private const string VibratePrefKey = "settings.vibrate";
    private const string DefaultLocaleCode = "ko";
    private const float DefaultVolume = 1.0f;
    private const float DefaultVibrate = 0.5f;

    [Header("UI 패널")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button quitButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField] private Slider vibrateSlider;
    [SerializeField] private TMP_Text vibrateValueText;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [Tooltip("SettingPanel/Footer/ResetButton")]
    [SerializeField] private Button resetButton;
    [Tooltip("SettingPanel/Footer/SaveButton")]
    [SerializeField] private Button saveButton;

    [Header("오디오 연결")]
    [SerializeField] private AudioManager audioManager;

    [Header("인풋 액션 연결")]
    [Tooltip("VR 컨트롤러 등에서 메뉴 토글. 비워두면 키보드 P만 사용.")]
    [SerializeField] private InputActionReference toggleMenuAction;
    [Tooltip("PC 키보드 단축키 (테스트용).")]
    [SerializeField] private Key keyboardToggleKey = Key.P;

    [Header("VR 패널 배치")]
    [Tooltip("플레이어 머리(메인 카메라) Transform.")]
    [SerializeField] private Transform headTransform;
    [Tooltip("이동시킬 패널의 루트 (보통 SettingPanel의 부모 Canvas).")]
    [SerializeField] private Transform panelRootCanvas;
    [Tooltip("머리 정면으로부터 떨어진 거리 (월드 단위, 미터).")]
    [SerializeField] private float spawnDistance = 2.0f;
    [Tooltip("머리 높이 기준 수직 오프셋. 0이면 시선 높이.")]
    [SerializeField] private float verticalOffset = 0f;

    private Transform _originalParent;
    private Vector3 _originalLocalPosition;
    private Quaternion _originalLocalRotation;
    private bool _hasOriginalTransform;

    private IEnumerator Start()
    {
        WarnIfMissingReferences();
        CacheOriginalTransform();
        InitializeVolume();
        InitializeVibrate();
        if (settingsPanel != null) settingsPanel.SetActive(false);

        yield return LocalizationSettings.InitializationOperation;

        if (LocalizationSettings.AvailableLocales == null
            || LocalizationSettings.AvailableLocales.Locales == null
            || LocalizationSettings.AvailableLocales.Locales.Count == 0)
        {
            Debug.LogWarning("[SettingsController] Available Locales가 비어있음. Project Settings > Localization에서 Locale을 추가해야 언어 전환이 동작함.");
            yield break;
        }

        ApplySavedLanguage();
        PopulateLanguageDropdown();
    }

    private void OnEnable()
    {
        if (toggleMenuAction != null && toggleMenuAction.action != null)
        {
            toggleMenuAction.action.Enable();
            toggleMenuAction.action.started += TogglePanel;
        }

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (vibrateSlider != null)
            vibrateSlider.onValueChanged.AddListener(OnVibrateChanged);

        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);
    }

    private void OnDisable()
    {
        if (toggleMenuAction != null && toggleMenuAction.action != null)
        {
            toggleMenuAction.action.started -= TogglePanel;
            toggleMenuAction.action.Disable();
        }

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

        if (vibrateSlider != null)
            vibrateSlider.onValueChanged.RemoveListener(OnVibrateChanged);

        if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(OnResetClicked);

        if (saveButton != null)
            saveButton.onClick.RemoveListener(OnSaveClicked);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[keyboardToggleKey].wasPressedThisFrame)
            TogglePanel();
    }

    private void WarnIfMissingReferences()
    {
        if (settingsPanel == null) Debug.LogWarning("[SettingsController] settingsPanel 미할당.", this);
        if (panelRootCanvas == null) Debug.LogWarning("[SettingsController] panelRootCanvas 미할당 — 패널이 머리 정면에 배치되지 않음.", this);
        if (headTransform == null) Debug.LogWarning("[SettingsController] headTransform 미할당 — 패널이 머리 정면에 배치되지 않음.", this);
        if (volumeSlider == null) Debug.LogWarning("[SettingsController] volumeSlider 미할당.", this);
        if (volumeValueText == null) Debug.LogWarning("[SettingsController] volumeValueText 미할당.", this);
        if (audioManager == null) Debug.LogWarning("[SettingsController] audioManager 미할당 — 볼륨 변경이 적용되지 않음.", this);
        if (languageDropdown == null) Debug.LogWarning("[SettingsController] languageDropdown 미할당.", this);
        if (quitButton == null) Debug.LogWarning("[SettingsController] quitButton 미할당.", this);
        if (resetButton == null) Debug.LogWarning("[SettingsController] resetButton 미할당 — Footer/ResetButton을 연결해야 '기본값으로'가 동작함.", this);
        if (saveButton == null) Debug.LogWarning("[SettingsController] saveButton 미할당 — Footer/SaveButton을 연결해야 '저장'이 동작함.", this);
    }

    private void CacheOriginalTransform()
    {
        if (panelRootCanvas == null || _hasOriginalTransform) return;
        _originalParent = panelRootCanvas.parent;
        _originalLocalPosition = panelRootCanvas.localPosition;
        _originalLocalRotation = panelRootCanvas.localRotation;
        _hasOriginalTransform = true;
    }

    private void OnQuitClicked()
    {
        ClosePanel();
    }

    private void InitializeVolume()
    {
        float saved = PlayerPrefs.GetFloat(VolumePrefKey, DefaultVolume);

        if (audioManager != null)
            audioManager.SetVolume(saved);

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(saved);

        UpdateVolumeText(saved);
    }

    private void OnVolumeChanged(float value)
    {
        value = Mathf.Clamp01(value);

        if (audioManager != null)
            audioManager.SetVolume(value);

        UpdateVolumeText(value);

        PlayerPrefs.SetFloat(VolumePrefKey, value);
        PlayerPrefs.Save();
    }

    private void UpdateVolumeText(float value01)
    {
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(value01 * 100f) + "%";
    }

    //진동 세기는 아직 소비하는 쪽이 없다. 값 저장 + 표시까지만 담당한다.
    //ㄴ 안 그러면 설정 패널의 "진동 %" 가 영원히 0% 로 남는다 (슬라이더만 있고 리스너가 없었음)
    private void InitializeVibrate()
    {
        float saved = PlayerPrefs.GetFloat(VibratePrefKey, DefaultVibrate);

        if (vibrateSlider != null)
            vibrateSlider.SetValueWithoutNotify(saved);

        UpdateVibrateText(saved);
    }

    private void OnVibrateChanged(float value)
    {
        value = Mathf.Clamp01(value);

        UpdateVibrateText(value);

        PlayerPrefs.SetFloat(VibratePrefKey, value);
        PlayerPrefs.Save();
    }

    private void UpdateVibrateText(float value01)
    {
        if (vibrateValueText != null)
            vibrateValueText.text = Mathf.RoundToInt(value01 * 100f) + "%";
    }

    /// <summary>
    /// 볼륨/진동/언어를 기본값으로. 슬라이더는 Notify 를 태워서 오디오까지 즉시 반영시킨다.
    /// </summary>
    private void OnResetClicked()
    {
        if (volumeSlider != null) volumeSlider.value = DefaultVolume;
        else OnVolumeChanged(DefaultVolume);

        if (vibrateSlider != null) vibrateSlider.value = DefaultVibrate;
        else OnVibrateChanged(DefaultVibrate);

        if (LocalizationSettings.AvailableLocales != null)
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(DefaultLocaleCode);
            if (locale != null)
            {
                SetLocale(locale);

                var locales = LocalizationSettings.AvailableLocales.Locales;
                if (languageDropdown != null && locales != null)
                {
                    var index = locales.IndexOf(locale);
                    if (index >= 0) languageDropdown.SetValueWithoutNotify(index);
                }
            }
        }
    }

    /// <summary>
    /// 각 설정은 바뀔 때마다 이미 저장된다. 이 버튼은 확정 + 패널 닫기 용도.
    /// </summary>
    private void OnSaveClicked()
    {
        PlayerPrefs.Save();
        ClosePanel();
    }

    private void OpenPanel()
    {
        if (settingsPanel == null) return;

        if (panelRootCanvas != null && headTransform != null)
        {
            panelRootCanvas.SetParent(null, false);

            Vector3 forward = headTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f)
                forward = Vector3.forward;
            forward.Normalize();

            panelRootCanvas.position = headTransform.position + forward * spawnDistance + Vector3.up * verticalOffset;
            panelRootCanvas.rotation = Quaternion.LookRotation(forward);
        }

        settingsPanel.SetActive(true);
    }

    private void ClosePanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (panelRootCanvas != null && _hasOriginalTransform)
        {
            panelRootCanvas.SetParent(_originalParent, false);
            panelRootCanvas.localPosition = _originalLocalPosition;
            panelRootCanvas.localRotation = _originalLocalRotation;
        }
    }

    private void TogglePanel(InputAction.CallbackContext context) => TogglePanel();

    private void TogglePanel()
    {
        if (settingsPanel == null) return;
        if (settingsPanel.activeSelf) ClosePanel();
        else OpenPanel();
    }

    private void PopulateLanguageDropdown()
    {
        if (languageDropdown == null) return;
        if (LocalizationSettings.AvailableLocales == null) return;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales == null || locales.Count == 0) return;

        var labels = new List<string>(locales.Count);
        foreach (var locale in locales)
            labels.Add(locale.LocaleName);

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(labels);

        var current = LocalizationSettings.SelectedLocale;
        if (current == null) return;

        var index = locales.IndexOf(current);
        if (index >= 0)
            languageDropdown.SetValueWithoutNotify(index);
    }

    private void OnLanguageChanged(int index)
    {
        if (LocalizationSettings.AvailableLocales == null) return;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales == null || index < 0 || index >= locales.Count) return;
        SetLocale(locales[index]);
    }

    private void SetLocale(Locale locale)
    {
        if (locale == null) return;
        LocalizationSettings.SelectedLocale = locale;
        PlayerPrefs.SetString(LanguagePrefKey, locale.Identifier.Code);
        PlayerPrefs.Save();
    }

    private void ApplySavedLanguage()
    {
        if (LocalizationSettings.AvailableLocales == null) return;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales == null || locales.Count == 0) return;

        var code = PlayerPrefs.GetString(LanguagePrefKey, DefaultLocaleCode);
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale == null)
            locale = locales[0];

        LocalizationSettings.SelectedLocale = locale;
    }
}
