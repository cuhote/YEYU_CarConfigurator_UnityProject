# 프로젝트 파일 구조

> 기능 중심 차량 컨피규레이터 · 팀 YEYU · 기준: SRS v1.0 (MainScene 하나 + 영역 프리팹 3개) · 2026.10.13 구현 결과 반영
> 클래스 단위 설계(속성·메서드·관계)는 [`CLASS_DESIGN.md`](./CLASS_DESIGN.md), 장면별 수치는 SRS(`README.md`) 부록 A를 본다.
> 구현하지 않기로 한 파일(`CarFeatureSwitch`, `ScenarioTrigger`, `ShowroomCamera`, `ScenarioHUD`, `OptionCardPanel`, `Units`)은 이 문서에서 뺐다.

---

## 1. 전체 트리

```
YEYU_CarConfigurator_UnityProject/     ← GitHub 저장소 루트 = Unity 프로젝트 루트
├── Assets/
│   ├── _Project/                      ← 우리 팀이 만든 파일은 전부 여기 (에셋 스토어 파일과 분리)
│   │   ├── Scenes/
│   │   │   └── MainScene.unity        ← 유일한 씬. 공통 UI와 영역 프리팹 3개를 가진다
│   │   │
│   │   ├── Scripts/
│   │   │   ├── Core/                  ← 앱 뼈대: 영역 전환, 옵션 데이터, 화면 이동 기록
│   │   │   │   ├── ModeManager.cs
│   │   │   │   ├── AreaBase.cs
│   │   │   │   ├── AppMode.cs
│   │   │   │   ├── CarConfig.cs
│   │   │   │   ├── ConfiguratorArea.cs
│   │   │   │   └── ScreenHistory.cs
│   │   │   │
│   │   │   ├── Vehicle/               ← 두 시나리오가 같이 쓰는 차량
│   │   │   │   ├── CarController.cs
│   │   │   │   ├── CarAppearance.cs
│   │   │   │   └── ICarFeature.cs
│   │   │   │
│   │   │   ├── Camera/                ← 시점 전환 (분할 카메라 켜고 끄기는 AreaBase)
│   │   │   │   ├── CameraSwitcher.cs
│   │   │   │   ├── CameraAnchors.cs
│   │   │   │   ├── _CameraSwitcherTester.cs
│   │   │   │   └── Editor/
│   │   │   │       └── _SceneViewCameraAligner.cs
│   │   │   │
│   │   │   ├── Scenario/              ← 시나리오 공통 틀 (장면 순서, 장면 전환)
│   │   │   │   └── ScenarioBase.cs
│   │   │   │
│   │   │   ├── UI/                    ← MainScene 공통 Canvas
│   │   │   │   ├── UIManager.cs
│   │   │   │   ├── ExteriorPanel.cs
│   │   │   │   ├── FeaturePanel.cs
│   │   │   │   ├── NavigationBar.cs
│   │   │   │   └── Editor/            ← 화면을 목업 치수대로 조립하는 에디터 메뉴 (Yeyu > UI)
│   │   │   │       ├── _ExteriorPanelBuilder.cs
│   │   │   │       ├── _FeaturePanelBuilder.cs
│   │   │   │       ├── _NavigationBarBuilder.cs
│   │   │   │       └── _SmartKeyUIBuilder.cs
│   │   │   │
│   │   │   ├── Driving/               ← 주행 파트 전용
│   │   │   │   ├── DrivingArea.cs
│   │   │   │   ├── DrivingScenario.cs
│   │   │   │   ├── VirtualDriver.cs
│   │   │   │   ├── Pedestrian.cs
│   │   │   │   ├── NightVision/
│   │   │   │   │   ├── NightVisionSensor.cs
│   │   │   │   │   └── NightVisionDisplay.cs
│   │   │   │   ├── RearWheelSteering/
│   │   │   │   │   ├── RearWheelSteering.cs
│   │   │   │   │   └── TurningRadiusCalculator.cs
│   │   │   │   └── Visualization/
│   │   │   │       └── DrivingDrawer.cs
│   │   │   │
│   │   │   └── Parking/               ← 주차 파트 전용
│   │   │       ├── ParkingArea.cs
│   │   │       ├── ParkingScenario.cs
│   │   │       ├── _ParkingAreaTester.cs
│   │   │       ├── Valet/
│   │   │       │   ├── GridMap.cs
│   │   │       │   ├── ParkingSlot.cs
│   │   │       │   ├── ParkingSlotAllocator.cs
│   │   │       │   ├── ReservationTable.cs
│   │   │       │   └── ValetAgent.cs
│   │   │       ├── RemoteParking/
│   │   │       │   ├── RemoteParking.cs
│   │   │       │   ├── SmartKeyUI.cs
│   │   │       │   └── ProximityStop.cs
│   │   │       └── Visualization/
│   │   │           ├── ParkingDrawer.cs
│   │   │           ├── DoorSwingDrawer.cs
│   │   │           └── VehicleStatusLabel.cs
│   │   │
│   │   ├── Prefabs/
│   │   │   ├── ConfiguratorArea.prefab ← 옵션 선택 영역 (쇼룸, 미리보기 차량)
│   │   │   ├── Vehicle/
│   │   │   │   └── Car.prefab          ← 주행·주차·쇼룸 공통 차량 (색상 4종, 휠 2종 포함)
│   │   │   ├── Driving/
│   │   │   │   ├── DrivingArea.prefab  ← 주행 영역 (코스 A·B, 차량 2대, 보행자, 열화상 화면)
│   │   │   │   ├── DrivingArea/        ← 열화상 볼륨 프로파일
│   │   │   │   └── Pedestrian.prefab
│   │   │   ├── Parking/
│   │   │   │   ├── ParkingArea.prefab  ← 주차 영역 (주차장 A·B, 스마트키 화면)
│   │   │   │   ├── ParkingLot.prefab   ← 주차장 1벌 (차량, 주차면, 좁은 주차면 묶음 NarrowSlotSet)
│   │   │   │   └── _ParkingAreaTester.prefab
│   │   │   └── UI/
│   │   │       ├── ExteriorPanel.prefab
│   │   │       └── FeaturePanel.prefab
│   │   │
│   │   ├── Data/
│   │   │   └── CarConfig.asset         ← 선택 옵션 (유일한 ScriptableObject)
│   │   │
│   │   ├── Materials/
│   │   │   └── Visualization/          ← 원뿔·궤적·유턴 화살표 머티리얼
│   │   │
│   │   ├── RenderTextures/
│   │   │   └── NightVisionRT.renderTexture
│   │   │
│   │   └── UI/
│   │       ├── Icons/                  ← 버튼·카드 모양, 기능 아이콘
│   │       └── Fonts/                  ← Pretendard (Regular, Bold)
│   │
│   ├── ThirdParty/                    ← 에셋 스토어 차량·주차장·도로 에셋 (수정 금지)
│   └── TextMesh Pro/                  ← Unity가 자동 생성
│
├── Packages/                          ← 커밋 O
├── ProjectSettings/                   ← 커밋 O (레이어·태그·빌드 설정)
├── docs/
│   ├── WBS.md
│   ├── WBS.xlsx
│   ├── FOLDER_STRUCTURE.md            ← 이 문서
│   ├── CLASS_DESIGN.md                ← 클래스 다이어그램 (Mermaid)
│   ├── notes/                         ← 구현 메모 (예약 테이블, 발렛 상태 라벨)
│   ├── TroubleShooting/               ← 개발 중 발견된 문제/원인/해결 기록
│   └── images/
├── scripts/
│   ├── setup-git-hooks.sh             ← .githooks/ 활성화 (클론 후 1회 실행)
│   └── check-unity-meta.sh            ← 에셋 ↔ .meta 짝 검사 (훅·CI 공용)
├── .githooks/                         ← commit-msg · pre-commit (COMMIT_RULES.md 강제)
├── .github/workflows/                 ← meta-check (push·PR마다 .meta 검사)
├── COMMIT_RULES.md                    ← 브랜치 전략 · 커밋 메시지 · PR 규칙
├── CONVENTIONS.md                     ← 네이밍 · 파일 관리 · 폴더 담당 규칙
├── .gitignore
├── .gitattributes                     ← Git LFS 설정, 줄바꿈 규칙
└── README.md                          ← SRS (요구사항 명세서)
```

