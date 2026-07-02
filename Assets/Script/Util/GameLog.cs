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
    private static bool Ball     = true;
    private static bool Runner   = true;
    private static bool Hit   = true;
    private static bool Pitcher  = true;
    private static bool Defender = true;
    private static bool Return   = true; //공/주자 복귀 알고리즘 추적용

    //리플레이 녹화가 구독하는 훅. 콘솔에 찍히는 로그와 "같은 줄"을 여기로도 흘려보내
    //ReplayRecorder가 시간축에 쌓게 한다(=재생 씬 패널에 로그가 뜨는 근원).
    //ㄴ 실제 통지는 [Conditional("GAME_LOG")] 메서드 안에서만 일어나므로, GAME_LOG 없으면 훅도 안 울린다.
    public static event System.Action<string> OnLog;

    [Conditional("GAME_LOG")]
    public static void Log(string msg, Object context = null)
    {
        if (General) Emit($"<color=white>[Game]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void BallLog(string msg, Object context = null)
    {
        if (Ball) Emit($"<color=cyan>[Ball]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void RunnerLog(string msg, Object context = null)
    {
        if (Runner) Emit($"<color=lime>[Runner]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void HitLog(string msg, Object context = null)
    {
        if (Hit) Emit($"<color=yellow>[Batter]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void Pitch(string msg, Object context = null)
    {
        if (Pitcher) Emit($"<color=orange>[Pitcher]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void Defend(string msg, Object context = null)
    {
        if (Defender) Emit($"<color=#9aa0ff>[Defender]</color> {msg}", context);
    }

    [Conditional("GAME_LOG")]
    public static void Back(string msg, Object context = null)
    {
        if (Return) Emit($"<color=#ff7fbf>[복귀]</color> {msg}", context);
    }

    //콘솔 출력 + 리플레이 훅 통지를 한 곳에 모은다. 각 카테고리 메서드가 통과시킨 줄만 여기로 온다.
    private static void Emit(string line, Object context)
    {
        Debug.Log(line, context);
        OnLog?.Invoke(line);
    }

    //경고/에러는 GameLog에 넣지 않는다.
    //ㄴ [Conditional("GAME_LOG")] 때문에 릴리즈에서 같이 제거되면 안 되므로, 그대로 Debug.LogWarning / Debug.LogError 사용.
}
