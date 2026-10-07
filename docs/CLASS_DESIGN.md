# 클래스 설계서

> 팀 YEYU · 작성 김예은 · 2026.10.07 · 기준: SRS v0.7, `FOLDER_STRUCTURE.md` 5장 · 클래스 다이어그램 1차 피드백 반영본
파일 하나 = 클래스 하나. 이름은 `CONVENTIONS.md`(private 필드 `_camelCase`, 네임스페이스 `Yeyu.<폴더>`)를 따른다.
> 

## 1차 피드백 반영

| 번호 | 피드백 | 반영 |
| --- | --- | --- |
| 0 | FeaturePanel의 ScriptableObject 과함 | `FeatureDescriptions` 삭제. `FeaturePanel._descriptionTexts: TMP_Text[4]`에 Inspector로 입력 |
| 1 | 오른쪽 차량 운전자 없음 | 두 차량 모두 `VirtualDriver`가 운전. 오른쪽만 기능 컴포넌트 ON, 센서는 `RequestBrake()`로 운전자에게 감속 요청 |
| 2 | 조명 전환 책임 없음 | `AreaBase.ApplyLighting()` 추가, `OnEnter()`에서 영역별 ambient 적용 |
| 3 | 측정 책임 없음 | 측정·결과 기록 자체를 범위에서 제외: `ScenarioResult`, `ResultPanel` 삭제 (SRS v0.7 FR-13·UI-05 삭제) |
| 4 | 차량 원위치 불가 | `CarController.ResetPose(Pose)` 추가 (위치·속도·조향각 초기화) |
| 5 | ValetAgent가 ICarFeature 구현 | `ReservationTable`이 `ICarFeature` 구현, `ValetAgent`는 일반 클래스 |
| 6 | IPathProvider + Waypoint + AStar | 셋 다 삭제, 웨이포인트는 `ValetAgent._route`로 흡수 (SRS FR-46·NFR-10 삭제) |
| 7 | IVehicleDriver + BicycleModelDriver 중복 | 둘 다 삭제, 구동은 `CarController` 하나(자전거 모델)로 통일 (SRS FR-43 수정, NFR-11 삭제) |
| 8 | ReverseParkingPlanner + PurePursuitController | 둘 다 삭제, 후진은 미리 찍은 `_reverseRoute` 웨이포인트 추종 (SRS FR-40·41 수정, FR-45 삭제) |
| 9 | Drawer 6개 + DebugDraw | `DrivingDrawer`, `ParkingDrawer`로 통합, `DoorSwingDrawer`만 유지, `DebugDraw` 삭제 |
| 10 | AreaBase, ScenarioBase, ICarFeature 좋음 | 유지 |

결과: 클래스 54개 → 41개.

## 표기법

| 표기 | 의미 |
| --- | --- |
| `+` `-` `#` | public, private, protected |
| 밑줄 (Mermaid `$`) | static |
| 기울임 (Mermaid `*`) | 추상 메서드 |
| `«interface»` `«abstract»` `«enumeration»` `«ScriptableObject»` `«static»` | 스테레오 타입 |
| `[*]` `[0..1]` `[2]` | 다중성: 개수 미정 / 0개 또는 1개 / 고정 개수 |
| `<\|--` / `<\|..` | 상속 / 실체화(인터페이스 구현) |
| `o--` / `*--` | 집합(참조만, 씬에 따로 존재) / 합성(같은 오브젝트의 일부) |
| `-->` / `..>` | 연관(필드로 보유) / 의존(인자로 받거나 잠깐 호출) |

## 도면 1. 뼈대 — Core · Vehicle (공통)