`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Build/`는 `.gitignore`로 제외한다.

이름이 `_`로 시작하는 스크립트·프리팹은 테스트용이거나 에디터 전용이다(`CONVENTIONS.md`).

---

## 2. 폴더 구분 원칙

| 구분 | 폴더 | 누가 수정하나 |
| --- | --- | --- |
| 공통 | `Core/`, `Vehicle/`, `Camera/`, `Scenario/`, `UI/` | 6장에서 정한 담당자만. 다른 사람은 수정 전에 알림 |
| 주행 전용 | `Scripts/Driving/`, `Prefabs/Driving/` | 김예은 |
| 주차 전용 | `Scripts/Parking/`, `Prefabs/Parking/` | 강유나 |
| 외부 에셋 | `ThirdParty/` | 수정 금지. 바꿀 땐 `_Project/Prefabs`로 복사해서 수정 |

이렇게 나누면 각자 자기 폴더만 고치는 동안에는 Git 충돌이 나지 않는다. 충돌 위험은 공통 폴더와 `MainScene.unity`에만 남는다.

---

## 3. 씬과 영역 구성

씬은 `MainScene.unity` 하나다. 영역 3개는 프리팹으로 만들어 MainScene에 함께 배치하고, 켜고 끄는 방식으로 화면을 바꾼다(SRS FR-36, FR-37).

