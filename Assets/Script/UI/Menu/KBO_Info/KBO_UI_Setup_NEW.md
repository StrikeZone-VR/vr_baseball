# KBO 정보 UI 만들기 가이드 (완전 새로 만들기)

**기존 메뉴 Canvas 옆에 새로운 KBO 전용 Canvas를 처음부터 만드는 방법입니다.**  
**최대한 간단하고 자세하게 설명합니다!**

---

## 🎯 Step 1: 새로운 KBO Canvas 만들기

### 1.1 Canvas 생성

1. **Hierarchy 창에서 우클릭**
2. **UI → Canvas** 선택
3. **이름을 "KBO_Canvas"로 변경**

### 1.2 Canvas 설정 (기존 메뉴와 동일하게 맞추기)

1. **KBO_Canvas 선택**
2. **Inspector에서 Canvas 컴포넌트**:
   - **Render Mode**: World Space (기존 메뉴와 같게)
   - **Event Camera**: Main Camera 드래그해서 넣기
3. **RectTransform 설정** (기존 메뉴 옆에 배치):
   - **Pos X**: 5 (기존 메뉴 오른쪽에)
   - **Pos Y**: 0
   - **Pos Z**: 0
   - **Width**: 4
   - **Height**: 3

### 1.3 Canvas Scaler 추가 (선택사항)

1. **KBO_Canvas에 Canvas Scaler 컴포넌트 추가**
2. **UI Scale Mode**: Constant Pixel Size (기본값 그대로)

---

## 📱 Step 2: KBO 메인 패널 만들기

### 2.1 메인 패널 생성

1. **KBO_Canvas 우클릭**
2. **UI → Panel** 선택
3. **이름을 "KBO_MainPanel"로 변경**

### 2.2 메인 패널 크기 설정 (Canvas 전체 크기로 맞추기)

1. **KBO_MainPanel 선택**
2. **Inspector 창에서 RectTransform 컴포넌트 찾기**
3. **Anchor 설정**:
   - RectTransform에서 **Anchor 버튼 클릭** (사각형 십자가 모양 아이콘)
   - **Alt + Shift 키를 누른 상태로** 오른쪽 아래 모서리 클릭
   - 이렇게 하면 패널이 Canvas 전체 크기로 늘어남
4. **Margin 설정**:
   - **Left**: 0, **Right**: 0, **Top**: 0, **Bottom**: 0
   - (이미 0으로 되어 있을 수 있음)

### 2.3 패널 배경색 설정

1. **KBO_MainPanel이 선택된 상태에서**
2. **Inspector 창에서 Image 컴포넌트 찾기**
3. **Color 설정**:
   - **Color 박스 클릭** (색상 선택창이 열림)
   - **R**: 240, **G**: 240, **B**: 240, **A**: 255 입력
   - 또는 **Hex 값**: #F0F0F0FF 입력
   - **Apply 또는 색상창 닫기**

---

## 🏷️ Step 3: 제목 영역 만들기

### 3.1 제목 텍스트 추가

1. **KBO_MainPanel 우클릭**
2. **UI → Text - TextMeshPro** 선택
3. **이름을 "TitleText"로 변경**

### 3.2 제목 텍스트 설정

1. **TitleText 선택**
2. **RectTransform**:
   - **Anchor**: Top Stretch (상단 가로 늘이기)
   - **Pos X**: 0, **Pos Y**: -50
   - **Width**: 0 (자동), **Height**: 100
3. **TextMeshPro 컴포넌트**:
   - **Text**: "KBO 리그 정보"
   - **Font Size**: 48
   - **Alignment**: Center (가운데 정렬)
   - **Font Style**: Bold

---

## 🔘 Step 4: 탭 버튼들 만들기

### 4.1 탭 버튼 컨테이너 생성

1. **KBO_MainPanel 우클릭**
2. **Create Empty** 선택
3. **이름을 "TabButtonContainer"로 변경**

### 4.2 탭 컨테이너 설정

1. **TabButtonContainer 선택**
2. **RectTransform**:
   - **Anchor**: Top Stretch
   - **Pos X**: 0, **Pos Y**: -150
   - **Width**: 0 (자동), **Height**: 80
3. **Layout 컴포넌트 추가**:
   - **Add Component** 버튼 클릭
   - **Horizontal Layout Group** 검색해서 추가
   - **Spacing**: 20
   - **Child Alignment**: Middle Center

### 4.3 탭 버튼 3개 만들기

**팀 순위 버튼:**

1. **TabButtonContainer 우클릭**
2. **UI → Button - TextMeshPro** 선택
3. **이름을 "TeamRankingTabButton"로 변경**
4. **Button 설정**:
   - **RectTransform**: Width: 200, Height: 60
   - **Image**: Color - 연한 파랑 (200, 220, 255, 255)
