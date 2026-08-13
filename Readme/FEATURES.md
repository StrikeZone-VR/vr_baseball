이 문서는 게임플레이 위주로 정리했다.

# 목차
1. [UI](#UI)
2. [투수](#투수)



---
# UI
<img width="1920" height="936" alt="StrikeZone VR Test1 0-8 screenshot" src="https://github.com/user-attachments/assets/eba5864b-f2ac-4c5c-96d5-7124865ba189" /><br>
<img width="508" height="410" alt="image" src="https://github.com/user-attachments/assets/2838e9fb-5d7f-4bd6-acc2-d0455433a833" /><br>
<img width="296" height="242" alt="image" src="https://github.com/user-attachments/assets/d56015f9-bd6b-4a08-a99d-14007d6a09ba" /><br>

- [ ] todo : settingPanel, GameResult, MainMenu 씬의 UI를 개선하자.

메뉴에는 각자 게임 씬으로 들어갈 수 있는 버튼과 오른쪽에는 KBO 실시간 정보를 보여주는 UI 창을 띄웠다. <br>
전체적으로 UI 분위기는 귀여운 로우파이느낌을 살릴 수 있게 둥글게 구현했다.

---
# 투수
<img width="506" height="438" alt="image" src="https://github.com/user-attachments/assets/8887898f-9d5b-4ebe-b9fb-cc774a2e854d" /><br>
기본적으로 AI투수가 던지는 투구, 플레이어가 직접 던지는 투구가 있다.

## 플레이어 투구
### 기본 투구
초반 플레이어가 던지는 난이도를 줄이기 위해 가중치라는 시스템이 존재한다.<br>
![bandicam 2026-01-05 00-29-14-325 (1)](https://github.com/user-attachments/assets/ba506b70-37ba-4156-a176-8fa42a2e1112)<br>
기본 투구<br><br>

![bandicam 2026-01-05 00-29-14-325](https://github.com/user-attachments/assets/47c6d653-cee9-46fc-843a-c2b8506b25cf)<br>
- 가중치 추가한 버전<br><br>

- [ ] todo : 플레이어가 던지는 슬라이더, 커브, 포크볼도 gif 만들어주자.


## AI 투수
![bandicam 2025-12-24 22-04-35-245](https://github.com/user-attachments/assets/119d3df8-a414-411b-9f38-a48cb139c3d4)
- 직구
시작 위치, 도착 위치, 투수 구속을 매개변수로 넣으면 시작 공 속력을 반환한다. <br>

### 커브
![bandicam 2025-12-24 22-11-19-358](https://github.com/user-attachments/assets/36571b80-12b0-46dc-89c9-7caa7fe25e4c)
- 커브
기존의 직구에서 마그누스 효과를 적용했다. 속력이 클수록 마그누스 힘을 증가시켜 실제 커브처럼 날카롭게 떨어지는 움직임을 구현했다.

### 슬라이더
<img width="400" height="184" alt="bandicam 2026-05-25 22-53-03-233" src="https://github.com/user-attachments/assets/69a46cbc-35ad-4690-b767-6673622ab612" /><br>
x값에 가중치를 둬서 옆으로 휘어지게 만들었다.

```
//직구, 커브, 슬라이더 통합 계산 코드
public Vector3 CalculateVelocity(Vector3 start, Vector3 target, float speed, Vector3 pitchForce)
{
    speed /= 3.6f;
    float g = Mathf.Abs(Physics.gravity.y);
    Vector3 diff = target - start;
    float t = new Vector2(diff.x, diff.z).magnitude / speed;

    // 힘이 속도에 의존하므로 근사 → 재계산 2단계로 구한다
    float vSqApprox = speed * speed;
    Vector3 devApprox = -0.5f * vSqApprox * t * pitchForce;
    float vSq = vSqApprox + (devApprox.x * devApprox.x + devApprox.z * devApprox.z) / 3f;
    Vector3 accel = vSq * pitchForce;

    Vector3 velocity = new Vector3(diff.x, 0, diff.z).normalized * speed;
    velocity.x -= 0.5f * accel.x * t;
    velocity.z -= 0.5f * accel.z * t;
    velocity.y = (diff.y + 0.5f * (g - accel.y) * t * t) / t;
    return velocity;
}
```

---
# 타자
![bandicam 2025-11-14 14-04-26-322](https://github.com/user-attachments/assets/c843d020-e9be-4904-9b7b-d8df1b67220b)
- 타격

<img width="905" height="398" alt="image" src="https://github.com/user-attachments/assets/9875f4a7-16ac-4562-8afb-0a47ed262a90" /><br>
- 구속 설정과 타격 지표 스텟 확인할 수 있는 UI

## AI 타자
![debughitting](https://github.com/user-attachments/assets/71151cb2-ec3a-46d3-b553-86b3932204df) <br>
<img width="1177" height="575" alt="image" src="https://github.com/user-attachments/assets/372a2084-9629-4876-812c-414d1ed7bdd2" /><br>
스윙은 배트를 축(`axis`) 기준 원형 궤도 위에서 각도(`batAngle`)를 프레임마다 갱신하며 움직인다. <br>
위치와 회전을 같은 각도값으로 함께 계산해야 배트가 궤도를 미끄러지지 않고 자연스럽게 따라간다.<br>

### 코드
```
IEnumerator Swing()
{
    Vector3 orbitYAxis = axis.transform.up;
    Quaternion zTilt = Quaternion.AngleAxis(axis.localEulerAngles.z - 90f, axis.transform.forward);

    void ApplyPose(float progress)
    {
        float batAngle = startBatAngle + totalOrbitAngle * progress;
        float rad = batAngle * Mathf.Deg2Rad;

        transform.position = axis.transform.position
            + xWorld * (Mathf.Cos(rad) * AXIS_DISTANCE)
            + zWorld * (-Mathf.Sin(rad) * AXIS_DISTANCE);
        transform.localRotation = Quaternion.AngleAxis(batAngle, orbitYAxis) * zTilt;
    }

    ApplyPose(0f);
    while (elapsed < ROTATION_TIME)
    {
        elapsed += Time.deltaTime;
        ApplyPose(elapsed / ROTATION_TIME);
        yield return null;
    }

    isSwing = false;
    elapsed = 0;
    ApplyPose(1f);
}
```



---

# 디버깅
### 야구공 궤적 표시
<img width="803" height="253" alt="image" src="https://github.com/user-attachments/assets/b5eb6f85-514b-4eae-a53e-46f592b799ac" /><br>
공의 궤적을 체크하기 위해 만든 기능이다.<br>
예측 궤적을 점선으로 그려 스트라이크 존 통과 여부와 최종 낙구 지점을 눈으로 확인한다. <br>
공의 궤적을 계산해서 노란색 실선으로 그려주는 역할을 한다.<br><br>





### 주자
<img width="291" height="283" alt="image" src="https://github.com/user-attachments/assets/4969c1cf-6cb4-4d9d-9453-e71f1704e6af" /><br>
- 주자 현황을 보여주는 디버깅 UI

### 리플레이
<img width="400" height="185" alt="bandicam 2026-06-30 20-30-16-705" src="https://github.com/user-attachments/assets/2d5425a5-13d1-4162-8375-8b8183a4a38a" /><br>
- [ ] todo 리플레이 테스트하는 영상? 링크

---
# 경기
## 수비
![defense](https://github.com/user-attachments/assets/93d01c32-d9cb-48ec-a5c7-99ef37b08529)<br>
공의 궤적을 보고 자연스럽게 수비수가 따라간다.<br>

### 주자

<img width="441" height="414" alt="image" src="https://github.com/user-attachments/assets/0941e6a1-bc85-419c-bb0a-08aaea00674d" /><br>

*`BaseIndex`로 각 주자의 현재 위치를 관리한다.*

<img width="1083" height="397" alt="image" src="https://github.com/user-attachments/assets/26dba83f-9f7a-4b47-8fd2-017a2f13dd4d" /><br>

*수비와 주자에게 서로 다른 유니폼을 입혀 시야에서 바로 구분되도록 했다.*

타격이 성공하면 주자가 진루하고, 이미 베이스에 있던 주자도 함께 다음 베이스로 뛴다.

### 파울·플라잉 아웃 — 상태 롤백

타격 시점에 주자는 이미 뛰기 시작하고 득점까지 반영된다. 그런데 이후 파울이나 뜬공 아웃이 확정되면 **전부 타격 직전 상태로 되돌려야 한다.** 판정을 기다렸다가 움직이면 반응이 굼떠 보이므로, 먼저 움직이고 나중에 되돌리는 방식을 택했다.

방법은 타격 직전 점수·주자 위치를 스냅샷으로 저장해뒀다가, 판정에 따라 다르게 복원하는 것이다.

| 판정 | 복원 방식 |
|---|---|
| 파울 | 위치를 즉시 원위치로 되돌린다 (`IsMove = false`) |
| 뜬공 아웃 | 한 베이스 뒤로 실제로 뛰어서 돌아간다 (`IsMove = true`) |
| 득점 후 파울 | 늘어난 주자 수만큼 다시 채워 넣는다 |

<details>
<summary>코드 보기 — 저장 및 롤백</summary>

```csharp
// 타격 직전 스냅샷
public void SaveBeforeStatus()
{
    before_score = _teamStatus[GetTeamIndex()].Score;
    before_runners.Clear();
    foreach (var r in runners) before_runners.Add(r.BaseIndex);
}

// 파울 — 즉시 원위치
public void FoulRollbackBeforeStatus()
{
    _teamStatus[GetTeamIndex()].Score = before_score;
    for (int i = 0; i < before_runners.Count; i++)
    {
        runners[i].SetBaseIndexPosition(before_runners[i]);
        runners[i].IsMove = false;
    }
}

// 뜬공 아웃 — 한 베이스 뒤로 귀루
public void FlyingOutRollbackBeforeStatus()
{
    _teamStatus[GetTeamIndex()].Score = before_score;
    for (int i = 0; i < before_runners.Count; i++)
    {
        runners[i].BaseIndex = before_runners[i] - 1;
        runners[i].IsMove = true;
    }
}
```
</details>

> 득점 후 파울처럼 이미 반영된 점수를 취소할 때는, 줄어든 점수 수만큼 주자를 다시 채워 넣어 인원을 맞춘다.