| 위치 | 루트 오브젝트 | 들어가는 것 | 조명 |
| --- | --- | --- | --- |
| `MainScene` | `App` | `ModeManager`, `CameraSwitcher`, `ScreenHistory`, `UIManager` | 없음 |
| `MainScene` | `Canvas` | `ExteriorPanel`, `FeaturePanel`, `NavigationBar`, 시점 탭과 좌우 라벨(`ScreenButtons`) | - |
| `ConfiguratorArea.prefab` | `ConfiguratorArea` (X=0) | 쇼룸 조명, 카메라, 미리보기 차량, `ConfiguratorArea` | 밝은 실내 |
| `DrivingArea.prefab` | `DrivingArea` (X=2000) | 코스 A(X=2000)·B(X=3000), 차량 2대, 보행자, 분할 카메라 2대, 열화상 화면(`DrivingCanvas`) | 야간 (유턴 장면은 밝게) |
| `ParkingArea.prefab` | `ParkingArea` (X=4000) | 주차장 A(X=4000)·B(X=5000) = `ParkingLot` 2벌, 차량 E·NPC, 좁은 주차면 묶음, 분할 카메라 2대, 스마트키 화면(`SmartKeyCanvas`) | 주차장 조명 |

각 영역 프리팹의 루트 오브젝트에 해당 `AreaBase` 상속 스크립트(`ConfiguratorArea.cs`, `DrivingArea.cs`, `ParkingArea.cs`)를 붙인다. 조명은 각 `AreaBase`의 ambient 색 필드에 넣고 `OnEnter()`에서 적용한다(FR-38).

---

## 4. 핵심 연결 구조