```mermaid
classDiagram
  direction TB
  class ModeManager {
    +Instance: ModeManager$
    +IsReady: bool
    +CurrentMode: AppMode
    +Config: CarConfig
    -_configAsset: CarConfig
    -_areas: AreaBase[3]
    +SwitchTo(AppMode) void
    +StartDriving() void
    +StartParking() void
    -LoadAreasAsync() IEnumerator
  }
  class AppMode {
    <<enumeration>>
    Configurator
    Driving
    Parking
  }
  class AreaBase {
    <<abstract>>
    -_mode: AppMode
    -_ambientColor: Color
    #_leftCam: Camera
    #_rightCam: Camera
    +Root: GameObject
    +LeftCam: Camera
    +RightCam: Camera
    +ResetArea(CarConfig)* void
    +OnEnter() void
    +OnExit() void
    #ApplyLighting() void
    #SetSplitCameras(bool) void
  }
  class ConfiguratorArea {
    -_previewCar: CarAppearance
    +ResetArea(CarConfig) void
  }
  class DrivingArea {
    +ResetArea(CarConfig) void
  }
  class ParkingArea {
    +ResetArea(CarConfig) void
  }
  class CarConfig {
    <<ScriptableObject>>
    -_bodyColor: int
    -_trimIndex: int
    -_wheelIndex: int
    -_nightVision: bool = true
    -_rearWheelSteering: bool = true
    -_valetParking: bool = true
    -_remoteParking: bool = true
    +HasDrivingFeature() bool
    +HasParkingFeature() bool
    +CreateRuntimeCopy() CarConfig
    +CreateComparisonCopy() CarConfig
  }
  class ScreenHistory {
    -_stack: List~ScreenId~
    +Push(ScreenId) void
    +Pop() ScreenId
    +Clear() void
  }
  class ICarFeature {
    <<interface>>
    +FeatureId: string
    +SetActive(bool) void
    +ResetFeature() void
  }
  class CarController {
    -_wheelBase: float = 2.7
    -_maxSteerAngle: float = 35
    -_maxSteerRate: float = 30
    +SpeedKmh: float
    +SetSpeed(float) void
    +SetSteer(float) void
    +SetRearSteer(float) void
    +Brake(float) void
    +Follow(Vector3[], float, bool) IEnumerator
    +ResetPose(Pose) void
  }
  class CarAppearance {
    -_bodyMaterials: Material[4]
    -_trims: GameObject[2]
    -_wheels: GameObject[2]
    +Apply(CarConfig) void
  }
  class CarFeatureSwitch {
    -_features: ICarFeature[*]
    +Apply(CarConfig) void
    +ResetAll() void
  }
  AreaBase <|-- ConfiguratorArea
  AreaBase <|-- DrivingArea
  AreaBase <|-- ParkingArea
  ModeManager "1" o-- "3" AreaBase : _areas
  ModeManager "1" *-- "1" CarConfig : Config
  ModeManager "1" *-- "1" ScreenHistory
  ModeManager ..> AppMode
  AreaBase ..> CarConfig : ResetArea
  ConfiguratorArea --> CarAppearance : 미리보기 차량
  CarAppearance ..> CarConfig : Apply
  CarFeatureSwitch ..> CarConfig : Apply
  CarFeatureSwitch "1" o-- "*" ICarFeature : _features
```

- `ModeManager`는 `SwitchTo(AppMode)`로 `AreaBase` 3개만 다룬다. 영역 안 클래스와는 선이 없다.
- `AreaBase.OnEnter()`가 영역별 조명(`ApplyLighting()`: `RenderSettings.ambientLight` 등)과 분할 카메라 2대(`SetSplitCameras(true)`)를 켠다(FR-11, FR-38). `ModeManager`는 활성 씬만 지정한다.
- `CarController`가 모든 차량의 유일한 구동부다. 자전거 모델로 위치·방향을 갱신하고(FR-43, 뒷바퀴 조향 포함), `Follow()`로 웨이포인트를 따라가며(FR-41), `ResetPose()`로 위치·속도·조향각을 처음 상태로 되돌린다(FR-39).
- `CarConfig` 기능 값 4개의 초기값은 모두 true(FR-05). 실행 중에는 `CreateRuntimeCopy()` 복사본을 수정한다. ScriptableObject는 이 하나뿐이다.
- `ScreenHistory`는 뒤로가기·홈의 화면 이동 기록이다(FR-53·54). `ScreenId`는 같은 파일의 중첩 enum이다.

## 도면 2. 공용 — Scenario · Camera · UI · Utils (공통)

