# 프로젝트 파일 구조

> 기능 중심 차량 컨피규레이터 · 팀 YEYU · 기준: SRS v0.2 (MainScene + 영역 씬 Additive 로드)
> 공통 파일의 담당자는 아직 정하지 않았다. 팀 회의에서 정한 뒤 6장 표의 "담당" 열을 채운다.

---

## 1. 전체 트리

```
YEYU-Configurator/                     ← GitHub 저장소 루트 = Unity 프로젝트 루트
├── Assets/
│   ├── _Project/                      ← 우리 팀이 만든 파일은 전부 여기 (에셋 스토어 파일과 분리)
│   │   ├── Scenes/
│   │   │   ├── MainScene.unity
│   │   │   ├── ConfiguratorArea.unity
│   │   │   ├── DrivingArea.unity
│   │   │   └── ParkingArea.unity
│   │   │
│   │   ├── Scripts/
│   │   │   ├── Core/                  ← 앱 뼈대: 영역 전환, 옵션 데이터
│   │   │   │   ├── ModeManager.cs
│   │   │   │   ├── AreaBase.cs
│   │   │   │   ├── AppMode.cs
│   │   │   │   └── CarConfig.cs
│   │   │   │
│   │   │   ├── Vehicle/               ← 두 시나리오가 같이 쓰는 차량
│   │   │   │   ├── CarController.cs
│   │   │   │   ├── CarAppearance.cs
│   │   │   │   ├── CarFeatureSwitch.cs
│   │   │   │   └── ICarFeature.cs
│   │   │   │
│   │   │   ├── Camera/                ← 분할 화면, 시점 전환
│   │   │   │   ├── SplitScreenManager.cs
│   │   │   │   ├── CameraSwitcher.cs
│   │   │   │   └── ShowroomCamera.cs
│   │   │   │
│   │   │   ├── Scenario/              ← 시나리오 공통 틀
│   │   │   │   ├── ScenarioBase.cs
│   │   │   │   ├── ScenarioTrigger.cs
│   │   │   │   └── ScenarioResult.cs
│   │   │   │
│   │   │   ├── UI/                    ← MainScene 공통 Canvas
│   │   │   │   ├── UIManager.cs
│   │   │   │   ├── ExteriorPanel.cs
│   │   │   │   ├── FeaturePanel.cs
│   │   │   │   ├── ScenarioHUD.cs
│   │   │   │   └── ResultPanel.cs
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
│   │   │   │       ├── SensorConeDrawer.cs
│   │   │   │       └── TrajectoryDrawer.cs
│   │   │   │
│   │   │   ├── Parking/               ← 주차 파트 전용
│   │   │   │   ├── ParkingArea.cs
│   │   │   │   ├── ParkingScenario.cs
│   │   │   │   ├── Valet/
│   │   │   │   │   ├── GridMap.cs
│   │   │   │   │   ├── AStarPlanner.cs
│   │   │   │   │   ├── ReservationTable.cs
│   │   │   │   │   ├── ParkingSlotManager.cs
│   │   │   │   │   └── ValetAgent.cs
│   │   │   │   ├── RemoteParking/
│   │   │   │   │   ├── RemoteParking.cs
│   │   │   │   │   ├── SmartKeyUI.cs
│   │   │   │   │   └── ProximityStop.cs
│   │   │   │   └── Visualization/
│   │   │   │       ├── ReservationCellDrawer.cs
│   │   │   │       └── DoorSwingDrawer.cs
│   │   │   │
│   │   │   └── Utils/
│   │   │       ├── DebugDraw.cs
│   │   │       └── Units.cs
│   │   │
│   │   ├── Prefabs/
│   │   │   ├── Vehicle/
│   │   │   │   ├── Car.prefab
│   │   │   │   ├── Trims/              ← 트림별 그릴·범퍼 파츠
│   │   │   │   └── Wheels/             ← 휠 2종
│   │   │   ├── Driving/
│   │   │   │   ├── Pedestrian.prefab
│   │   │   │   ├── NightRoadCourse.prefab
│   │   │   │   └── AlleyCourse.prefab
│   │   │   ├── Parking/
│   │   │   │   ├── ParkingLot.prefab
│   │   │   │   ├── ParkedCar.prefab
│   │   │   │   └── ParkingSlot.prefab
│   │   │   └── UI/
│   │   │       └── MainCanvas.prefab
│   │   │
│   │   ├── Data/
│   │   │   ├── CarConfig.asset         ← 런타임 선택 옵션
│   │   │   └── FeatureDescriptions.asset ← 기능 설명 문장 (FR-06)
│   │   │
│   │   ├── Materials/
│   │   │   ├── Vehicle/                ← 차체 색상 4종
│   │   │   ├── Environment/
│   │   │   └── Visualization/          ← 원뿔·궤적·예약 셀 반투명 머티리얼
│   │   │
│   │   ├── Shaders/
│   │   │   └── NightVisionGray.shader  ← 적외선 화면 (필요할 때만)
│   │   │
│   │   ├── RenderTextures/
│   │   │   └── NightVisionRT.renderTexture
│   │   │
│   │   ├── Audio/
│   │   │   └── Warning.wav
│   │   │
│   │   └── UI/
│   │       ├── Icons/
│   │       └── Fonts/
│   │
│   ├── ThirdParty/                    ← 에셋 스토어 차량 모델 등 외부 에셋 (수정 금지)
│   └── TextMesh Pro/                  ← Unity가 자동 생성
│
├── Packages/                          ← 커밋 O
├── ProjectSettings/                   ← 커밋 O (레이어·태그·빌드 설정)
├── docs/
│   ├── WBS.md
│   ├── WBS.xlsx
│   ├── FOLDER_STRUCTURE.md            ← 이 문서
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

---

## 2. 폴더 구분 원칙

| 구분 | 폴더 | 누가 수정하나 |
|---|---|---|
| 공통 | `Core/`, `Vehicle/`, `Camera/`, `Scenario/`, `UI/`, `Utils/` | 담당자를 정한 뒤 그 사람만. 다른 사람은 수정 전에 알림 |
| 주행 전용 | `Scripts/Driving/`, `Prefabs/Driving/`, `DrivingArea.unity` | 김예은 |
| 주차 전용 | `Scripts/Parking/`, `Prefabs/Parking/`, `ParkingArea.unity` | 강유나 |
| 외부 에셋 | `ThirdParty/` | 수정 금지. 바꿀 땐 `_Project/Prefabs`로 복사해서 수정 |

이렇게 나누면 각자 자기 폴더만 고치는 동안에는 Git 충돌이 나지 않는다. 충돌 위험은 공통 폴더에만 남는다.

---

## 3. 씬 구성

| 씬 | 루트 오브젝트 | 들어가는 것 | 조명 |
|---|---|---|---|
| `MainScene` | `App` | `ModeManager`, `UIManager`, 공통 Canvas, `EventSystem`, `AudioListener` 1개 | 없음 |
| `ConfiguratorArea` | `ConfiguratorRoot` (X=0) | 쇼룸, 턴테이블, 미리보기 차량, `ShowroomCamera` | 밝은 실내 |
| `DrivingArea` | `DrivingRoot` (X=2000) | 코스 A·B, 차량 2대, 보행자, 분할 카메라 2대 | 야간 |
| `ParkingArea` | `ParkingRoot` (X=4000) | 주차장 A·B, 차량, 주차된 차, 분할 카메라 2대 | 주차장 조명 |

각 영역 씬의 루트 오브젝트에 해당 `AreaBase` 상속 스크립트(`DrivingArea.cs` 등)를 붙인다.

---

## 4. 핵심 연결 구조

```
ModeManager ──(SwitchTo)──▶ AreaBase
   │                           ├ ConfiguratorArea (Configurator 영역 처리)
   │                           ├ DrivingArea      → DrivingScenario
   │                           └ ParkingArea      → ParkingScenario
   │
   └─ CarConfig (외관 3개 + 기능 4개)
          │
          ▼
   Car.prefab
     ├ CarController        : 가속·조향·제동 입력 받기
     ├ CarAppearance        : CarConfig 외관 값 적용
     ├ CarFeatureSwitch     : CarConfig 기능 값으로 ICarFeature들을 켜고 끔
     └ (기능 컴포넌트)       : NightVisionSensor, RearWheelSteering, RemoteParking, ValetAgent
                              → 모두 ICarFeature 구현