```
UIManager ──(ShowExterior / ShowFeature / ShowDrivingScene / ShowParkingScene)
   │   화면에 맞는 패널만 켜고, 시점 탭 강조와 오른쪽 라벨 글자도 여기서 맞춘다
   ▼
ModeManager ──(SwitchTo)──▶ AreaBase  ── ResetArea(CarConfig) → OnEnter(): 영역 조명 적용 + 분할 카메라 2대 켜기
   │                           ├ ConfiguratorArea (쇼룸, 미리보기 차량)
   │                           ├ DrivingArea      → DrivingScenario (ScenarioBase)
   │                           └ ParkingArea      → ParkingScenario (ScenarioBase)
   │
   ├─ ScreenHistory : 거쳐 온 화면 기록 → NavigationBar의 뒤로가기·홈 (FR-53, FR-54)
   └─ CarConfig (외관 2개 + 기능 4개) ◀── ExteriorPanel 버튼 · FeaturePanel 체크박스
          │
          ▼
   Car.prefab (주행·주차·쇼룸 공통)
     ├ CarController        : 유일한 구동부. 자전거 모델(앞·뒷바퀴 조향), 웨이포인트 추종, ResetPose(받은 시작 위치로 복귀)
     ├ CarAppearance        : CarConfig 외관 값(색상·휠) 적용. 영역의 ResetArea가 사용자 차에만 부른다
     └ (기능 컴포넌트)       : NightVisionSensor, RearWheelSteering, RemoteParking → ICarFeature

기능 켜고 끄기 (CarFeatureSwitch 없이 영역이 직접 한다):
  나이트 비전·후륜 조향 : DrivingArea가 장면을 준비할 때 CarConfig 값으로 SetActive
  발렛                  : ParkingArea.ResetArea()가 valetParking 값으로 주차장 B의 ReservationTable을 켜고 끔 (A는 항상 OFF)
  스마트키 원격 주차     : ParkingScenario가 스마트키 장면에서 RemoteParking과 스마트키 화면을 켬

운전자 (두 차량이 같은 방식으로 운전, 오른쪽만 기능 ON):
  주행: VirtualDriver ──▶ CarController      (NightVisionSensor → VirtualDriver.RequestBrake)
        유턴은 웨이포인트가 아니라 돈 각도를 누적하는 구간 방식. 구간 목록만 다르게 주면 반경이 다른 두 차를 같은 코드로 몬다
  주차: ValetAgent    ──▶ CarController      (전역 웨이포인트 → 후진 웨이포인트)

시작 위치 (FR-39): DrivingArea·ParkingArea가 장면별·차량별 시작 위치(Transform)를 들고 있고,
  장면을 시작하거나 다시 시작할 때 GetStartPose(장면, 차량)로 꺼내 CarController.ResetPose()에 넘긴다.

ScenarioBase : 장면 목록 → 꺼진 옵션 장면 건너뛰기(FR-62) → 장면 끝나면 0.5초 간격 후 다음 장면(FR-57)
```

- 측정·결과 기록은 범위에서 제외했다(SRS v0.7). 화면 숫자(회전 반경)는 계산해서 표시만 한다.
- 시나리오 중 옵션을 켜고 끄는 옵션 카드는 범위에서 제외했다(SRS v1.0). 옵션은 기능 선택 화면에서만 바꾼다.
- 주행·주차 기능은 서로를 참조하지 않는다. 공통 파일만 참조한다.

---

## 5. 파일별 역할

### 5.1 공통 파일

