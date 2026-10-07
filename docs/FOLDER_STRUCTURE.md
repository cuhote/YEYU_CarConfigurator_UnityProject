# 프로젝트 파일 구조

> 기능 중심 차량 컨피규레이터 · 팀 YEYU · 기준: SRS v0.7 (MainScene + 영역 씬 Additive 로드) · 클래스 다이어그램 1차 피드백 반영
클래스 단위 설계(속성·메서드·관계)는 `CLASS_DESIGN.md`, 장면별 수치는 `SCENARIO.md`를 본다.
공통 파일의 담당은 WBS 배정을 6장에 옮겨 적었다. 19:00 회의에서 확정한다.
> 

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
│   │   │   │   ├── CarFeatureSwitch.cs
│   │   │   │   └── ICarFeature.cs
│   │   │   │
│   │   │   ├── Camera/                ← 시점 전환 (분할 카메라 켜고 끄기는 AreaBase)
│   │   │   │   ├── CameraSwitcher.cs
│   │   │   │   └── ShowroomCamera.cs
│   │   │   │
│   │   │   ├── Scenario/              ← 시나리오 공통 틀 (장면 순서, 재시작)
│   │   │   │   ├── ScenarioBase.cs
│   │   │   │   └── ScenarioTrigger.cs
│   │   │   │
│   │   │   ├── UI/                    ← MainScene 공통 Canvas
│   │   │   │   ├── UIManager.cs
│   │   │   │   ├── ExteriorPanel.cs
│   │   │   │   ├── FeaturePanel.cs
│   │   │   │   ├── ScenarioHUD.cs
│   │   │   │   ├── OptionCardPanel.cs
│   │   │   │   └── NavigationBar.cs
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
│   │   │   ├── Parking/               ← 주차 파트 전용
│   │   │   │   ├── ParkingArea.cs
│   │   │   │   ├── ParkingScenario.cs
│   │   │   │   ├── Valet/
│   │   │   │   │   ├── GridMap.cs
│   │   │   │   │   ├── ParkingSlotAllocator.cs
│   │   │   │   │   ├── ReservationTable.cs
│   │   │   │   │   └── ValetAgent.cs
│   │   │   │   ├── RemoteParking/
│   │   │   │   │   ├── RemoteParking.cs
│   │   │   │   │   ├── SmartKeyUI.cs
│   │   │   │   │   └── ProximityStop.cs
│   │   │   │   └── Visualization/
│   │   │   │       ├── ParkingDrawer.cs
│   │   │   │       └── DoorSwingDrawer.cs
│   │   │   │
│   │   │   └── Utils/
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
│   │   │   │   ├── ParkingSlot.prefab
│   │   │   │   └── NarrowSlotSet.prefab  ← 스마트키 장면의 좁은 주차면 + 양옆 차량
│   │   │   └── UI/
│   │   │       └── MainCanvas.prefab
│   │   │
│   │   ├── Data/
│   │   │   └── CarConfig.asset         ← 선택 옵션 기본값 (실행 중에는 복사본 사용, 유일한 ScriptableObject)
│   │   │
│   │   ├── Materials/
│   │   │   ├── Vehicle/                ← 차체 색상 4종
│   │   │   ├── Environment/
│   │   │   └── Visualization/          ← 원뿔·궤적·예약 셀·문 열림 영역 반투명 머티리얼
│   │   │
│   │   ├── Shaders/
│   │   │   └── NightVisionGray.shader  ← 열화상 흑백 화면 (필요할 때만)
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
│   ├── SCENARIO.md                    ← 장면 단위 시나리오, 수치 근거
│   ├── FOLDER_STRUCTURE.md            ← 이 문서
│   ├── CLASS_DESIGN.md                ← 클래스 다이어그램 (Mermaid)
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
| --- | --- | --- |
| 공통 | `Core/`, `Vehicle/`, `Camera/`, `Scenario/`, `UI/`, `Utils/` | 6장에서 정한 담당자만. 다른 사람은 수정 전에 알림 |
| 주행 전용 | `Scripts/Driving/`, `Prefabs/Driving/`, `DrivingArea.unity` | 김예은 |
| 주차 전용 | `Scripts/Parking/`, `Prefabs/Parking/`, `ParkingArea.unity` | 강유나 |
| 외부 에셋 | `ThirdParty/` | 수정 금지. 바꿀 땐 `_Project/Prefabs`로 복사해서 수정 |

이렇게 나누면 각자 자기 폴더만 고치는 동안에는 Git 충돌이 나지 않는다. 충돌 위험은 공통 폴더에만 남는다.