```

- 기능 컴포넌트는 `ICarFeature` 인터페이스를 구현해 `CarFeatureSwitch`가 이름이 아니라 타입으로 켜고 끈다(NFR-06).
- 주행·주차 기능은 서로를 참조하지 않는다. 공통 파일만 참조한다.

---

## 5. 파일별 역할

### 5.1 공통 파일 (담당 미정)

| 파일 | 역할 | 관련 요구사항 | 담당 |
|---|---|---|---|
| `Core/ModeManager.cs` | 영역 씬 3개 Additive 로드, 영역 켜고 끄기, 활성 씬 지정 | FR-36~38 | |
| `Core/AreaBase.cs` | 영역 공통 부모: `Root`, `ResetArea(CarConfig)`, `OnEnter()`, `OnExit()` | FR-39 | |
| `Core/AppMode.cs` | `enum AppMode { Configurator, Driving, Parking }` | FR-37 | |
| `Core/CarConfig.cs` | ScriptableObject: 외관 3개 + 기능 4개, 비교용 복사본 생성 | FR-01~11 | |
| `Vehicle/CarController.cs` | `WheelCollider` 구동, 스크립트로 가속·조향·제동·뒷바퀴 조향각 입력 | FR-10, NFR-02 | |
| `Vehicle/CarAppearance.cs` | 색상·트림 파츠·휠 교체 | FR-01~03 | |
| `Vehicle/CarFeatureSwitch.cs` | `CarConfig`대로 기능 컴포넌트 `enabled` 설정 | FR-08, FR-09 | |
| `Vehicle/ICarFeature.cs` | 기능 공통 인터페이스 (`FeatureId`, `SetActive(bool)`, `ResetFeature()`) | NFR-06 | |
| `Camera/SplitScreenManager.cs` | 카메라 2대 좌우 분할, "옵션 미적용/적용" 라벨 | FR-11 | |
| `Camera/CameraSwitcher.cs` | 운전석 ↔ 버드뷰, 양쪽 동시 전환 | FR-12 | |
| `Camera/ShowroomCamera.cs` | 쇼룸 마우스 드래그 회전 | FR-04 | |
| `Scenario/ScenarioBase.cs` | 시나리오 공통 흐름: 준비 → 동시 출발 → 진행 → 종료 | FR-11, NFR-02 | |
| `Scenario/ScenarioTrigger.cs` | 구간 트리거 콜라이더 → 이벤트 발생 | FR-17, FR-21 | |
| `Scenario/ScenarioResult.cs` | 정지 거리, TTC, 회전 반경, 유턴 횟수, 충돌 여부 기록 | FR-13 | |
| `UI/UIManager.cs` | 모드별 패널 전환 | FR-37 | |
| `UI/ExteriorPanel.cs` | UI-01 외관 선택 버튼 | FR-01~03 | |
| `UI/FeaturePanel.cs` | UI-02 기능 체크박스, 설명 문장, 시작 버튼 | FR-05, FR-06 | |
| `UI/ScenarioHUD.cs` | UI-03·04 공통: 시점 버튼, 라벨, 수치 표시 칸 | FR-12, FR-19 | |
| `UI/ResultPanel.cs` | UI-05 결과 패널 (Could) | FR-13 | |
| `Utils/DebugDraw.cs` | 원뿔·선·점을 그리는 공통 도구 (LineRenderer, Mesh) | FR-18, FR-23, FR-29 | |
| `Utils/Units.cs` | km/h ↔ m/s 변환 등 | - | |

### 5.2 주행 파트 (김예은)

| 파일 | 역할 | 관련 요구사항 |
|---|---|---|
| `Driving/DrivingArea.cs` | `AreaBase` 상속, 코스 A·B 리셋, 차량 2대에 설정 적용 | FR-08, FR-39 |
| `Driving/DrivingScenario.cs` | `ScenarioBase` 상속, 야간 도로 → 골목 유턴 진행 | FR-11 |
| `Driving/VirtualDriver.cs` | 미적용 차량 운전자: 보이는 순간 + 1초 후 급제동, 3점 턴 | FR-17, FR-21 |
| `Driving/Pedestrian.cs` | 트리거 시 도로로 걷기, 초기 위치 복원 | FR-17 |
| `NightVision/NightVisionSensor.cs` | 120m 감지, 거리·상대속도·TTC, 경고·감속 요청 | FR-15, FR-17 |
| `NightVision/NightVisionDisplay.cs` | 적외선 카메라 → RenderTexture → 클러스터, 노란 박스 | FR-16 |
| `RearWheelSteering/RearWheelSteering.cs` | 30km/h 미만 역위상 10도, 60km/h 이상 동위상 2도 | FR-20, FR-24 |
| `RearWheelSteering/TurningRadiusCalculator.cs` | 자전거 모델 회전 반경, ICR 계산 | FR-22 |
| `Visualization/SensorConeDrawer.cs` | 헤드램프·적외선 원뿔 | FR-18 |
| `Visualization/TrajectoryDrawer.cs` | 회전 궤적 선, ICR 점, 반경 라벨 | FR-23 |

### 5.3 주차 파트 (강유나)

| 파일 | 역할 | 관련 요구사항 |
|---|---|---|
| `Parking/ParkingArea.cs` | `AreaBase` 상속, 주차장 A·B 리셋 | FR-09, FR-39 |
| `Parking/ParkingScenario.cs` | `ScenarioBase` 상속, 발렛 → 원격 주차 진행 | FR-11 |
| `Valet/GridMap.cs` | 주차장 격자 맵 | FR-26 |
| `Valet/AStarPlanner.cs` | A* 경로 계획 | FR-26 |
| `Valet/ReservationTable.cs` | 셀·시간 구간 예약, 해제 | FR-27, FR-28 |
| `Valet/ParkingSlotManager.cs` | 빈 주차면 배정 | FR-26 |
| `Valet/ValetAgent.cs` | 차량별 경로 추종, 예약 요청·대기 | FR-27, FR-28 |
| `RemoteParking/RemoteParking.cs` | 누르는 동안 3km/h 이동, 떼면 정지 | FR-31, FR-32 |
| `RemoteParking/SmartKeyUI.cs` | UI-06 스마트키 화면 | FR-31 |
| `RemoteParking/ProximityStop.cs` | 0.3m 장애물 정지 | FR-33 |
| `Visualization/ReservationCellDrawer.cs` | 예약 셀 차량 색 표시 | FR-29 |
| `Visualization/DoorSwingDrawer.cs` | 문 열림 반경, 공간 부족 빨간색 | FR-34, FR-35 |

> 주차 파트 파일 구성은 강유나 검토 후 바뀔 수 있다.

---

## 6. 공통 파일 담당 정하기

공통 파일은 10/7부터 두 파트가 모두 기다리는 선행 작업이라, 10/6 회의에서 담당을 정해야 한다. 같은 묶음은 한 사람이 맡아야 인터페이스가 흔들리지 않는다.

| 묶음 | 포함 파일 | 다른 작업이 기다리는 정도 | 담당 |
|---|---|---|---|
| 영역 전환 | `ModeManager`, `AreaBase`, `AppMode` | 높음 (두 영역 모두 상속) | |
| 옵션 데이터 | `CarConfig` | 높음 (모든 파일이 참조) | |
| 차량 | `CarController`, `CarFeatureSwitch`, `ICarFeature` | 높음 (기능 4종이 모두 의존) | |
| 외관 | `CarAppearance` | 중간 | |
| 카메라 | `SplitScreenManager`, `CameraSwitcher`, `ShowroomCamera` | 중간 | |
| 시나리오 틀 | `ScenarioBase`, `ScenarioTrigger`, `ScenarioResult` | 중간 | |
| UI | `UIManager`, `ExteriorPanel`, `FeaturePanel`, `ScenarioHUD`, `ResultPanel` | 낮음 | |
| 공용 도구 | `DebugDraw`, `Units` | 낮음 | |

**먼저 합의할 것 (코드 없이 이름만)**

1. `CarConfig` 필드 이름 7개 (SRS 6장 그대로 쓸지)
2. `AreaBase`의 메서드 이름: `ResetArea(CarConfig)`, `OnEnter()`, `OnExit()`
3. `ICarFeature`의 메서드 이름: `SetActive(bool)`, `ResetFeature()`
4. `CarController`의 입력 함수 이름: `SetThrottle(float)`, `SetSteer(float)`, `SetBrake(float)`, `SetRearSteer(float)`

이 네 가지만 정해 두면 공통 파일이 완성되기 전에도 각자 기능 코드를 작성할 수 있다.

---

## 7. 레이어·태그 (ProjectSettings 공유)

| 종류 | 이름 | 용도 |
|---|---|---|
| Layer | `Vehicle` | 차량 충돌·감지 |
| Layer | `Pedestrian` | 나이트 비전 감지 대상 |
| Layer | `Obstacle` | 원격 주차 근접 정지 |
| Layer | `Visualization` | 원뿔·궤적 등. 운전석 카메라에서는 숨김 |
| Layer | `NightVisionOnly` | 적외선 카메라에만 보이는 오브젝트 |
| Tag | `CourseA`, `CourseB` | 분할 화면 코스 구분 |

레이어·태그는 `ProjectSettings/TagManager.asset`에 저장되므로 한 사람이 처음에 한 번에 추가하고 커밋한다.

---

## 8. 네이밍 규칙

| 대상 | 규칙 | 예 |
|---|---|---|
| 스크립트·클래스 | PascalCase, 파일명 = 클래스명 | `NightVisionSensor.cs` |
| 공개 메서드 | PascalCase | `ResetArea()` |
| private 필드 | `_camelCase` | `_detectRange` |
| Inspector 노출 필드 | `[SerializeField] private` + `_camelCase` | `[SerializeField] private float _ttcWarn = 4f;` |
| 프리팹 | PascalCase | `NightRoadCourse.prefab` |
| 머티리얼 | `M_` 접두사 | `M_Body_Red`, `M_Cone_IR` |
| 네임스페이스 | `Yeyu.<폴더>` | `Yeyu.Core`, `Yeyu.Driving` |