| 파일 | 역할 | 관련 요구사항 | 담당 |
| --- | --- | --- | --- |
| `Core/ModeManager.cs` | MainScene에 배치된 영역 3개 켜고 끄기, 영역 초기화와 진입 처리, 분할 카메라를 `CameraSwitcher`에 연결 | FR-36~38 | 김예은 |
| `Core/AreaBase.cs` | 영역 공통 부모: `Root`, `ResetArea(CarConfig)`, `OnEnter()`, `OnExit()`. `OnEnter()`에서 영역별 ambient 조명 적용, 영역 카메라 2대를 왼쪽·오른쪽 Viewport로 나눠 켜고 끔 | FR-11, FR-38, FR-39 | 김예은 |
| `Core/AppMode.cs` | `enum AppMode { Configurator, Driving, Parking }` | FR-37 | 김예은 |
| `Core/CarConfig.cs` | ScriptableObject: 외관 2개(색상, 휠) + 기능 4개(초기값 모두 선택) | FR-01~11 | 강유나 |
| `Core/ConfiguratorArea.cs` | `AreaBase` 상속, 옵션 선택 영역: 미리보기 차량에 외관 적용 | FR-01, FR-03, FR-59 | 공동 |
| `Core/ScreenHistory.cs` | 화면 이동 기록(외관 선택 → 기능 선택 → 장면). 홈이면 비움 | FR-53, FR-54 | 강유나 |
| `Vehicle/CarController.cs` | 모든 차량의 유일한 구동부: 자전거 모델로 위치·방향 갱신(앞·뒷바퀴 조향), 속도·제동, 웨이포인트 추종(`Follow`), 지면 따라가기, 받은 시작 위치로 위치·속도·조향각 초기화(`ResetPose(Pose)`). 시작 위치는 직접 저장하지 않음 | FR-39, FR-41, FR-43, NFR-02 | 김예은 |
| `Vehicle/CarAppearance.cs` | 색상·휠 교체. 차체 도장 칸의 머티리얼을 바꾸고 휠 세트를 켜고 끔 | FR-01, FR-03, FR-10 | 공동 |
| `Vehicle/ICarFeature.cs` | 차량에 붙는 기능 공통 인터페이스 (`FeatureId`, `SetActive(bool)`, `ResetFeature()`). 구현: NightVisionSensor, RearWheelSteering, RemoteParking | NFR-06 | 김예은 |
| `Camera/CameraSwitcher.cs` | AreaBase가 켠 카메라 2대의 시점 전환: 운전석 ↔ 버드뷰 동시 전환, 스마트키 장면 고정 시점, [탑승] 시 오른쪽만 지정한 운전석으로. 버드뷰에서는 지정 높이 위를 그리지 않음 | FR-12, FR-31, FR-65 | 강유나 |
| `Camera/CameraAnchors.cs` | 카메라 오브젝트에 붙는 시점 위치 보관: 운전석·버드뷰·고정 Transform 3개와 버드뷰 절단 높이를 Inspector로 연결, `CameraSwitcher`가 `Bind` 때 꺼내 사용 | FR-12, FR-31, FR-65 | 강유나 |
| `Scenario/ScenarioBase.cs` | 시나리오 공통 흐름: 장면 목록, 꺼진 장면 건너뛰기, 장면 준비(`PrepareScene`), 자동 전환(0.5초 간격), 중단(`StopScenario`) | FR-11, FR-57, FR-62, NFR-02 | 공동 |
| `UI/UIManager.cs` | 화면별 패널 전환(외관·기능·주행·주차), 시점 탭 강조, 오른쪽 화면 라벨 글자("나이트비전 적용 차량" 등). `ScenarioHUD`를 따로 두지 않고 여기에 포함 | FR-12, FR-37, FR-56 | 강유나 · 김예은 |
| `UI/ExteriorPanel.cs` | UI-01 차체 색상·휠 탭, 색상 버튼 4개, 휠 버튼 2개, 선택 요약 카드, [다음] | FR-01, FR-03, FR-58 | 공동 |
| `UI/FeaturePanel.cs` | UI-02 주행·주차 탭, 체크박스, 설명 문장 4개(`TMP_Text` 필드에 Inspector로 입력), 탭별 시작 버튼 | FR-05, FR-06, FR-60, FR-61 | 강유나 |
| `UI/NavigationBar.cs` | 왼쪽 위 [뒤로가기] [홈] 아이콘 버튼 | FR-53~56 | 강유나 |
| `UI/Editor/_*Builder.cs` | 에디터 메뉴 `Yeyu > UI`: 외관 패널, 기능 패널, 내비게이션 바, 스마트키 화면을 목업 치수(1920×1080)대로 조립해 프리팹으로 저장 | - | 강유나 |

### 5.2 주행 파트 (김예은)

| 파일 | 역할 | 관련 요구사항 |
| --- | --- | --- |
| `Driving/DrivingArea.cs` | `AreaBase` 상속, 장면별·차량별 시작 위치 보관(나이트 비전 A·B, 유턴 A·B)과 `GetStartPose()`, 장면 준비(`SetScene`), 차량 2대에 외관 적용, 센서·후륜 조향 켜고 끄기, 유턴 장면 조명 | FR-08, FR-10, FR-39 |
| `Driving/DrivingScenario.cs` | `ScenarioBase` 상속, 장면 목록: 나이트 비전 → 후륜 조향 유턴. 운전자 2명(A·B)을 같은 프레임에 출발 | FR-11, FR-57 |
| `Driving/VirtualDriver.cs` | 두 차량 공통 운전자. 나이트 비전: 60km/h 주행, 헤드램프 50m 인지 → 반응 1.5초 → 6.0m/s² 급제동, 센서의 감속 요청 수행. 유턴: 유턴 지점까지 직진한 뒤, 돈 각도를 누적해 구간별 목표 각도에 이르면 다음 구간으로 넘어감(왼쪽 90 → 115 → 180도 3구간, 오른쪽 180도 1구간) | FR-11, FR-17, FR-21 |
| `Driving/Pedestrian.cs` | 도로 위에 서 있는 보행자 표식 (움직이지 않음) | FR-17 |
| `NightVision/NightVisionSensor.cs` | 120m 감지, 거리·TTC 계산, 4초 경고·2.5초 감속(5.0m/s²)을 운전자에게 요청 | FR-15, FR-17 |
| `NightVision/NightVisionDisplay.cs` | 열화상 카메라 → RenderTexture → 클러스터 화면(운전석 시점에서만), 보행자 노란 박스, 경고 아이콘 | FR-16 |
| `RearWheelSteering/RearWheelSteering.cs` | 30km/h 미만 역위상 10도, 60km/h 이상 동위상 2도 | FR-20, FR-24 |
| `RearWheelSteering/TurningRadiusCalculator.cs` | 자전거 모델 회전 반경 R = L ÷ (tan δf − tan δr), ICR 계산 | FR-22 |
| `Visualization/DrivingDrawer.cs` | 주행 영역 그리기 전부: 헤드램프(50m)·열화상(120m) 원뿔, 유턴 궤적 선, ICR 점, 최소 반경 원, 반경 글자 | FR-18, FR-22, FR-23 |

