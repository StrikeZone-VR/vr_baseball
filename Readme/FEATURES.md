# 게임 플레이
이 markdown 파일은 게임플레이 위주로 설명한다.

---
## 게임 메뉴
<img width="1920" height="936" alt="StrikeZone VR Test1 0-8 screenshot" src="https://github.com/user-attachments/assets/eba5864b-f2ac-4c5c-96d5-7124865ba189" /><br>
메뉴에는 각자 게임 씬으로 들어갈 수 있는 버튼과 오른쪽에는 KBO 실시간 정보를 보여주는 UI 창을 띄웠다. <br>

----

## 투수
<img width="1920" height="936" alt="StrikeZone VR Test1 1-4 screenshot" src="https://github.com/user-attachments/assets/0e1eb183-8819-484a-8124-4015f0de891a" /><br>

![bandicam 2026-01-05 00-29-14-325 (1)](https://github.com/user-attachments/assets/ba506b70-37ba-4156-a176-8fa42a2e1112)<br>
- 기본 투구 <br>

![bandicam 2026-01-05 00-29-14-325](https://github.com/user-attachments/assets/47c6d653-cee9-46fc-843a-c2b8506b25cf)<br>
- 가중치 추가한 버전<br>

### AI 투수
![bandicam 2025-12-24 22-04-35-245](https://github.com/user-attachments/assets/119d3df8-a414-411b-9f38-a48cb139c3d4)
- 직구
시작 위치, 도착 위치, 투수 구속을 매개변수로 넣으면 시작 공 속력을 반환한다. <br>


```
public Vector3 CalculateSimpleVelocity(Vector3 start, Vector3 target, float velocityXZ)
{
    velocityXZ /= 3.6f; //시속 평준화
    float g = Mathf.Abs(Physics.gravity.y); // 9.81 (양수)
    Vector3 dis = target - start;

    float mytime = dis.magnitude / velocityXZ;

    float velocityY = mytime / 2 * g;
    Vector3 velocityXZ_normal = dis.normalized;
    velocityXZ_normal *= velocityXZ;

    Vector3 result = velocityXZ_normal + new Vector3(0, velocityY, 0);
    return result;
}
```

### 커브
![bandicam 2025-12-24 22-11-19-358](https://github.com/user-attachments/assets/36571b80-12b0-46dc-89c9-7caa7fe25e4c)
- 커브
기존의 직구에서 마그누스 효과를 적용했다. 속력이 클수록 마그누스 힘을 증가시켜 실제 커브처럼 날카롭게 떨어지는 움직임을 구현했다.

### 슬라이더
<img width="400" height="184" alt="bandicam 2026-05-25 22-53-03-233" src="https://github.com/user-attachments/assets/69a46cbc-35ad-4690-b767-6673622ab612" /><br>
x값에 가중치를 둬서 옆으로 휘어지게 만들었다.

```
/// <summary>
/// 통합 계산 단위 => 직구, 슬라이더, 커브 통합
/// </summary>
/// <param name="start"></param>
/// <param name="target"></param>
/// <param name="velocity_xy">km/h단위</param>
/// <returns></returns>
public Vector3 CalculateVelocity(Vector3 start, Vector3 target
    , float velocity_xy, Vector3 piterTypeForce)
{
    velocity_xy /= 3.6f;
    float g = Mathf.Abs(Physics.gravity.y); // 9.81 (양수)
    //g -= piterTypeForce.y;

    Vector3 diff = target - start;
    Vector3 dirXZ = new Vector3(diff.x, 0, diff.z).normalized;
    float d = new Vector2(diff.x, diff.z).magnitude; // 수평 거리
    float h = diff.y; // 높이차

    // 비행 시간 계산: t = d / velocity_xy
    float t = d / velocity_xy;
    flightTime = t;

    float vSq = velocity_xy * velocity_xy; //제곱
    float aX_rough = vSq * piterTypeForce.x;
    float aZ_rough = vSq * piterTypeForce.z;
    float vxComp = -0.5f * aX_rough * t;
    float vzComp = -0.5f * aZ_rough * t;
    float vSqAdjusted = vSq + (vxComp * vxComp + vzComp * vzComp) / 3f;

    float aX = vSqAdjusted * piterTypeForce.x;
    float aY = vSqAdjusted * piterTypeForce.y; // 보통 음수 (아래로 휨)
    float aZ = vSqAdjusted * piterTypeForce.z;

    // y방향 초기 속도 Vy = (h + 0.5 * g * t^2) / t
    // 유효 중력 = g - aY (forceWeight.y < 0 이면 더 빨리 떨어짐)
    float effectiveG = g - aY;
    float vy = (h + 0.5f * effectiveG * t * t) / t;

    // 최종 속도 벡터
    Vector3 velocity = dirXZ * velocity_xy;
    // x/z 옆 휨 보정: 0.5*a*t² 만큼 휘므로 초기 방향을 반대로 살짝 틀어준다
    velocity.x -= 0.5f * aX * t;
    velocity.z -= 0.5f * aZ * t;
    velocity.y = vy;

    return velocity;
}
```
---

## 타자
![bandicam 2025-11-14 14-04-26-322](https://github.com/user-attachments/assets/c843d020-e9be-4904-9b7b-d8df1b67220b)
- 타격

<img width="905" height="398" alt="image" src="https://github.com/user-attachments/assets/9875f4a7-16ac-4562-8afb-0a47ed262a90" /><br>
- 구속 설정과 타격 지표 스텟 확인할 수 있는 UI

### AI 타자
![debughitting](https://github.com/user-attachments/assets/71151cb2-ec3a-46d3-b553-86b3932204df) <br>
<img width="1177" height="575" alt="image" src="https://github.com/user-attachments/assets/372a2084-9629-4876-812c-414d1ed7bdd2" /><br>
- 자동으로 스윙하는 함수<br>

```
IEnumerator Swing()
{
    Quaternion start_rotation;
    Quaternion current_rotation;
    Quaternion end_rotation;

    Vector3 start_pos;
    Vector3 current_pos;
    Vector3 end_pos;

    float prevCurve = 0f;

    Vector3 orbitYAxis = axis.transform.up; // axis의 로컬 Y축을 월드로 
    Vector3 orbitZAxis = axis.transform.forward;

    //zWorld * sin, xWorld * cos
    start_pos = axis.transform.position 
        + xWorld * (Mathf.Cos(startBatAngle * Mathf.Deg2Rad) * AXIS_DISTANCE)
        + zWorld * (-Mathf.Sin(startBatAngle * Mathf.Deg2Rad) * AXIS_DISTANCE);

    //zWorld * sin, xWorld * cos
    end_pos = axis.transform.position
        + xWorld * (Mathf.Cos((startBatAngle + totalOrbitAngle) * Mathf.Deg2Rad) * AXIS_DISTANCE)
        + zWorld * (-Mathf.Sin((startBatAngle + totalOrbitAngle) * Mathf.Deg2Rad) * AXIS_DISTANCE);

    start_rotation = Quaternion.AngleAxis(startBatAngle, orbitYAxis);
    end_rotation = Quaternion.AngleAxis(startBatAngle + totalOrbitAngle, orbitYAxis);

    //z축 기준으로 90도
    Quaternion zRotateQuaternion = Quaternion.AngleAxis(axis.localEulerAngles.z -90f, orbitZAxis);
    start_rotation *= zRotateQuaternion;
    end_rotation *= zRotateQuaternion; 

    transform.position = start_pos;
    transform.localRotation = start_rotation;  //rotation

    //프레임마다 회전한다.
    while (elapsed < ROTATION_TIME)
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / ROTATION_TIME;
        
        ////각도만 추가하자
        float batAngle = startBatAngle + totalOrbitAngle * progress;

        if (progress >= 0.5f && debugCheck)
        {
            debugCheck = false;
            Debug.Log("스윙 중간 : ");
            //Debug.Break();
        }
        
        //Debug.Log("hit time : (" +Time.time + ") : " + batAngle);
        // if (-190f <= batAngle && batAngle <= -170f)
        // {
        //     Debug.Log("real hit time : (" +Time.time + ")"+ batAngle + "도");
        // }
        
        //pos
        current_pos = axis.transform.position
            + xWorld * (Mathf.Cos(batAngle * Mathf.Deg2Rad) * AXIS_DISTANCE)
            + zWorld * (-Mathf.Sin(batAngle * Mathf.Deg2Rad) * AXIS_DISTANCE);
        
        //rotation
        current_rotation = Quaternion.AngleAxis(batAngle, orbitYAxis);
        current_rotation *= zRotateQuaternion; //기울어라 => 계산 순서는 -90 -45

        transform.position = current_pos;
        transform.localRotation = current_rotation;
        yield return null;
    }

    isSwing = false;
    elapsed = 0;
    transform.position = end_pos;
    transform.localRotation = end_rotation;
}
```

---

# 디버깅
### 야구공 궤적 표시
<img width="803" height="253" alt="image" src="https://github.com/user-attachments/assets/b5eb6f85-514b-4eae-a53e-46f592b799ac" /><br>
공의 궤적을 체크하기 위해 만든 기능이다.<br>
공의 궤적을 계산해서 노란색 실선으로 그려주는 역할을 한다.<br><br>

```
void CalTrajectory(bool isDebug = false)
{
    Vector3 predictedStrikePos;
    float dashLength = 0.1f; // 그려지는 짧은 선 길이
    float gapLength  = 0.1f; // 대시 사이 공백
    
    int steps = 160;
    float dt = 0.05f;
    
    if(isDebug)
        Gizmos.color = Color.yellow;

    Vector3 p = transform.position;
    
    Vector3 g = Physics.gravity;
    Vector3 v;
    if (_rigidbody == null)
    {
        v = new Vector3(1, 0, 1).normalized * _debugVelocity / 3.6f;
    }
    else
    {
        v = _rigidbody.velocity;
    }
    
    float stepLen = dashLength + Mathf.Max(0f, gapLength);

    for (int i = 0; i < steps; i++)
    {
        //중력 적용
        v += g * dt;
        Vector3 nextP = p + v * dt;

        // p -> nextP 구간을 대시로 쪼개서 그리기
        if(isDebug)
            DrawDashedSegment(p, nextP, dashLength, stepLen);

        // 수정된 완벽한 충돌 감지 로직
        if (Physics.Linecast(p, nextP, out var hit, -1, QueryTriggerInteraction.Collide))
        {
            // 1. 만약 부딪힌 게 Trigger(스트라이크 존 등)라면?
            if (hit.collider.isTrigger && (hit.collider.CompareTag("BallZone") || hit.collider.CompareTag("StrikeZone")))
            {
                // 존을 관통하는 위치만 쓱 기록하고 break는 하지 않음! (궤적 통과)
                if (!hasPassedStrikeZone) 
                {
                    predictedStrikePos = hit.point; // 관통한 정확한 좌표
                    hasPassedStrikeZone = true;
                    bat.MoveAxis(predictedStrikePos);
                }
            }
            // 2. 만약 부딪힌 게 진짜 물리적인 벽이나 땅이라면?
            else if(!hit.collider.isTrigger)
            {
                if(isDebug) 
                    Gizmos.DrawWireSphere(hit.point, 0.2f);
        
                _targetPosition = hit.point; // 최종 도착 지점 기록
                break; // 여기서 궤적 그리기 종료
            }
        }

        p = nextP;
    }
}

void DrawDashedSegment(Vector3 a, Vector3 b, float dashLen, float stepLen)
{
    Vector3 ab = b - a;
    float len = ab.magnitude;
    if (len < 0.00001f) return;

    Vector3 dir = ab / len;

    for (float t = 0f; t < len; t += stepLen)
    {
        float t0 = t;
        float t1 = Mathf.Min(t + dashLen, len);
        Gizmos.DrawLine(a + dir * t0, a + dir * t1);
    }
}
```

### 주자
<img width="291" height="283" alt="image" src="https://github.com/user-attachments/assets/4969c1cf-6cb4-4d9d-9453-e71f1704e6af" /><br>
- 주자 현황을 보여주는 디버깅 UI

---
# 경기
### 수비
![defense](https://github.com/user-attachments/assets/93d01c32-d9cb-48ec-a5c7-99ef37b08529)<br>
공의 궤적을 보고 자연스럽게 수비수가 따라간다.


### 주자
<img width="441" height="414" alt="image" src="https://github.com/user-attachments/assets/0941e6a1-bc85-419c-bb0a-08aaea00674d" /><br>
base_index<br><br>

<img width="1083" height="397" alt="image" src="https://github.com/user-attachments/assets/26dba83f-9f7a-4b47-8fd2-017a2f13dd4d" /><br>
- 주자 시점
- 주자 유니폼
  - 수비와 주자 모두 유니폼을 입혀 쉽게 구분하게 만들었습니다. 방망이로 치면 주자는 이동할 수 있고 베이스에 있는 주자도 베이스를 향해 뛸 수 있다.

## 파울, 플라잉 아웃
경기 중에 파울이나 플라잉 아웃을 하면 전 베이스로 돌아가야 합니다. 그래서 기존의 정보를 저장하는 기능과 복귀하는 과정을 구현했다.

### 기존 상태 저장
```
//runners, score
public void SaveBeforeStatus()
{
    before_score = _teamStatus[GetTeamIndex()].Score;
    
    before_runners.Clear();
    for (int i = 0; i < runners.Count; i++)
    {
        before_runners.Add(runners[i].BaseIndex);
    }
}
```

득점 후 파울 판정 시, 주자 리셋
```
if (gamePlayModel.BeforeScore != gamePlayModel.GetScore())
{
    //runners의 맨 앞으로 이동
    gamePlayModel.InsertRunner(CreateBatter(false, 0));
}
```

### 파울 롤백
```
//foul
public void FoulRollbackBeforeStatus()
{
    _teamStatus[GetTeamIndex()].Score = before_score;

    //그리고 주자 맨 뒤는 제거. 혹시 모르니 if문으로 사이즈 오버되면 null처리
    for (int i = 0; i < before_runners.Count; i++)
    {
        runners[i].SetBaseIndexPosition(before_runners[i]);
        runners[i].IsMove = false;
    }
}
```

### 득점 취소 후 이전 베이스로 주자 재배치
```
int n = gamePlayModel.GetScore() - gamePlayModel.BeforeScore;
for (int i = 0; i < n; i++)
{
    gamePlayModel.InsertRunner(CreateBatter(false, 0));
}
```

### 뜬공 아웃 시 주자의 귀루 시스템
```
public void FlyingOutRollbackBeforeStatus()
{
    _teamStatus[GetTeamIndex()].Score = before_score;

    for (int i = 0; i < before_runners.Count; i++)
    {
        //되돌아가는 기능
        runners[i].BaseIndex = before_runners[i] - 1;
        runners[i].IsMove = true;
    }
}
```
플라잉 아웃이 되면 주자들이 알아서 돌아간다.

