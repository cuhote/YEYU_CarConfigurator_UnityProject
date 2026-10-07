# 클래스 설계서

> 팀 YEYU · 작성 김예은 · 2026.10.07 · 기준: SRS v0.6, `FOLDER_STRUCTURE.md` 5장
> 파일 하나 = 클래스 하나. 이름은 `CONVENTIONS.md`(private 필드 `_camelCase`, 네임스페이스 `Yeyu.<폴더>`)를 따른다.

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
    #_leftCam: Camera
    #_rightCam: Camera
    +Root: GameObject
    +LeftCam: Camera
    +RightCam: Camera
    +ResetArea(CarConfig)* void
    +OnEnter() void
    +OnExit() void
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
  class FeatureDescriptions {
    <<ScriptableObject>>
    -_entries: Entry[4]
    +TryGet(string, Entry) bool
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
    -_wheels: WheelCollider[4]
    -_wheelBase: float = 2.7
    -_maxSteerAngle: float = 35
    +SpeedKmh: float
    +SetThrottle(float) void
    +SetSteer(float) void
    +SetBrake(float) void
    +SetRearSteer(float) void
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

- `ModeManager`는 `SwitchTo(AppMode)`로 `AreaBase` 3개만 다룬다. 영역 안 클래스와는 선이 없다. 분할 카메라 2대는 각 영역의 `AreaBase`가 `OnEnter()`·`OnExit()`에서 켜고 끈다(ConfiguratorArea는 쇼룸 카메라 1대).
- Car.prefab에 `CarAppearance` · `CarFeatureSwitch` · 구동부 · 기능 컴포넌트가 함께 붙는다. 기능은 `ICarFeature` 타입으로 켜고 끈다(NFR-06).
- `CarConfig` 기능 값 4개의 초기값은 모두 true(FR-05). 실행 중에는 `CreateRuntimeCopy()` 복사본을 수정한다.
- `ScreenHistory`는 뒤로가기·홈의 화면 이동 기록이다(SRS 6장, FR-53·54). `ScreenId`는 같은 파일의 중첩 enum이다.

## 도면 2. 공용 — Scenario · Camera · UI · Utils (공통)

```mermaid
classDiagram
  direction TB
  class ScenarioBase {
    <<abstract>>
    #_scenes: SceneStep[2]
    #_currentIndex: int
    #_result: ScenarioResult
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
  class ScenarioResult {
    +stopDistance: float[2]
    +minTtc: float[2]
    +turnRadius: float[2]
    +uTurnCount: int[2]
    +collided: bool[2]
    +waitTime: float[2]
    +parkingError: float[2]
    +Clear() void
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
    -_descriptions: FeatureDescriptions
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
  class ResultPanel {
    +Show(ScenarioResult) void
  }
  class DebugDraw {
    <<static>>
    +Cone(Transform, float, float, Material) GameObject$
    +Polyline(LineRenderer, Vector3[]) void$
    +Sector(Transform, float, float, Material) GameObject$
  }
  class Units {
    <<static>>
    +KmhToMs(float) float$
    +MsToKmh(float) float$
  }
  ScenarioBase "1" *-- "1" ScenarioResult : _result
  ScenarioBase "1" o-- "*" ScenarioTrigger
  ScenarioBase --> CameraSwitcher
  CameraSwitcher ..> CameraView
  OptionCardPanel ..> ScenarioBase : RestartCurrentScene
  FeaturePanel --> FeatureDescriptions
  ResultPanel ..> ScenarioResult
  UIManager o-- ExteriorPanel
  UIManager o-- FeaturePanel
  UIManager o-- ScenarioHUD
  UIManager o-- OptionCardPanel
  UIManager o-- NavigationBar
  UIManager o-- ResultPanel
```

- `ScenarioBase`가 장면 순서(FR-57), 꺼진 장면 건너뛰기(FR-62·63), 토글 시 재시작(FR-48), 0.5초 페이드를 맡는다. 자식은 `IsSceneEnabled`와 `RunScene`만 구현한다.
- `ScenarioResult`의 배열 길이 2는 [0] 왼쪽(미적용), [1] 오른쪽(적용)이다.
- 분할 화면은 역할을 셋으로 나눈다: 카메라 2대를 왼쪽·오른쪽 Viewport로 켜고 끄는 일은 `AreaBase.SetSplitCameras()`(영역마다 카메라가 다르므로), 시점 전환은 `CameraSwitcher`(영역에 들어올 때 `Bind(LeftCam, RightCam)`), 좌우 라벨 "옵션 미적용 / 선택 옵션 적용"은 `ScenarioHUD`가 맡는다.

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
    #IsSceneEnabled(int, CarConfig) bool
    #RunScene(int) IEnumerator
    -RunNightVision() IEnumerator
    -RunAlleyUTurn() IEnumerator
  }
  class VirtualDriver {
    -_visibleRange: float = 50
    -_reactionTime: float = 1.5
    -_brakeDecel: float = 6.0
    -_turnWaypoints: Transform[*]
    +Observe(Pedestrian) void
    +DoThreePointTurn() IEnumerator
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
  class SensorConeDrawer {
    -_headlampRange: float = 50
    -_thermalRange: float = 120
    +Show(bool) void
  }
  class TrajectoryDrawer {
    -_line: LineRenderer
    -_icrMarker: Transform
    +Record(Vector3) void
    +ShowRadius(float) void
    +Clear() void
  }
  AreaBase <|-- DrivingArea
  ScenarioBase <|-- DrivingScenario
  ICarFeature <|.. NightVisionSensor
  ICarFeature <|.. RearWheelSteering
  DrivingArea "1" *-- "1" DrivingScenario : _scenario
  DrivingArea "1" o-- "*" Pedestrian
  DrivingArea "1" o-- "2" CarController : A, B
  DrivingScenario "1" *-- "1" VirtualDriver : 코스 A
  VirtualDriver --> CarController
  VirtualDriver --> Pedestrian : 인지
  NightVisionSensor --> Pedestrian : 감지
  NightVisionSensor --> CarController : 감속 요청
  NightVisionSensor --> NightVisionDisplay
  RearWheelSteering --> CarController : SetRearSteer
  RearWheelSteering ..> TurningRadiusCalculator
  TrajectoryDrawer ..> TurningRadiusCalculator
  SensorConeDrawer ..> NightVisionSensor
```

- 빈 상자(AreaBase, ScenarioBase, ICarFeature, CarController)는 도면 1·2의 클래스를 다시 표시한 것이다.
- `NightVisionSensor`와 `RearWheelSteering`은 `ICarFeature`를 구현하고 오른쪽 차량에서만 켜진다. 왼쪽 차량은 `VirtualDriver`가 운전한다.
- 필드 기본값은 SRS v0.6과 SCENARIO.md 2·3장 값이다. `TurningRadiusCalculator.Radius(L, δf, δr)`는 R = L ÷ (tan δf − tan δr)이며 상태가 없어 static이다.

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
  class ParkingArea {
    -_map: GridMap
    -_table: ReservationTable
    -_slots: ParkingSlotAllocator
    -_scenario: ParkingScenario
    +ResetArea(CarConfig) void
  }
  class ParkingScenario {
    -_agents: ValetAgent[3]
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
  class IPathProvider {
    <<interface>>
    +GetPath(Vector2Int, Vector2Int) List~Vector2Int~
  }
  class WaypointPathProvider {
    -_routes: Transform[*]
    +GetPath(Vector2Int, Vector2Int) List~Vector2Int~
  }
  class AStarPathProvider {
    -_map: GridMap
    +GetPath(Vector2Int, Vector2Int) List~Vector2Int~
  }
  class ReservationTable {
    -_slotTime: float = 0.5
    +TryReserve(int, List~Vector2Int~, float) bool
    +Release(int, Vector2Int) void
    +Clear() void
  }
  class ValetAgent {
    -_useReservation: bool
    -_pathProvider: IPathProvider
    +State: AgentState
    +Go(ParkingSlot) IEnumerator
    +SetActive(bool) void
    +ResetFeature() void
  }
  class ReverseParkingPlanner {
    -_arcRadius: float = 5.0
    +Plan(Pose, Pose) List~Vector3~
    -CheckClearance(List~Vector3~) bool
  }
  class PurePursuitController {
    -_lookAhead: float = 2.0
    -_maxSteerRate: float = 30
    -_maxSpeedKmh: float = 5
    +Step(List~Vector3~, Pose) void
  }
  class IVehicleDriver {
    <<interface>>
    +SetSteer(float) void
    +SetSpeed(float) void
  }
  class BicycleModelDriver {
    -_wheelBase: float = 2.7
    -_maxSteerAngle: float = 35
    +SetSteer(float) void
    +SetSpeed(float) void
  }
  class RemoteParking {
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
  class ReservationCellDrawer {
    +Draw(ReservationTable) void
  }
  class ReversePathDrawer {
    +Draw(List~Vector3~, float) void
  }
  class DoorSwingDrawer {
    -_doorLength: float = 1.1
    -_requiredGap: float = 0.6
    +CanOpen() bool
  }
  AreaBase <|-- ParkingArea
  ScenarioBase <|-- ParkingScenario
  ICarFeature <|.. ValetAgent
  ICarFeature <|.. RemoteParking
  IPathProvider <|.. WaypointPathProvider
  IPathProvider <|.. AStarPathProvider
  IVehicleDriver <|.. BicycleModelDriver
  ParkingArea "1" *-- "1" GridMap
  ParkingArea "1" *-- "1" ReservationTable
  ParkingArea "1" *-- "1" ParkingSlotAllocator
  ParkingArea "1" *-- "1" ParkingScenario
  ParkingScenario "1" o-- "3" ValetAgent : E, NPC1, NPC2
  ValetAgent --> IPathProvider
  ValetAgent --> ReservationTable
  ValetAgent --> ReverseParkingPlanner
  ValetAgent --> PurePursuitController
  PurePursuitController --> IVehicleDriver : 조향각·속도
  RemoteParking --> IVehicleDriver
  RemoteParking --> ProximityStop
  RemoteParking --> DoorSwingDrawer : 탑승 가능 판정
  SmartKeyUI --> RemoteParking : 누름·뗌·탑승
  AStarPathProvider --> GridMap
  ReservationCellDrawer ..> ReservationTable
```

- SRS 2.1 '주차 영역 차량 이동 구조'를 그대로 옮겼다: 경로 공급부 → 예약 테이블 → 후진 궤적 → Pure Pursuit → 구동부.
- `IPathProvider`(NFR-10)와 `IVehicleDriver`(NFR-11)를 실체화로 분리해, 구현을 바꿔도 `ValetAgent`·`PurePursuitController` 코드는 그대로다.
- `RemoteParking`은 원격 이동(FR-31~33)과 탑승(FR-64~66)을 맡고, 탑승 가능 판정은 `DoorSwingDrawer.CanOpen()`으로 한다.

## 클래스 도출표

| 네임스페이스 | 클래스 | 수 |
| --- | --- | --- |
| `Yeyu.Core` | ModeManager, AreaBase, AppMode, CarConfig, FeatureDescriptions, ConfiguratorArea, ScreenHistory | 7 |
| `Yeyu.Vehicle` | CarController, CarAppearance, CarFeatureSwitch, ICarFeature | 4 |
| `Yeyu.Camera` | CameraSwitcher (+ CameraView), ShowroomCamera | 2 |
| `Yeyu.Scenario` | ScenarioBase, ScenarioTrigger, ScenarioResult | 3 |
| `Yeyu.UI` | UIManager, ExteriorPanel, FeaturePanel, ScenarioHUD, OptionCardPanel, NavigationBar, ResultPanel | 7 |
| `Yeyu.Utils` | DebugDraw, Units | 2 |
| `Yeyu.Driving` | DrivingArea, DrivingScenario, VirtualDriver, Pedestrian, NightVisionSensor, NightVisionDisplay, RearWheelSteering, TurningRadiusCalculator, SensorConeDrawer, TrajectoryDrawer | 10 |
| `Yeyu.Parking` | ParkingArea, ParkingScenario, GridMap, ParkingSlotAllocator, IPathProvider, WaypointPathProvider, AStarPathProvider, ReservationTable, ValetAgent, ReverseParkingPlanner, PurePursuitController, IVehicleDriver, BicycleModelDriver, RemoteParking, SmartKeyUI, ProximityStop, ReservationCellDrawer, ReversePathDrawer, DoorSwingDrawer | 19 |
| 합계 | | 54 |

## ICarFeature 구현과 CarConfig 필드 대응

| CarConfig 필드 | 구현 클래스 | 장면 |
| --- | --- | --- |
| nightVision | NightVisionSensor | 주행 1 |
| rearWheelSteering | RearWheelSteering | 주행 2 |
| valetParking | ValetAgent (`_useReservation`) | 주차 1 |
| remoteParking | RemoteParking | 주차 2 |