### 5.3 주차 파트 (강유나)

| 파일 | 역할 | 관련 요구사항 |
| --- | --- | --- |
| `Parking/ParkingArea.cs` | `AreaBase` 상속, 장면별·차량별 시작 위치 보관(발렛 E·NPC1·NPC2 × A·B, 스마트키 A·B)과 `GetStartPose()`, 주차장 A·B 리셋(차량, 주차면), 사용자 차(Car_E, SmartKeyCar)에 외관 적용, `valetParking` 값으로 주차장 B 예약 테이블 ON/OFF (A는 항상 OFF) | FR-09, FR-10, FR-27, FR-28, FR-39 |
| `Parking/ParkingScenario.cs` | `ScenarioBase` 상속, 장면 목록: 발렛 → 스마트키. 장면에 나오지 않는 차 숨기기, 발렛은 버드뷰·스마트키는 고정 시점으로 시작, 스마트키 화면 켜고 끄기 | FR-11, FR-31, FR-57 |
| `Valet/GridMap.cs` | 주차장 격자 맵 (셀 2.5m), 위치 ↔ 셀 변환 | FR-26 |
| `Valet/ParkingSlot.cs` | 주차면 하나의 데이터: 사용 여부, 진입 준비 지점, 형상 파라미터로 후진 웨이포인트 계산 | FR-26, FR-40 |
| `Valet/ParkingSlotAllocator.cs` | 빈 주차면 배정, 주차면 리셋 | FR-26 |
| `Valet/ReservationTable.cs` | 발렛 옵션의 실체. 셀·시간 구간 예약·해제, 후진 구간 셀 포함. ON/OFF는 `ParkingArea`가 정함 (`ICarFeature` 아님) | FR-27, FR-28, FR-42 |
| `Valet/ValetAgent.cs` | 차량 에이전트(E·NPC 공통): 전역 웨이포인트 → 후진 웨이포인트를 `CarController`로 추종, 테이블 ON이면 예약 후 이동·OFF면 앞이 막히면 정지 | FR-26~28, FR-40, FR-41 |
| `RemoteParking/RemoteParking.cs` | `ICarFeature` 구현. 누르는 동안 3km/h 전진·후진, 떼면 0.2초 내 정지, 탑승 가능 판정, [탑승] 처리, 장애물 앞 정지와 후진 한계 | FR-31~34, FR-64~66 |
| `RemoteParking/SmartKeyUI.cs` | UI-06 스마트키 화면 ([전진] [후진] 길게 누르기, 탑승 가능할 때만 보이는 [탑승], 상태 문구) | FR-31, FR-64, FR-65 |
| `RemoteParking/ProximityStop.cs` | 차 앞·뒤 0.3m 안의 장애물 검사 (범퍼 바깥 상자 영역) | FR-33 |
| `Visualization/ParkingDrawer.cs` | 버드뷰 예약 셀 표시: 차량 색 타일과 "예약" 표지 | FR-29 |
| `Visualization/DoorSwingDrawer.cs` | 문 열림 반경, 겹치면 빨강·안 겹치면 초록, 탑승 가능 판정(`CanOpen`) | FR-34, FR-35 |
| `Visualization/VehicleStatusLabel.cs` | 발렛 차량 위 상태 라벨(이동 중, 대기 중, 후진 중, 주차 완료, 정지) | FR-30 |

### 5.4 테스트용 파일

