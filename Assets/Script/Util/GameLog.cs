using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug; // System.Diagnostics.Debug와 이름 충돌 방지

/// <summary>
/// 카테고리별 토글 가능한 디버그 로거.
///
/// - 에디터에서 보고 싶은 카테고리만 bool을 true로 둔다 (런타임 토글).
/// - 모든 메서드에 [Conditional("GAME_LOG")]가 붙어 있어,
///   Scripting Define Symbols에 GAME_LOG이 없으면 호출부가 컴파일에서 통째로 제거된다 (릴리즈 비용 0).
///   ㄴ 켜는 곳: Edit > Project Settings > Player > Other Settings > Scripting Define Symbols 에 GAME_LOG 추가
///
/// 사용 예: GameLog.Run($"OutRunner 진입 base_index={base_index}");
/// </summary>
public static class GameLog
{
    //카테고리 스위치 - 보고 싶은 것만 true
    private static bool General  = true;
    private static bool Ball     = false;
    private static bool Runner   = false;
    private static bool Hit   = false;
    private static bool Pitcher  = false;
    private static bool Defender = true;
    private static bool Return   = true; //공/주자 복귀 알고리즘 추적용

    [Conditional("GAME_LOG")]
    public static void Log(string msg, Object context = null)
    {
        if (General) Debug.Log($"<color=white>[Game]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void BallLog(string msg, Object context = null)
    {
        if (Ball) Debug.Log($"<color=cyan>[Ball]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void RunnerLog(string msg, Object context = null)
    {
        if (Runner) Debug.Log($"<color=lime>[Runner]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void HitLog(string msg, Object context = null)
    {
        if (Hit) Debug.Log($"<color=yellow>[Batter]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void Pitch(string msg, Object context = null)
    {
        if (Pitcher) Debug.Log($"<color=orange>[Pitcher]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void Defend(string msg, Object context = null)
    {
        if (Defender) Debug.Log($"<color=#9aa0ff>[Defender]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void Back(string msg, Object context = null)
    {
        if (Return) Debug.Log($"<color=#ff7fbf>[복귀]</color> {msg}", context);
    }

    //경고/에러는 GameLog에 넣지 않는다.
    //ㄴ [Conditional("GAME_LOG")] 때문에 릴리즈에서 같이 제거되면 안 되므로, 그대로 Debug.LogWarning / Debug.LogError 사용.
}
