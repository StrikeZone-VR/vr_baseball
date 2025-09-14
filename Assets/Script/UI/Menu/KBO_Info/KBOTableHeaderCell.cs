using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// KBO 정보 테이블의 헤더 셀 컴포넌트
/// </summary>
public class KBOTableHeaderCell : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Image backgroundImage;

    public void SetHeaderText(string text)
    {
        if (headerText != null)
        {
            headerText.text = text;
            headerText.fontStyle = FontStyles.Bold;
        }
    }

    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
            backgroundImage.color = color;
    }

    void Awake()
    {
        // 컴포넌트 자동 찾기
        if (headerText == null)
            headerText = GetComponentInChildren<TextMeshProUGUI>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }
}