5. **자식 Text 설정**:
   - **Text**: "팀 순위"
   - **Font Size**: 24

**타자 버튼 (같은 방식으로):**

1. **TeamRankingTabButton을 복사** (Ctrl+D)
2. **이름을 "HittersTabButton"로 변경**
3. **자식 Text**: "타자"

**투수 버튼 (같은 방식으로):**

1. **HittersTabButton을 복사** (Ctrl+D)
2. **이름을 "PitchersTabButton"로 변경**
3. **자식 Text**: "투수"

### 4.4 새로고침 버튼 추가

1. **PitchersTabButton을 복사** (Ctrl+D)
2. **이름을 "RefreshButton"로 변경**
3. **자식 Text**: "새로고침"
4. **Image Color**: 연한 녹색 (200, 255, 200, 255)

---

## 📊 Step 5: 테이블 영역 만들기

### 5.1 스크롤 영역 생성

1. **KBO_MainPanel 우클릭**
2. **UI → Scroll View** 선택
3. **이름을 "DataScrollView"로 변경**

### 5.2 스크롤 영역 설정

1. **DataScrollView 선택**
2. **RectTransform**:
   - **Anchor**: Stretch (전체)
   - **Left**: 20, **Right**: 20
   - **Top**: 250, **Bottom**: 20
3. **Scroll Rect 컴포넌트**:
   - **Horizontal**: 체크 해제 (가로 스크롤 끄기)
   - **Vertical**: 체크 유지

### 5.3 테이블 컨테이너들 만들기

**Content 안에 헤더 컨테이너:**

1. **DataScrollView → Viewport → Content 선택**
2. **우클릭 → Create Empty**
3. **이름을 "HeaderContainer"로 변경**
4. **Layout 추가**: Horizontal Layout Group
   - **Child Force Expand Width**: 체크
   - **Child Control Width**: 체크

**Content 안에 행 컨테이너:**

1. **Content 선택**
2. **우클릭 → Create Empty**
3. **이름을 "RowContainer"로 변경**
4. **Layout 추가**: Vertical Layout Group
   - **Child Force Expand Width**: 체크
   - **Spacing**: 2

**Content Size Fitter 추가:**

1. **Content 선택**
2. **Add Component** → **Content Size Fitter**
3. **Vertical Fit**: Preferred Size

---

## ⏳ Step 6: 로딩/에러 표시 만들기

### 6.1 로딩 표시

1. **KBO_MainPanel 우클릭**
2. **UI → Text - TextMeshPro** 선택
3. **이름을 "LoadingIndicator"로 변경**
4. **설정**:
   - **Anchor**: Center
   - **Text**: "데이터 로딩 중..."
   - **Font Size**: 36
   - **Alignment**: Center
   - **기본적으로 비활성화** (Active 체크 해제)

### 6.2 에러 패널

1. **KBO_MainPanel 우클릭**
2. **UI → Panel** 선택
3. **이름을 "ErrorPanel"로 변경**
4. **설정**:
   - **Anchor**: Center
   - **Width**: 600, **Height**: 200
   - **Image Color**: 빨간색 (255, 200, 200, 255)
   - **기본적으로 비활성화** (Active 체크 해제)

**에러 텍스트 추가:**

1. **ErrorPanel 우클릭**
2. **UI → Text - TextMeshPro** 선택
3. **이름을 "ErrorText"로 변경**
4. **설정**:
   - **Anchor**: Stretch
   - **Text**: "오류 메시지"
   - **Alignment**: Center
   - **Color**: 빨간색

---

## 🔧 Step 7: 프리팹 만들기 (테이블 템플릿)

> **⚠️ 중요:** 이 단계는 **테이블 헤더와 데이터 행의 템플릿**을 만드는 것입니다!  
> **화면에 보이면 안 되고**, **Project 창에 파일로 저장**되어야 합니다!

### 7.1 헤더 셀 프리팹 (테이블 제목 템플릿)

**🎯 역할:** "팀명", "승", "패", "승률" 등의 테이블 헤더를 만들 템플릿

**1단계: 임시로 헤더셀 만들기**

1. **Hierarchy에서 우클릭**
2. **Create Empty** 선택
3. **이름을 "HeaderCell"로 변경**

**2단계: 헤더셀 모양 설정**

1. **HeaderCell 선택**
2. **Inspector에서 설정**:
   - **RectTransform**: Width: 100, Height: 40
   - **Add Component → Image** 클릭
   - **Image의 Color**: 어두운 회색 (R:80, G:80, B:80, A:255)

**3단계: 헤더 텍스트 추가**

1. **HeaderCell 우클릭 → UI → Text - TextMeshPro**
2. **이름을 "HeaderText"로 변경**
3. **HeaderText 설정**:
   - **Anchor**: Stretch (전체 늘이기)
   - **Text**: "헤더" (임시 텍스트)
   - **Color**: 흰색 (255, 255, 255, 255)
   - **Alignment**: Center (가운데 정렬)
   - **Font Style**: Bold (굵게)