---

## 3. 씬 구성

| 씬 | 루트 오브젝트 | 들어가는 것 | 조명 |
| --- | --- | --- | --- |
| `MainScene` | `App` | `ModeManager`, `UIManager`, `ScreenHistory`, 공통 Canvas, `EventSystem`, `AudioListener` 1개 | 없음 |
| `ConfiguratorArea` | `ConfiguratorRoot` (X=0) | 쇼룸, 턴테이블, 미리보기 차량, `ShowroomCamera`, `ConfiguratorArea` | 밝은 실내 |
| `DrivingArea` | `DrivingRoot` (X=2000) | 코스 A(X=2000)·B(X=3000), 차량 2대, 보행자, 골목, 분할 카메라 2대 | 야간 |
| `ParkingArea` | `ParkingRoot` (X=4000) | 주차장 A(X=4000)·B(X=5000), 차량 E·NPC, 주차된 차, 좁은 주차면, 전역·후진 웨이포인트, 분할 카메라 2대 | 주차장 조명 |

각 영역 씬의 루트 오브젝트에 해당 `AreaBase` 상속 스크립트(`ConfiguratorArea.cs`, `DrivingArea.cs`, `ParkingArea.cs`)를 붙인다. 조명 열은 각 `AreaBase`의 ambient 색 필드에 넣고 `OnEnter()`에서 적용한다(FR-38).

---

## 4. 핵심 연결 구조

```
ModeManager ──(SwitchTo)──▶ AreaBase  ── OnEnter(): 영역 조명 적용 + 분할 카메라 2대 켜기
   │                           ├ ConfiguratorArea (쇼룸, 미리보기 차량)
   │                           ├ DrivingArea      → DrivingScenario (ScenarioBase)
   │                           └ ParkingArea      → ParkingScenario (ScenarioBase)
   │
   ├─ ScreenHistory : 거쳐 온 화면 기록 → NavigationBar의 뒤로가기·홈 (FR-53, FR-54)
   └─ CarConfig (외관 3개 + 기능 4개) ◀── FeaturePanel 체크박스 · OptionCardPanel 토글 (FR-51)
          │
          ▼
   Car.prefab (주행·주차 공통)
     ├ CarController        : 유일한 구동부. 자전거 모델(앞·뒷바퀴 조향), 웨이포인트 추종, ResetPose
     ├ CarAppearance        : CarConfig 외관 값 적용
     ├ CarFeatureSwitch     : CarConfig 기능 값으로 차량에 붙은 ICarFeature를 켜고 끔
     └ (기능 컴포넌트)       : NightVisionSensor, RearWheelSteering, RemoteParking → ICarFeature

운전자 (두 차량이 같은 방식으로 운전, 오른쪽만 기능 ON):
  주행: VirtualDriver ──▶ CarController      (NightVisionSensor → VirtualDriver.RequestBrake)
  주차: ValetAgent    ──▶ CarController      (전역 웨이포인트 → 후진 웨이포인트)
        ReservationTable(ICarFeature) : 발렛 = 예약 테이블 ON/OFF. 주차장 A는 항상 OFF, B는 옵션 값

ScenarioBase : 장면 목록 → 꺼진 옵션 장면 건너뛰기(FR-62) → 장면 끝나면 0.5초 페이드 후 다음 장면(FR-57)
               토글 변경 시 현재 장면 초기화 후 재시작(FR-48), 현재 장면 옵션이 꺼지면 다른 장면 시작(FR-63)
```

- 기능 컴포넌트는 `ICarFeature`를 구현해 타입으로 켜고 끈다(NFR-06). 차량에 붙은 것은 `CarFeatureSwitch`가, 주차장에 붙은 `ReservationTable`은 `ParkingArea`가 켜고 끈다.
- 측정·결과 기록은 범위에서 제외했다(SRS v0.7). 화면 숫자(거리, 충돌 시간, 회전 반경)는 계산해서 표시만 한다.
- 주행·주차 기능은 서로를 참조하지 않는다. 공통 파일만 참조한다.

---

## 5. 파일별 역할

### 5.1 공통 파일