```mermaid
classDiagram
  direction TB
  class ScenarioBase {
    <<abstract>>
    #_currentIndex: int
    +IsRunning: bool
    +StartFirstScene(CarConfig) void
    +RestartCurrentScene(CarConfig) void
    +GoToNextScene() void
    #IsSceneEnabled(int, CarConfig)* bool
    #RunScene(int)* IEnumerator
    -FadeAndMove() IEnumerator
  }
  class ScenarioTrigger {
    -_eventId: string
    +Triggered: Action~string~
    -OnTriggerEnter(Collider) void
  }
  class CameraSwitcher {
    -_left: Camera
    -_right: Camera
    +Current: CameraView
    +Bind(Camera, Camera) void
    +SwitchTo(CameraView) void
    +SetFixedView(bool) void
    +SetRightDriverView() void
  }
  class CameraView {
    <<enumeration>>
    Driver
    BirdEye
    Fixed
  }
  class ShowroomCamera {
    -_rotateSpeed: float
    +HandleDrag(Vector2) void
  }
  class UIManager {
    -_panels: GameObject[*]
    +ShowFor(AppMode) void
  }
  class ExteriorPanel {
    -_summaryCard: TMP_Text
    +OnColorClicked(int) void
    +OnTrimClicked(int) void
    +OnWheelClicked(int) void
  }
  class FeaturePanel {
    -_tabs: Button[2]
    -_toggles: Toggle[4]
    -_descriptionTexts: TMP_Text[4]
    +ShowTab(int) void
    -RefreshStartButtons() void
  }
  class OptionCardPanel {
    -_cards: Toggle[2]
    +Bind(AppMode, CarConfig) void
    -OnToggleChanged(int, bool) void
    -LockLastEnabled() void
  }
  class ScenarioHUD {
    -_leftLabel: TMP_Text
    -_rightLabel: TMP_Text
    +SetViewButtonsVisible(bool) void
    +SetValues(float, float, float) void
    +ShowRestartNotice() void
  }
  class NavigationBar {
    +OnBackClicked() void
    +OnHomeClicked() void
  }
  class Units {
    <<static>>
    +KmhToMs(float) float$
    +MsToKmh(float) float$
  }
  ScenarioBase "1" o-- "*" ScenarioTrigger
  ScenarioBase --> CameraSwitcher
  CameraSwitcher ..> CameraView
  OptionCardPanel ..> ScenarioBase : RestartCurrentScene
  UIManager o-- ExteriorPanel
  UIManager o-- FeaturePanel
  UIManager o-- ScenarioHUD
  UIManager o-- OptionCardPanel
  UIManager o-- NavigationBar
```

- `ScenarioBase`가 장면 순서(FR-57), 꺼진 장면 건너뛰기(FR-62·63), 토글 시 재시작(FR-48), 0.5초 페이드를 맡는다. 자식은 `IsSceneEnabled`와 `RunScene`만 구현한다.
- 분할 화면 역할: 카메라 2대 켜기·끄기는 `AreaBase`, 시점 전환은 `CameraSwitcher`(영역에 들어올 때 `Bind(LeftCam, RightCam)`), 좌우 라벨은 `ScenarioHUD`.
- `FeaturePanel`은 설명 문구 4개를 `TMP_Text` 필드에 Inspector로 직접 입력한다(SRS 부록 A 문구).

## 도면 3. 주행 — Driving (김예은)

```mermaid
classDiagram
  direction TB
  class AreaBase {
    <<abstract>>
  }
  class ScenarioBase {
    <<abstract>>
  }
  class ICarFeature {
    <<interface>>
  }
  class CarController
  class DrivingArea {
    -_carA: CarController
    -_carB: CarController
    -_pedestrians: Pedestrian[*]
    -_scenario: DrivingScenario
    +ResetArea(CarConfig) void
  }
  class DrivingScenario {
    -_driverA: VirtualDriver
    -_driverB: VirtualDriver
    #IsSceneEnabled(int, CarConfig) bool
    #RunScene(int) IEnumerator
    -RunNightVision() IEnumerator
    -RunAlleyUTurn() IEnumerator
  }
  class VirtualDriver {
    -_car: CarController
    -_route: Transform[*]
    -_speedProfile: AnimationCurve
    -_visibleRange: float = 50
    -_reactionTime: float = 1.5
    -_brakeDecel: float = 6.0
    +Drive() IEnumerator
    +Observe(Pedestrian) void
    +RequestBrake(float) void
    +ResetDriver() void
  }
  class Pedestrian {
    -_walkSpeed: float = 1.2
    -_startPose: Pose
    +IsOnRoad: bool
    +StartWalking() void
    +ResetPose() void
  }
  class NightVisionSensor {
    -_detectRange: float = 120
    -_ttcWarn: float = 4.0
    -_ttcBrake: float = 2.5
    -_brakeDecel: float = 5.0
    -_driver: VirtualDriver
    +Target: Pedestrian [0..1]
    +CurrentTtc: float
    -CalcTtc(Pedestrian) float
    +SetActive(bool) void
    +ResetFeature() void
  }
  class NightVisionDisplay {
    -_thermalCamera: Camera
    -_output: RenderTexture
    +ShowTarget(Pedestrian, float, float) void
    +SetWarning(bool) void
  }
  class RearWheelSteering {
    -_counterAngle: float = 10
    -_counterBelowKmh: float = 30
    -_sameAngle: float = 2
    -_sameAboveKmh: float = 60
    +GetRearAngle(float, float) float
    +SetActive(bool) void
    +ResetFeature() void
  }
  class TurningRadiusCalculator {
    <<static>>
    +Radius(float, float, float) float$
    +Icr(Transform, float, float, float) Vector3$
  }
  class DrivingDrawer {
    -_headlampRange: float = 50
    -_thermalRange: float = 120
    -_trajectory: LineRenderer[2]
    +ShowCones(bool) void
    +RecordTrajectory(int, Vector3) void
    +ShowIcr(int, Vector3, float) void
    +Clear() void
  }
  AreaBase <|-- DrivingArea
  ScenarioBase <|-- DrivingScenario
  ICarFeature <|.. NightVisionSensor
  ICarFeature <|.. RearWheelSteering
  DrivingArea "1" *-- "1" DrivingScenario : _scenario
  DrivingArea "1" o-- "*" Pedestrian
  DrivingArea "1" o-- "2" CarController : A, B
  DrivingScenario "1" *-- "2" VirtualDriver : A, B
  VirtualDriver --> CarController : 운전
  VirtualDriver --> Pedestrian : 헤드램프 인지
  NightVisionSensor --> Pedestrian : 열화상 감지
  NightVisionSensor --> VirtualDriver : 감속 요청
  NightVisionSensor --> NightVisionDisplay
  RearWheelSteering --> CarController : SetRearSteer
  RearWheelSteering ..> TurningRadiusCalculator
  DrivingDrawer ..> TurningRadiusCalculator
```

