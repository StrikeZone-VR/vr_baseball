using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// KBO 팀 정보 및 색상 관리 유틸리티 클래스
/// </summary>
public static class KBOTeamHelper
{
    /// <summary>
    /// KBO 팀별 대표 색상 정보
    /// </summary>
    public static readonly Dictionary<string, Color> TeamColors = new Dictionary<string, Color>
    {
        // KBO 리그 팀들의 대표 색상
        {"LG", new Color(0.76f, 0.12f, 0.34f)},        // LG 트윈스 - 마젠타
        {"두산", new Color(0.0f, 0.0f, 0.0f)},          // 두산 베어스 - 검은색  
        {"한화", new Color(1.0f, 0.5f, 0.0f)},          // 한화 이글스 - 주황색
        {"SSG", new Color(0.8f, 0.0f, 0.0f)},          // SSG 랜더스 - 빨간색
        {"롯데", new Color(0.0f, 0.3f, 0.6f)},          // 롯데 자이언츠 - 파란색
        {"삼성", new Color(0.0f, 0.4f, 0.8f)},          // 삼성 라이온즈 - 블루
        {"기아", new Color(0.8f, 0.0f, 0.0f)},          // 기아 타이거즈 - 빨간색
        {"NC", new Color(0.0f, 0.5f, 0.8f)},           // NC 다이노스 - 네이비
        {"KT", new Color(0.0f, 0.0f, 0.0f)},           // KT 위즈 - 검은색
        {"키움", new Color(0.6f, 0.0f, 0.4f)}           // 키움 히어로즈 - 보라색
    };

    /// <summary>
    /// KBO 팀별 전체 이름 매핑
    /// </summary>
    public static readonly Dictionary<string, string> TeamFullNames = new Dictionary<string, string>
    {
        {"LG", "LG 트윈스"},
        {"두산", "두산 베어스"},
        {"한화", "한화 이글스"},
        {"SSG", "SSG 랜더스"},
        {"롯데", "롯데 자이언츠"},
        {"삼성", "삼성 라이온즈"},
        {"기아", "기아 타이거즈"},
        {"NC", "NC 다이노스"},
        {"KT", "KT 위즈"},
        {"키움", "키움 히어로즈"}
    };

    /// <summary>
    /// 팀 이름으로 대표 색상 가져오기
    /// </summary>
    /// <param name="teamName">팀 이름</param>
    /// <returns>팀 대표 색상 (없으면 회색 반환)</returns>
    public static Color GetTeamColor(string teamName)
    {
        if (TeamColors.TryGetValue(teamName, out Color color))
            return color;

        return new Color(0.5f, 0.5f, 0.5f); // 기본 회색
    }

    /// <summary>
    /// 팀 이름으로 전체 이름 가져오기
    /// </summary>
    /// <param name="teamName">팀 이름</param>
    /// <returns>팀 전체 이름 (없으면 원래 이름 반환)</returns>
    public static string GetTeamFullName(string teamName)
    {
        if (TeamFullNames.TryGetValue(teamName, out string fullName))
            return fullName;

        return teamName; // 원래 이름 반환
    }

    /// <summary>
    /// 순위에 따른 색상 가져오기 (1위 금색, 2위 은색, 3위 동색 등)
    /// </summary>
    /// <param name="rank">순위</param>
    /// <returns>순위에 맞는 색상</returns>
    public static Color GetRankColor(int rank)
    {
        switch (rank)
        {
            case 1:
                return new Color(1.0f, 0.84f, 0.0f);     // 금색
            case 2:
                return new Color(0.75f, 0.75f, 0.75f);   // 은색
            case 3:
                return new Color(0.8f, 0.5f, 0.2f);      // 동색
            case 4:
            case 5:
                return new Color(0.2f, 0.7f, 0.2f);      // 플레이오프 - 초록
            default:
                return new Color(0.3f, 0.3f, 0.3f);      // 기타 - 회색
        }
    }

    /// <summary>
    /// 타율/ERA 등 스탯에 따른 색상 그라데이션
    /// </summary>
    /// <param name="value">스탯 값</param>
    /// <param name="minValue">최소값</param>
    /// <param name="maxValue">최대값</param>
    /// <param name="isHigherBetter">값이 클수록 좋은 스탯인지 (타율은 true, ERA는 false)</param>
    /// <returns>성능에 따른 그라데이션 색상</returns>
    public static Color GetPerformanceColor(float value, float minValue, float maxValue, bool isHigherBetter = true)
    {
        float normalizedValue = Mathf.Clamp01((value - minValue) / (maxValue - minValue));

        if (!isHigherBetter)
            normalizedValue = 1.0f - normalizedValue;

        // 빨간색(나쁨) → 노란색(보통) → 초록색(좋음) 그라데이션
        if (normalizedValue < 0.5f)
        {
            // 빨간색 → 노란색
            float t = normalizedValue * 2.0f;
            return Color.Lerp(Color.red, Color.yellow, t);
        }
        else
        {
            // 노란색 → 초록색
            float t = (normalizedValue - 0.5f) * 2.0f;
            return Color.Lerp(Color.yellow, Color.green, t);
        }
    }
}