**4단계: 스크립트 추가**

1. **HeaderCell 선택**
2. **Add Component → KBOTableHeaderCell** 검색해서 추가

**5단계: ⭐ 프리팹으로 저장하고 삭제하기**

1. **Project 창에서 Assets 폴더 우클릭**
2. **Create → Folder** → **이름: "Prefabs"**
3. **Prefabs 폴더 안에 Create → Folder** → **이름: "UI"**
4. **HeaderCell을 Project의 Assets/Prefabs/UI/ 폴더로 드래그**
5. **⭐ 중요: Hierarchy에서 HeaderCell 우클릭 → Delete** (화면에서 제거)

> **✅ 완료 확인:** Project 창의 Assets/Prefabs/UI/에 HeaderCell 프리팹 파일이 있어야 함!

### 7.2 데이터 행 프리팹

**프리팹 생성:**

1. **Hierarchy에서 우클릭**
2. **Create Empty** 선택
3. **이름을 "DataRow"로 변경**

**설정:**

1. **DataRow 선택**
2. **RectTransform**: Width: 800, Height: 35
3. **Add Component → Image**:
   - **Color**: 흰색 (255, 255, 255, 255)
4. **Add Component → Horizontal Layout Group**:
   - **Child Force Expand Width**: 체크
   - **Child Control Width**: 체크

**텍스트 셀들 추가 (10개):**

1. **DataRow 우클릭 → UI → Text - TextMeshPro**
2. **이름을 "Cell1"로 변경**
3. **설정**:
   - **Text**: "데이터"
   - **Alignment**: Center
4. **Cell1을 9번 복사**해서 Cell2~Cell10 만들기

**스크립트 추가:**

1. **DataRow 선택**
2. **Add Component** → **KBOTableDataRow** 검색해서 추가

**프리팹 저장:**

1. **DataRow를 Project 창으로 드래그**
2. **Assets/Prefabs/UI/** 폴더에 저장
3. **Hierarchy에서 DataRow 삭제**

---

## ⚙️ Step 8: KBOInfoManager 스크립트 연결

### 8.1 스크립트 추가

1. **KBO_MainPanel 선택**
2. **Add Component** → **KBOInfoManager** 검색해서 추가

### 8.2 Inspector에서 참조 연결

**KBOInfoManager 컴포넌트에서 모든 필드를 다음과 같이 드래그해서 연결:**

- **KBO Info Panel**: KBO_MainPanel
- **Title Text**: TitleText
- **Team Ranking Tab Button**: TeamRankingTabButton
- **Hitters Tab Button**: HittersTabButton
- **Pitchers Tab Button**: PitchersTabButton
- **Content Area**: DataScrollView
- **Header Container**: HeaderContainer
- **Row Container**: RowContainer
- **Header Cell Prefab**: Project에서 HeaderCell 프리팹 드래그
- **Row Prefab**: Project에서 DataRow 프리팹 드래그
- **Loading Indicator**: LoadingIndicator
- **Error Panel**: ErrorPanel
- **Error Text**: ErrorText
- **Refresh Button**: RefreshButton

---

## 🚀 Step 9: 최종 설정 (항상 표시)

### 9.1 KBO 패널 활성화 확인 (필수!)

1. **KBO_MainPanel 선택**
2. **Inspector에서 Active 체크박스 켜기** ✅ (활성화)
3. **이렇게 하면 메뉴 씬에서 항상 보임**

### 9.2 Canvas 위치 조정 (필요시)

1. **KBO_Canvas 선택**
2. **RectTransform에서 위치 조정**:
   - **기존 메뉴와 겹치지 않게** 적절한 거리에 배치
   - **Pos X**: 5~8 (기존 메뉴 옆에)

---

## 🎮 Step 10: 테스트

1. **Play Mode 실행**
2. **"KBO 정보" 버튼 클릭** → KBO 캔버스가 나타나는지 확인
3. **탭 버튼들 클릭** → 데이터 전환 확인
4. **새로고침 버튼** → API 호출 확인

---

## 🎯 완성!

**이제 완전히 새로운 KBO 전용 Canvas가 기존 메뉴 옆에 생겼습니다!**

- ✅ **독립적인 KBO Canvas**: 기존 메뉴와 분리된 새로운 캔버스
- ✅ **현대적인 UI**: 탭, 테이블, 로딩 표시 모두 포함
- ✅ **실시간 데이터**: API 연결로 최신 KBO 정보 표시
- ✅ **VR 호환**: World Space Canvas로 VR 환경에서 사용 가능

**문제가 있으면 각 단계를 차근차근 다시 확인해보세요!** 🚀
