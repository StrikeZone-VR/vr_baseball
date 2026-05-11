using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

public class SettingsController : MonoBehaviour
{
    private const string LanguagePrefKey = "settings.language";
    private const string DefaultLocaleCode = "en";

    [Header("UI 패널")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button quitButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider vibrateSlider;
    [SerializeField] private DropdownField languageDropdown;


    //메뉴로 되돌아가기
    //- [ ] 소리
    //- [ ] 언어
    ///- [ ] 컨트롤러 진동 세기
    [Header("인풋 액션 연결")]

    //Input Action Asset의 하위 버전 : XRI : UI / Click 이런거
    [SerializeField] private InputActionReference toggleMenuAction;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        ApplySavedLanguage();
        PopulateLanguageDropdown();
    }

    private void OnEnable()
    {
        toggleMenuAction.action.Enable();
        toggleMenuAction.action.started += TogglePanel;

        quitButton.clicked += OnQuitClicked;

        if (languageDropdown != null)
            languageDropdown.RegisterValueChangedCallback(OnLanguageChanged);
    }

    private void OnDisable()
    {
        toggleMenuAction.action.started -= TogglePanel;
        toggleMenuAction.action.Disable();

        quitButton.clicked -= OnQuitClicked;

        if (languageDropdown != null)
            languageDropdown.UnregisterValueChangedCallback(OnLanguageChanged);
    }

    private void OnQuitClicked()
    {
        settingsPanel.SetActive(false);
    }

    private void PopulateLanguageDropdown()
    {
        if (languageDropdown == null) return;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        var labels = new List<string>(locales.Count);
        foreach (var locale in locales)
            labels.Add(locale.LocaleName);

        languageDropdown.choices = labels;

        var current = LocalizationSettings.SelectedLocale;
        if (current != null)
            languageDropdown.SetValueWithoutNotify(current.LocaleName);
    }

    private void OnLanguageChanged(ChangeEvent<string> evt)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        foreach (var locale in locales)
        {
            if (locale.LocaleName == evt.newValue)
            {
                SetLocale(locale);
                return;
            }
        }
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
        var code = PlayerPrefs.GetString(LanguagePrefKey, DefaultLocaleCode);
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale != null)
            LocalizationSettings.SelectedLocale = locale;
    }

    private void TogglePanel(InputAction.CallbackContext context)
    {
        // 패널 껐다 켜기
        settingsPanel.SetActive(!settingsPanel.activeSelf);

        if (settingsPanel.activeSelf)
        {
            Debug.Log("환경설정 창 켜짐!");
        }
    }
}