- 빈 상자(AreaBase, ScenarioBase, ICarFeature, CarController)는 도면 1·2의 클래스를 다시 표시한 것이다.
- 두 차량 모두 `VirtualDriver`가 같은 경로·속도 프로파일로 운전한다. 오른쪽 차량에서만 `NightVisionSensor`·`RearWheelSteering`이 켜지고, 센서는 운전자에게 `RequestBrake(5.0)`를 보낸다. 왼쪽 운전자는 헤드램프 50m 인지 → 1.5초 → 6.0m/s²로만 반응한다.
- 골목 유턴도 `VirtualDriver`가 웨이포인트 경로를 따라간다. 왼쪽은 3회 유턴 경로, 오른쪽은 1회 경로를 쓰고 후륜 조향이 `SetRearSteer`로 뒷바퀴를 꺾는다.
- 그리는 일은 `DrivingDrawer` 하나가 맡는다(헤드램프·열화상 원뿔, 회전 궤적, ICR, 반경 라벨).

## 도면 4. 주차 — Parking (강유나, 검토 필요)

```mermaid
classDiagram
  direction TB
  class AreaBase {
    <<abstract>>
  }
  class ScenarioBase {
    <<abstract>>
  }
  class ICarFeature {
    <<interface>>
  }
  class CarController
  class ParkingArea {
    -_map: GridMap
    -_tableA: ReservationTable
    -_tableB: ReservationTable
    -_slots: ParkingSlotAllocator
    -_scenario: ParkingScenario
    +ResetArea(CarConfig) void
  }
  class ParkingScenario {
    -_agents: ValetAgent[*]
    -_remote: RemoteParking
    #IsSceneEnabled(int, CarConfig) bool
    #RunScene(int) IEnumerator
  }
  class GridMap {
    -_cellSize: float = 2.5
    -_width: int = 20
    -_height: int = 12
    +WorldToCell(Vector3) Vector2Int
    +CellToWorld(Vector2Int) Vector3
  }
  class ParkingSlotAllocator {
    -_slots: ParkingSlot[*]
    +AssignEmpty() ParkingSlot
    +ResetSlots() void
  }
  class ReservationTable {
    -_slotTime: float = 0.5
    +IsOn: bool
    +TryReserve(int, List~Vector2Int~, float) bool
    +Release(int, Vector2Int) void
    +SetActive(bool) void
    +ResetFeature() void
  }
  class ValetAgent {
    -_car: CarController
    -_route: Transform[*]
    -_reverseRoute: Transform[*]
    -_table: ReservationTable
    +State: AgentState
    +Go() IEnumerator
    -IsPathBlocked() bool
    +ResetAgent() void
  }
  class RemoteParking {
    -_car: CarController
    -_speedKmh: float = 3
    -_stopTime: float = 0.2
    +State: RemoteState
    +Press(int) void
    +Release() void
    +Board() void
    +SetActive(bool) void
    +ResetFeature() void
  }
  class SmartKeyUI {
    +SetBoardEnabled(bool) void
    +SetStatus(string) void
  }
  class ProximityStop {
    -_stopDistance: float = 0.3
    +IsBlocked: bool
  }
  class ParkingDrawer {
    +DrawReservations(ReservationTable) void
    +DrawReverseRoute(Transform[], float) void
    +Clear() void
  }
  class DoorSwingDrawer {
    -_doorLength: float = 1.1
    -_requiredGap: float = 0.6
    +CanOpen() bool
  }
  AreaBase <|-- ParkingArea
  ScenarioBase <|-- ParkingScenario
  ICarFeature <|.. ReservationTable
  ICarFeature <|.. RemoteParking
  ParkingArea "1" *-- "1" GridMap
  ParkingArea "1" *-- "2" ReservationTable : A OFF, B 옵션
  ParkingArea "1" *-- "1" ParkingSlotAllocator
  ParkingArea "1" *-- "1" ParkingScenario
  ParkingScenario "1" o-- "*" ValetAgent : E, NPC1, NPC2 x2
  ValetAgent --> CarController : 운전
  ValetAgent --> ReservationTable : 예약 요청
  ReservationTable --> GridMap
  RemoteParking --> CarController
  RemoteParking --> ProximityStop
  RemoteParking --> DoorSwingDrawer : 탑승 가능 판정
  SmartKeyUI --> RemoteParking : 누름·뗌·탑승
  ParkingDrawer ..> ReservationTable
```

