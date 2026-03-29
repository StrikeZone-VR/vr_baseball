# 아키텍처
프로젝트의 구조를 도식화했습니다.

---
## 상속관계
<img width="1176" height="575" alt="image" src="https://github.com/user-attachments/assets/b15b303e-dcf8-42c3-b97e-b3a908610066" />
중첩되는 코드들은 상속 관계로 개선시켰다. 코드 간의 의존성 관계가 애매하게 있어서 MVC 구조를 활용했습니다.
다만 특이하게 여기서는 Controller를 View처럼 UI를 관리하는 역할이고 기존의 MVC구조의 Controller 역할을 Manager가 합니다.

## 시퀸스 다이어그램

<img width="625" height="546" alt="image" src="https://github.com/user-attachments/assets/9c100d52-4531-479a-823c-8fb9099525ea" />

함수 호출이 복잡해짐에 따라 변수의 상태 변화를 추적하는 데 어려움이 있었습니다. 이를 해결하기 위해 GamePlayManager를 중심으로 시퀸스 다이어그램을 작성하여 로직을 시각화 했습니다. 덕분에 특정 함수가 실행될 때 변수가 어떤 값으로 제어되는지 직관적으로 확인하며 로직의 논리적 결함을 예방할 수 있었습니다.

### AI 투수가 공을 받은 함수
<img width="864" height="709" alt="image" src="https://github.com/user-attachments/assets/6c490bb3-83cc-4097-a301-ffe1588f50b4" />

### 투수가 공을 던지는 함수
<img width="1275" height="524" alt="image" src="https://github.com/user-attachments/assets/775bd30f-6c49-49d8-8301-ff3f53e96148" />
