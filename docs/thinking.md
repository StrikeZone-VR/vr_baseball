# 주자 사라짐 버그 — 사수 예상 답안 (스포일러, 필요할 때만 열어볼 것)

## 확인된 사실 (지금까지 대화에서 코드로 검증됨)

- 강제아웃 판정은 `GamePlayManager.OutRunner(int)` (`GamePlayManager.Defense.cs:116`)가 담당.
- `BasemanComponent.IsInPosition` setter(`BasemanComponent.cs:75-85`)가 값이 바뀔 때마다 `OutRunner()` → `outRunnerEvent.RaiseEvent(base_index-1)` 발행 → `GamePlayManager.OutRunner(int)`로 연결(`GamePlayManager.cs:108` 구독).
- `GamePlayManager.OutRunner(int)`는 `if (!runner.IsMove) return;` 가드로 막힌다(140번대 줄) — 주자가 "달리는 중"이 아니면 조용히 스킵, 아웃도 세이프도 아무 결과도 안 남김.
- `runner.IsMove`는 주자가 베이스 트리거에 닿는 즉시 확정된다(`BatterComponent.OnTriggerEnter` → `TryExtraBase()`, `BatterComponent.cs:41-59`). 이 판단은 오직 "공이 현재 `BallState.Thrown`인가"만 보고, 수비수가 실제로 도착했는지/공을 잡았는지는 전혀 안 봄.
- 수비수의 `IsInPosition=true`는 완전히 별개 타이밍(NavMeshAgent가 실제로 걸어서 거리 안에 들어왔을 때, `BasemanComponent.UpdateInPositionByDistance`)에 일어남.
- 두 이벤트 사이에 순서 보장이 전혀 없음 → 전형적인 조건 경합(race condition).

## 예상 원인

`TryExtraBase()`가 `IsMove=false`로 "정착"을 확정하는 순간, 그 판정은 그걸로 끝 — 이후 수비수가 도착해서 `IsInPosition=true`가 되어도, `GamePlayManager.OutRunner(int)`가 그 시점에만(이벤트 기반) 한 번 체크하고 이미 `IsMove=false`라 그냥 스킵해버림. 아무도 "나중에 다시 확인"을 안 함. 정착 판정과 아웃 판정이 서로 다른 트리거(콜라이더 진입 vs 거리 폴링)로 완전히 분리돼 있어서, 어느 쪽이 먼저 끝나느냐에 따라 결과가 갈리는 구조.

## 예상 수정 방향 (스포일러)

1. **가장 직접적**: `BatterComponent`가 `IsMove=false`로 정착하는 그 순간, 혹시 그 베이스의 수비수가 이미 `IsInPosition && 공을 들고 있음` 상태라면 그 자리에서 즉시 아웃 판정을 한 번 더 시도(재조회/재발행). 즉 "둘 중 늦게 끝나는 쪽이 트리거"가 되게 양방향으로 체크.
2. **대안**: `GamePlayManager.OutRunner(int)`의 `!runner.IsMove` 가드를 완화. 다만 이 가드는 포스아웃 규칙(안 뛰는 주자는 강제아웃 안 됨) 자체를 보호하는 것일 수 있어서, 없애면 진짜 세이프여야 할 상황까지 아웃 처리할 위험 있음 — 신중하게.
3. 근본적으로는 "정착"과 "아웃판정"을 하나의 원자적 체크로 합치거나, 최소한 상태가 바뀔 때마다 서로를 재확인하는 구조로 가야 완전히 해결됨.

## 확신도

리플레이 2개(사라짐/사라짐2)로 방향성은 꽤 확실하게 재현/설명됨. 다만 정확한 구현 디테일(어디서 재확인 호출을 넣을지, 가드 완화가 다른 룰을 깨진 않는지)은 실제로 고쳐보면서 리플레이로 재검증 필요.

---

## 추가(2026-07-17): "사라짐" 리플레이는 사실 다른 실패 케이스였음

`frames.ball.pos`를 96.7~101.2초 구간 통째로 찍어보니, 위에서 가정한 "1루수가 잡긴 잡았는데 타이밍 레이스로 아웃 판정을 놓침" 시나리오가 아니었다.

- 96.759995: 공이 던진 자리에서 거의 안 움직인 채로 재취득 (짧은 릴레이).
- 96.769 두 번째 던지기부터 101.14까지 **4.4초 내내 공중 체공**, y 0.5→23.5(정점)→0. 완전한 포물선 하나. 착지 `(-26.6, 0, 0.77)` — 1루 바로 옆인데 z>0이라 파울 라인 밖.
- **1루수는 이 공을 잡은 적이 없다.** 좌익수 쪽 중계 송구가 45km/h 고정 속력 + 장거리 때문에 초고각 롭이 되어([[project_replay_asset_forensics]] 참고) 아무도 못 받고 파울 라인 밖에 떨어진 오버스로우였음.
- 로그에 "아웃" 텍스트는 전혀 없음 — `Foul()→RollbackBeforeStatus()→RemoveLastRunner()` 버그(이미 수정됨)로 주자가 삭제된 것.

즉 **"주자 사라짐"은 최소 두 가지 서로 다른 실패 모드가 섞여 있었다**:
1. (위 원래 이론) 캐치는 성공했는데 `IsMove`/`IsInPosition` 순서 레이스로 포스아웃 판정을 놓치는 케이스 — 아직 미확인, "사라짐2"의 첫 더블패스(20.76s)는 정상적으로 "아웃"까지 났으니 이쪽은 다른 리플레이로 더 검증 필요.
2. (이번에 새로 확인) 수비 송구가 아예 빗나가서 아무도 못 받고 파울 라인 밖으로 나가는 케이스 — 이 경우 지금 고친 `_wasThrownByDefenseThisPlay` 가드가 `Foul()`을 무시하긴 하는데, **`CurrentState`를 아무것도 안 바꾸고 그냥 return**한다. 공이 착지하면 `BaseballPhysics.OnCollisionEnter`(Ground 태그)가 또 `Foul()`을 부를 텐데 그것도 씹히면, `IsInGamePlay`가 계속 true인 채로 플레이가 영영 안 끝나고 멈출 위험이 있음. 주자 삭제는 막았지만 새로운 "게임 멈춤" 구멍을 만들었을 수 있음 — 미해결, 확인 필요.
