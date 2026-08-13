이 문서는 게임플레이 위주로 정리했다.

# 목차
1. [UI](#UI)
2. [투수](#투수)
3. [타자](#타자)
4. [경기](#경기)
5. [디버깅](#디버깅)
6. [개발 예정](#개발-예정)

---

# UI

<img width="500" alt="메인 메뉴 화면" src="https://github.com/user-attachments/assets/eba5864b-f2ac-4c5c-96d5-7124865ba189" /><br>

*메뉴에는 각 게임 씬으로 들어가는 버튼과, 오른쪽에 KBO 실시간 정보를 보여주는 UI 창을 띄웠다.*

<img width="300" alt="메뉴 버튼 확대" src="https://github.com/user-attachments/assets/2838e9fb-5d7f-4bd6-acc2-d0455433a833" /><br>
<img width="300" alt="KBO 정보 패널 확대" src="https://github.com/user-attachments/assets/d56015f9-bd6b-4a08-a99d-14007d6a09ba" /><br>

전체적인 UI 분위기는 귀여운 로우파이 느낌을 살릴 수 있게 둥글게 구현했다.

---

# 투수

<img width="400" alt="투수 개요" src="https://github.com/user-attachments/assets/8887898f-9d5b-4ebe-b9fb-cc774a2e854d" /><br>

기본적으로 AI 투수가 던지는 투구와 플레이어가 직접 던지는 투구가 있다.

## 플레이어 투구

초반 플레이어가 던지는 난이도를 줄이기 위해 가중치라는 시스템이 존재한다.

<img width="400" alt="기본 투구" src="https://github.com/user-attachments/assets/ba506b70-37ba-4156-a176-8fa42a2e1112" /><br>

*기본 투구.*

<img width="400" alt="가중치 적용 투구" src="https://github.com/user-attachments/assets/47c6d653-cee9-46fc-843a-c2b8506b25cf" /><br>

*가중치를 추가한 버전. 같은 구속에서도 코스가 미세하게 흔들려 실제 투구에 가까워진다.*

## AI 투수

<img width="400" alt="직구" src="https://github.com/user-attachments/assets/119d3df8-a414-411b-9f38-a48cb139c3d4" /><br>

시작 위치, 도착 위치, 투수 구속을 매개변수로 넣으면 시작 공 속력을 반환한다.

### 커브

<img width="400" alt="커브" src="https://github.com/user-attachments/assets/36571b80-12b0-46dc-89c9-7caa7fe25e4c" /><br>

기존의 직구에서 마그누스 효과를 적용했다. 속력이 클수록 마그누스 힘을 증가시켜 실제 커브처럼 날카롭게 떨어지는 움직임을 구현했다.

### 슬라이더

<img width="400" alt="슬라이더" src="https://github.com/user-attachments/assets/69a46cbc-35ad-4690-b767-6673622ab612" /><br>

x값에 가중치를 둬서 옆으로 휘어지게 만들었다.

### 구종 통합 계산

직구·커브·슬라이더는 모두 같은 함수를 쓴다. 구종마다 다른 건 힘 벡터(`pitchForce`) 하나뿐이고, 이 벡터의 방향에 따라 궤적이 갈라진다.

| 구종 | 힘 벡터 | 결과 |
|---|---|---|
| 직구 | (0, 0, 0) | 힘 없음, 순수 포물선 |
| 커브 | y축 성분 | 더 크게 떨어짐 |
| 슬라이더 | x축 성분 | 옆으로 휘어짐 |

힘이 최종 속도에 의존하는 순환 구조라, 대략적인 속도로 편차를 먼저 추정한 뒤 그 값으로 힘을 다시 계산하는 2단계 근사를 쓴다.

<details>
<summary>코드 보기 — CalculateVelocity</summary>

```csharp
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
</details>

---

# 타자

<img width="400" alt="타격 장면" src="https://github.com/user-attachments/assets/c843d020-e9be-4904-9b7b-d8df1b67220b" /><br>

*타격 장면.*

<img width="500" alt="구속 설정 및 타격 지표 UI" src="https://github.com/user-attachments/assets/9875f4a7-16ac-4562-8afb-0a47ed262a90" /><br>

*구속을 설정하고 타격 지표 스탯을 확인할 수 있는 UI다.*

## AI 타자

<img width="400" alt="AI 타자 스윙" src="https://github.com/user-attachments/assets/71151cb2-ec3a-46d3-b553-86b3932204df" /><br>
<img width="500" alt="스윙 궤도 상세" src="https://github.com/user-attachments/assets/372a2084-9629-4876-812c-414d1ed7bdd2" /><br>

스윙은 배트를 축(`axis`) 기준 원형 궤도 위에서 각도(`batAngle`)를 프레임마다 갱신하며 움직인다. 위치와 회전을 같은 각도값으로 함께 계산해야 배트가 궤도를 미끄러지지 않고 자연스럽게 따라간다.

<details>
<summary>코드 보기 — Swing()</summary>

```csharp
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
</details>

---

# 경기

## 수비

<img width="400" alt="수비 동작" src="https://github.com/user-attachments/assets/93d01c32-d9cb-48ec-a5c7-99ef37b08529" /><br>

*공의 궤적을 보고 자연스럽게 수비수가 따라간다.*

## 주자

<img width="300" alt="주자 base_index 구조" src="https://github.com/user-attachments/assets/0941e6a1-bc85-419c-bb0a-08aaea00674d" /><br>

*`BaseIndex`로 각 주자의 현재 위치를 관리한다.*

<img width="500" alt="주자 유니폼" src="https://github.com/user-attachments/assets/26dba83f-9f7a-4b47-8fd2-017a2f13dd4d" /><br>

*수비와 주자에게 서로 다른 유니폼을 입혀 시야에서 바로 구분되도록 했다.*

타격이 성공하면 주자가 진루하고, 이미 베이스에 있던 주자도 함께 다음 베이스로 뛴다.

## 파울·플라잉 아웃 — 상태 롤백

타격 시점에 주자는 이미 뛰기 시작하고 득점까지 반영된다. 그런데 이후 파울이나 뜬공 아웃이 확정되면 전부 타격 직전 상태로 되돌려야 한다. 판정을 기다렸다가 움직이면 반응이 굼떠 보이므로, 먼저 움직이고 나중에 되돌리는 방식을 택했다.

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

---

# 디버깅

## 궤적 시각화

<img width="500" alt="공 궤적 표시" src="https://github.com/user-attachments/assets/b5eb6f85-514b-4eae-a53e-46f592b799ac" /><br>

*예측 궤적을 점선으로 그려 스트라이크 존 통과 여부와 최종 낙구 지점을 눈으로 확인한다.*

공의 궤적을 체크하기 위해 만든 기능이다. 이 계산은 시각화뿐 아니라 타자의 배트 위치 결정과 수비수 이동 목표 산출에도 그대로 재사용된다.

## 주자 현황판

<img width="300" alt="주자 현황 디버깅 UI" src="https://github.com/user-attachments/assets/4969c1cf-6cb4-4d9d-9453-e71f1704e6af" /><br>

*주자 현황을 보여주는 디버깅 UI.*

---

# 개발 예정

- [ ] SettingPanel, GameResult, MainMenu 씬의 UI 개선
- [ ] 플레이어가 던지는 슬라이더·커브·포크볼 gif 추가

### 리플레이

<img width="400" alt="리플레이 (진행 중)" src="https://github.com/user-attachments/assets/2d5425a5-13d1-4162-8375-8b8183a4a38a" /><br>

- [ ] 리플레이 테스트 영상 링크 추가
