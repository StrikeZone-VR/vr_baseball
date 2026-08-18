/// <summary>
/// 가중치 기반 추첨.
/// 합이 100이 아니어도 총합으로 나누기 때문에 그대로 쓸 수 있다.
/// ㄴ 그래서 인스펙터에서 100을 억지로 맞추지 않아도 확률이 안 깨진다.
/// </summary>
public static class WeightedRandom
{
    /// <summary>
    /// count개 중 가중치에 비례해서 인덱스 하나를 뽑는다.
    /// 가중치가 전부 0이거나 음수면(= 데이터 미입력) 균등 추첨으로 폴백한다.
    /// </summary>
    /// <param name="count">후보 개수</param>
    /// <param name="weightOf">i번째 후보의 가중치를 돌려주는 함수</param>
    /// <returns>뽑힌 인덱스. count가 0 이하면 -1</returns>
    public static int Pick(int count, System.Func<int, float> weightOf)
    {
        if (count <= 0 || weightOf == null)
        {
            return -1;
        }

        float total = 0f;
        for (int i = 0; i < count; i++)
        {
            float w = weightOf(i);
            if (w > 0f) total += w;
        }

        //전부 0이면 확률을 만들 수 없다 => 균등하게 뽑아서 "아무것도 안 던지는" 상황을 막는다
        if (total <= 0f)
        {
            return UnityEngine.Random.Range(0, count);
        }

        float r = UnityEngine.Random.value * total;
        for (int i = 0; i < count; i++)
        {
            float w = weightOf(i);
            if (w <= 0f) continue;

            r -= w;
            if (r <= 0f) return i;
        }

        //부동소수 오차로 여기까지 오는 경우가 드물게 있다
        return count - 1;
    }
}