| 파일 | 역할 | 관련 요구사항 | 담당 |
| --- | --- | --- | --- |
| `Core/ModeManager.cs` | 영역 씬 3개 Additive 로드, 영역 켜고 끄기, 활성 씬 지정, 일시정지 해제 후 전환 | FR-36~38 | 김예은 |
| `Core/AreaBase.cs` | 영역 공통 부모: `Root`, `ResetArea(CarConfig)`, `OnEnter()`, `OnExit()`. `OnEnter()`에서 영역별 ambient 조명 적용, 영역 카메라 2대를 왼쪽·오른쪽 Viewport로 나눠 켜고 끔 | FR-11, FR-38, FR-39 | 김예은 |
| `Core/AppMode.cs` | `enum AppMode { Configurator, Driving, Parking }` | FR-37 | 김예은 |
| `Core/CarConfig.cs` | ScriptableObject: 외관 3개 + 기능 4개(초기값 모두 선택), 비교용 복사본 생성 | FR-01~11, FR-51 | 강유나 |
| `Core/ConfiguratorArea.cs` | `AreaBase` 상속, 옵션 선택 영역: 미리보기 차량에 외관 적용, 쇼룸 카메라 켜기 | FR-01~04, FR-59 | 공동 |
| `Core/ScreenHistory.cs` | 화면 이동 기록(외관 선택 → 기능 선택 → 장면). 건너뛴 장면은 쌓지 않음, 홈이면 비움 | FR-53, FR-54 | 강유나 |
| `Vehicle/CarController.cs` | 모든 차량의 유일한 구동부: 자전거 모델로 위치·방향 갱신(앞·뒷바퀴 조향), 속도·제동, 웨이포인트 추종(`Follow`), 위치·속도·조향각 초기화(`ResetPose`) | FR-39, FR-41, FR-43, NFR-02 | 김예은 |
| `Vehicle/CarAppearance.cs` | 색상·트림 파츠·휠 교체 | FR-01~03, FR-10 | 공동 |
| `Vehicle/CarFeatureSwitch.cs` | `CarConfig`대로 차량에 붙은 기능 컴포넌트 켜고 끄기 | FR-08, FR-09 | 김예은 |
| `Vehicle/ICarFeature.cs` | 기능 공통 인터페이스 (`FeatureId`, `SetActive(bool)`, `ResetFeature()`) | NFR-06 | 김예은 |
| `Camera/CameraSwitcher.cs` | AreaBase가 켠 카메라 2대의 시점 전환: 운전석 ↔ 버드뷰 동시 전환, 스마트키 장면 고정 시점, [탑승] 시 오른쪽만 운전석 시점 | FR-12, FR-31, FR-65 | 강유나 |
| `Camera/ShowroomCamera.cs` | 쇼룸 마우스 드래그 회전 | FR-04 | 공동 |
| `Scenario/ScenarioBase.cs` | 시나리오 공통 흐름: 장면 목록, 꺼진 장면 건너뛰기, 동시 출발, 자동 전환(0.5초 페이드), 토글 시 재시작 | FR-11, FR-48, FR-57, FR-62, FR-63, NFR-02 | 공동 |
| `Scenario/ScenarioTrigger.cs` | 구간 트리거 콜라이더 → 이벤트 발생 | FR-17, FR-21 | 공동 |
| `UI/UIManager.cs` | 모드별 패널 전환 | FR-37 | 강유나 |
| `UI/ExteriorPanel.cs` | UI-01 외관 선택 버튼, 선택 요약 카드 | FR-01~03, FR-58 | 공동 |
| `UI/FeaturePanel.cs` | UI-02 주행·주차 탭, 체크박스, 설명 문장 4개(`TMP_Text` 필드에 Inspector로 입력), 탭별 시작 버튼 | FR-05, FR-06, FR-60, FR-61 | 강유나 |
| `UI/ScenarioHUD.cs` | UI-03·04 공통: 시점 버튼, 좌우 라벨("옵션 미적용/선택 옵션 적용"), 수치 표시 칸, 재시작 안내 문구, [일시 정지] | FR-11, FR-12, FR-14, FR-19, FR-52 | 김예은 |
| `UI/OptionCardPanel.cs` | 오른쪽 위 옵션 카드 2개(ON/OFF 토글), 마지막 켜진 토글 잠금, `CarConfig` 연동 | FR-47~51 | 김예은 |
| `UI/NavigationBar.cs` | 왼쪽 위 [뒤로가기] [홈] 아이콘 버튼 | FR-53~56 | 강유나 |
| `Utils/Units.cs` | km/h ↔ m/s 변환 등 | - | 김예은 |

### 5.2 주행 파트 (김예은)

