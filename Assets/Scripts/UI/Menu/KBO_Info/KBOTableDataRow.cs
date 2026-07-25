using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// KBO 정보 테이블의 데이터 행 컴포넌트
/// </summary>
public class KBOTableDataRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] dataCells;
    [SerializeField] private Image backgroundImage;

    public void SetData(string[] data)
    {
        if (dataCells == null)
            dataCells = GetComponentsInChildren<TextMeshProUGUI>();

        for (int i = 0; i < data.Length && i < dataCells.Length; i++)
        {
            if (dataCells[i] != null)
                dataCells[i].text = data[i];
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
        if (dataCells == null || dataCells.Length == 0)
            dataCells = GetComponentsInChildren<TextMeshProUGUI>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }
}