- `ICarFeature`는 `ReservationTable`(발렛 = 예약 테이블 ON/OFF)과 `RemoteParking`이 구현한다. 주차장 A의 테이블은 항상 OFF, 주차장 B의 테이블은 `valetParking` 값을 따른다.
- `ValetAgent`는 기능이 아니라 차량을 움직이는 에이전트다. E와 NPC 모두 같은 클래스이고, 전역 웨이포인트(`_route`)와 후진 웨이포인트(`_reverseRoute`)를 `CarController.Follow()`로 따라간다. 테이블이 OFF면 예약 없이 가다가 앞이 막히면 정지한다(FR-28).
- 그리는 일은 `ParkingDrawer` 하나(예약 셀, 후진 웨이포인트)가 맡고, 탑승 가능 판정(`CanOpen`)이 있는 `DoorSwingDrawer`만 따로 둔다.

## 클래스 도출표

| 네임스페이스 | 클래스 | 수 |
| --- | --- | --- |
| `Yeyu.Core` | ModeManager, AreaBase, AppMode, CarConfig, ConfiguratorArea, ScreenHistory | 6 |
| `Yeyu.Vehicle` | CarController, CarAppearance, CarFeatureSwitch, ICarFeature | 4 |
| `Yeyu.Camera` | CameraSwitcher (+ CameraView), ShowroomCamera | 2 |
| `Yeyu.Scenario` | ScenarioBase, ScenarioTrigger | 2 |
| `Yeyu.UI` | UIManager, ExteriorPanel, FeaturePanel, ScenarioHUD, OptionCardPanel, NavigationBar | 6 |
| `Yeyu.Utils` | Units | 1 |
| `Yeyu.Driving` | DrivingArea, DrivingScenario, VirtualDriver, Pedestrian, NightVisionSensor, NightVisionDisplay, RearWheelSteering, TurningRadiusCalculator, DrivingDrawer | 9 |
| `Yeyu.Parking` | ParkingArea, ParkingScenario, GridMap, ParkingSlotAllocator, ReservationTable, ValetAgent, RemoteParking, SmartKeyUI, ProximityStop, ParkingDrawer, DoorSwingDrawer | 11 |
| 합계 |  | 41 |

## ICarFeature 구현과 CarConfig 필드 대응

| CarConfig 필드 | 구현 클래스 | 붙는 곳 | 장면 |
| --- | --- | --- | --- |
| nightVision | NightVisionSensor | 차량 (`CarFeatureSwitch`가 켜고 끔) | 주행 1 |
| rearWheelSteering | RearWheelSteering | 차량 (`CarFeatureSwitch`) | 주행 2 |
| valetParking | ReservationTable | 주차장 B (`ParkingArea.ResetArea()`가 켜고 끔) | 주차 1 |
| remoteParking | RemoteParking | 차량 (`CarFeatureSwitch`) | 주차 2 |