| 파일 | 역할 | 관련 요구사항 |
| --- | --- | --- |
| `Driving/DrivingArea.cs` | `AreaBase` 상속, 코스 A·B 리셋, 차량 2대에 설정 적용 | FR-08, FR-39 |
| `Driving/DrivingScenario.cs` | `ScenarioBase` 상속, 장면 목록: 나이트 비전 → 후륜 조향. 운전자 2명(A·B)을 같은 타이머로 출발 | FR-11, FR-57 |
| `Driving/VirtualDriver.cs` | 두 차량 공통 운전자: 경로·속도 프로파일 주행, 헤드램프 50m 인지 → 반응 1.5초 → 6.0m/s² 급제동, 센서의 감속 요청 수행, 골목 유턴 웨이포인트(왼쪽 3회·오른쪽 1회) | FR-11, FR-17, FR-21 |
| `Driving/Pedestrian.cs` | 시나리오 타이머로 도로를 향해 걷기, 초기 위치 복원 | FR-17 |
| `NightVision/NightVisionSensor.cs` | 120m 감지, 거리·상대속도·TTC, 4초 경고·2.5초 감속(5.0m/s²)을 운전자에게 요청 | FR-15, FR-17 |
| `NightVision/NightVisionDisplay.cs` | 열화상 카메라 → RenderTexture → 클러스터, 노란 박스, 거리·충돌 시간 | FR-16, FR-19 |
| `RearWheelSteering/RearWheelSteering.cs` | 30km/h 미만 역위상 10도, 60km/h 이상 동위상 2도 | FR-20, FR-24 |
| `RearWheelSteering/TurningRadiusCalculator.cs` | 자전거 모델 회전 반경 R = L ÷ (tan δf − tan δr), ICR 계산 | FR-22 |
| `Visualization/DrivingDrawer.cs` | 주행 영역 그리기 전부: 헤드램프(50m)·열화상(120m) 원뿔, 회전 궤적 선, ICR 점, 반경 라벨 | FR-18, FR-23 |

### 5.3 주차 파트 (강유나)

| 파일 | 역할 | 관련 요구사항 |
| --- | --- | --- |
| `Parking/ParkingArea.cs` | `AreaBase` 상속, 주차장 A·B 리셋(차량, 예약 테이블, 주차면, 탑승 상태) | FR-09, FR-39 |
| `Parking/ParkingScenario.cs` | `ScenarioBase` 상속, 장면 목록: 발렛 → 스마트키 | FR-11, FR-57 |
| `Valet/GridMap.cs` | 주차장 격자 맵 (셀 2.5m, 20×12), 위치 ↔ 셀 변환 | FR-26 |
| `Valet/ParkingSlotAllocator.cs` | 빈 주차면 배정, 주차면별 진입 준비 지점과 후진 웨이포인트 | FR-26, FR-40 |
| `Valet/ReservationTable.cs` | `ICarFeature` 구현 = 발렛 옵션. 셀·시간 구간 예약·해제, 후진 구간 셀 포함. 주차장 A는 항상 OFF | FR-27, FR-28, FR-42 |
| `Valet/ValetAgent.cs` | 차량 에이전트(E·NPC 공통): 전역 웨이포인트 → 후진 웨이포인트를 `CarController`로 추종, 테이블 ON이면 예약 후 이동·OFF면 앞이 막히면 정지, 상태 표시 | FR-26~28, FR-30, FR-40, FR-41 |
| `RemoteParking/RemoteParking.cs` | `ICarFeature` 구현. 누르는 동안 3km/h 전진·후진, 떼면 0.2초 내 정지, 탑승 가능 판정, [탑승] 처리 | FR-31, FR-32, FR-34, FR-64~66 |
| `RemoteParking/SmartKeyUI.cs` | UI-06 스마트키 화면 ([전진] [후진] [탑승], 상태 문구) | FR-31, FR-64 |
| `RemoteParking/ProximityStop.cs` | 전후방 0.3m 장애물 정지 | FR-33 |
| `Visualization/ParkingDrawer.cs` | 주차 영역 그리기: 예약 셀 차량 색, 후진 웨이포인트와 앞바퀴 조향각 | FR-29, FR-44 |
| `Visualization/DoorSwingDrawer.cs` | 문 열림 반경, 겹치면 빨강·안 겹치면 초록, 탑승 가능 판정(`CanOpen`) | FR-34, FR-35 |

> 주차 파트는 클래스 1차 피드백(5~9번)에 따라 경로 공급부·A*·구동부 분리·궤적 계산·Pure Pursuit 클래스를 없애고 웨이포인트 추종으로 단순화했다. 강유나 검토 후 바뀔 수 있다.
> 