| 파일 | 역할 |
| --- | --- |
| `Camera/_CameraSwitcherTester.cs` | 숫자 키로 시점 전환을 확인 |
| `Camera/Editor/_SceneViewCameraAligner.cs` | 씬 뷰 카메라를 앵커 위치에 맞추는 에디터 도구 |
| `Parking/_ParkingAreaTester.cs`, `_ParkingAreaTester.prefab` | 숫자 키로 주차 영역 진입, 차량 이동, 리셋, 스마트키 장면을 확인 |

---

## 6. 공통 파일 담당

| 묶음 | 포함 파일 | 다른 작업이 기다리는 정도 | 담당 |
| --- | --- | --- | --- |
| 영역 전환·조명·분할 카메라 | `ModeManager`, `AreaBase`, `AppMode` | 높음 (세 영역 모두 상속) | 김예은 |
| 차량 | `CarController`, `ICarFeature` | 높음 (주행·주차 모든 차량이 의존) | 김예은 |
| 옵션 데이터 | `CarConfig`, `FeaturePanel` | 높음 (모든 파일이 참조) | 강유나 |
| 카메라 | `CameraSwitcher`, `CameraAnchors` | 중간 | 강유나 |
| 화면 이동 | `ScreenHistory`, `NavigationBar`, `UIManager` | 중간 | 강유나 (`UIManager`의 시점 탭·라벨은 김예은) |
| 외관·쇼룸 | `ConfiguratorArea`, `CarAppearance`, `ExteriorPanel` | 중간 | 공동 |
| 시나리오 틀 | `ScenarioBase` | 중간 | 공동 |

**공통으로 맞춘 이름**

1. `CarConfig` 필드 6개: bodyColor, wheelIndex, nightVision, rearWheelSteering, valetParking, remoteParking
2. `AreaBase`의 메서드: `ResetArea(CarConfig)`, `OnEnter()`, `OnExit()`
3. `ICarFeature`의 메서드: `SetActive(bool)`, `ResetFeature()`
4. `CarController` 공개 함수(주행·주차 공통): `SetSpeed(float)`, `SetSteer(float)`, `SetRearSteer(float)`, `Brake(float)`, `Follow(Vector3[], float, bool)`, `ResetPose(Pose)`
5. `ScenarioBase`의 장면 제어: `StartFirstScene(CarConfig)`, `RestartCurrentScene(CarConfig)`, `GoToNextScene()`, `StopScenario()`

---

## 7. 레이어·태그 (ProjectSettings 공유)

| 종류 | 이름 | 용도 |
| --- | --- | --- |
| Layer | `Vehicle` | 차량 충돌·감지 |
| Layer | `Pedestrian` | 나이트 비전 감지 대상 |
| Layer | `Obstacle` | 문 열림 영역 판정, 발렛 앞 차량 감지 |
| Layer | `Visualization` | 원뿔·궤적·예약 셀·문 열림 영역. 운전석 카메라에서는 숨김 (스마트키 고정 시점에서는 표시) |
| Layer | `NightVisionOnly` | 열화상 카메라에만 보이는 오브젝트 |
| Layer | `Ground` | 차량이 따라가는 지면. 장애물 검사에서 제외 |
| Tag | `CourseA`, `CourseB` | 분할 화면 코스 구분 |

레이어·태그는 `ProjectSettings/TagManager.asset`에 저장되므로 한 사람이 추가하고 커밋한다.

---

## 8. 네이밍 규칙

| 대상 | 규칙 | 예 |
| --- | --- | --- |
| 스크립트·클래스 | PascalCase, 파일명 = 클래스명 (인터페이스는 `I` 접두) | `NightVisionSensor.cs`, `ICarFeature.cs` |
| 테스트·에디터 전용 스크립트 | 이름 앞에 `_` | `_ParkingAreaTester.cs`, `_FeaturePanelBuilder.cs` |
| 공개 메서드 | PascalCase | `ResetArea()` |
| private 필드 | `_camelCase` | `_detectRange` |
| Inspector 노출 필드 | `[SerializeField] private` + `_camelCase` | `[SerializeField] private float _ttcWarn = 4f;` |
| 프리팹 | PascalCase | `ParkingLot.prefab` |
| 머티리얼 | `M_` 접두사 | `M_TrailA`, `M_Cone_IR` |
| 네임스페이스 | 사용하지 않음 (모든 클래스가 전역) | - |