---

## 6. 공통 파일 담당

공통 파일은 두 파트가 모두 기다리는 선행 작업이다. 같은 묶음은 한 사람이 맡아야 인터페이스가 흔들리지 않는다. 아래 담당은 WBS 배정을 옮겨 적은 것이며 19:00 회의에서 확정한다.

| 묶음 | 포함 파일 | 다른 작업이 기다리는 정도 | 담당 | WBS |
| --- | --- | --- | --- | --- |
| 영역 전환·조명·분할 카메라 | `ModeManager`, `AreaBase`, `AppMode` | 높음 (세 영역 모두 상속) | 김예은 | B.1 |
| 차량 | `CarController`, `CarFeatureSwitch`, `ICarFeature` | 높음 (주행·주차 모든 차량이 의존) | 김예은 | B.2 |
| 옵션 데이터 | `CarConfig`, `FeaturePanel` | 높음 (모든 파일이 참조) | 강유나 | B.4 |
| 카메라 | `CameraSwitcher` | 중간 | 강유나 | B.5 |
| 화면 이동 | `ScreenHistory`, `NavigationBar`, `UIManager` | 중간 | 강유나 | B.6 |
| 외관·쇼룸 | `ConfiguratorArea`, `CarAppearance`, `ExteriorPanel`, `ShowroomCamera` | 중간 | 공동 | E.0 |
| 시나리오 UI | `OptionCardPanel`, `ScenarioHUD` | 중간 | 김예은 | E.10 |
| 시나리오 틀 | `ScenarioBase`, `ScenarioTrigger` | 중간 | 공동 | E.1·E.2·E.11 |
| 공용 도구 | `Units` | 낮음 | 김예은 (제안) | C.5 |

**먼저 합의할 것 (코드 없이 이름만)**

1. `CarConfig` 필드 이름 7개: SRS 6장 그대로(bodyColor, trimIndex, wheelIndex, nightVision, rearWheelSteering, valetParking, remoteParking)
2. `AreaBase`의 메서드 이름: `ResetArea(CarConfig)`, `OnEnter()`, `OnExit()`
3. `ICarFeature`의 메서드 이름: `SetActive(bool)`, `ResetFeature()`
4. `CarController` 공개 함수 이름(주행·주차 공통): `SetSpeed(float)`, `SetSteer(float)`, `SetRearSteer(float)`, `Brake(float)`, `Follow(Vector3[], float, bool)`, `ResetPose(Pose)`
5. `ScenarioBase`의 장면 제어 이름: `StartFirstScene()`, `RestartCurrentScene()`, `GoToNextScene()`

이 다섯 가지만 정해 두면 공통 파일이 완성되기 전에도 각자 기능 코드를 작성할 수 있다.

---

## 7. 레이어·태그 (ProjectSettings 공유)

| 종류 | 이름 | 용도 |
| --- | --- | --- |
| Layer | `Vehicle` | 차량 충돌·감지 |
| Layer | `Pedestrian` | 나이트 비전 감지 대상 |
| Layer | `Obstacle` | 원격 주차 근접 정지, 발렛 앞 차량 감지 |
| Layer | `Visualization` | 원뿔·궤적·예약 셀·웨이포인트·문 열림 영역. 운전석 카메라에서는 숨김 (스마트키 고정 시점에서는 표시) |
| Layer | `NightVisionOnly` | 열화상 카메라에만 보이는 오브젝트 |
| Tag | `CourseA`, `CourseB` | 분할 화면 코스 구분 |

레이어·태그는 `ProjectSettings/TagManager.asset`에 저장되므로 한 사람이 처음에 한 번에 추가하고 커밋한다.

---

## 8. 네이밍 규칙

| 대상 | 규칙 | 예 |
| --- | --- | --- |
| 스크립트·클래스 | PascalCase, 파일명 = 클래스명 (인터페이스는 `I` 접두) | `NightVisionSensor.cs`, `ICarFeature.cs` |
| 공개 메서드 | PascalCase | `ResetArea()` |
| private 필드 | `_camelCase` | `_detectRange` |
| Inspector 노출 필드 | `[SerializeField] private` + `_camelCase` | `[SerializeField] private float _ttcWarn = 4f;` |
| 프리팹 | PascalCase | `NightRoadCourse.prefab` |
| 머티리얼 | `M_` 접두사 | `M_Body_Red`, `M_Cone_IR` |
| 네임스페이스 | `Yeyu.<폴더>` | `Yeyu.Core`, `Yeyu.Driving` |
