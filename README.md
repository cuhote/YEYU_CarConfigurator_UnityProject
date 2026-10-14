# 기능 중심 차량 컨피규레이터

차량 고급 옵션이 실제로 무엇을 해 주는지, 옵션이 없는 차와 있는 차를 나란히 달리게 해서 보여 주는 Unity 컨피규레이터입니다.

<!-- TODO: 시연 GIF 또는 스크린샷. docs/images/에 넣고 아래 줄의 주석을 푼다 -->
<!-- ![시연 화면](./docs/images/demo.gif) -->

"나이트 비전", "후륜 조향" 같은 옵션은 이름만으로는 차이를 알기 어렵습니다. 이 프로젝트는 화면을 좌우로 나눠 왼쪽에는 옵션이 없는 차, 오른쪽에는 옵션이 있는 차를 두고, 같은 상황에서 두 차가 어떻게 달라지는지 보여 줍니다.

## 사용 흐름

1. **외관 선택**: 차체 색상과 휠을 고르면 차량에 바로 반영됩니다.
2. **기능 선택**: 주행·주차 탭에서 보고 싶은 기능을 고릅니다. 탭마다 하나 이상 고르면 시작할 수 있습니다.
3. **시나리오 비교**: 고른 기능의 장면이 차례로 재생됩니다. 끈 기능의 장면은 건너뜁니다.

화면 전환은 모두 하나의 씬 안에서 로딩 없이 이루어집니다.

## 주요 기능

### 주행

| 기능 | 왼쪽 (미적용) | 오른쪽 (적용) |
| --- | --- | --- |
| 나이트 비전 | 야간 도로에서 보행자가 헤드램프 범위(50m) 안에 들어온 뒤에야 제동을 시작합니다. | 열화상 카메라가 120m 앞의 보행자를 먼저 감지해 경고하고 미리 감속합니다. |
| 후륜 조향 | 2차로 도로에서 전진·후진을 섞어 세 번에 유턴합니다. (회전 반경 4.12m) | 뒷바퀴가 반대 방향으로 꺾여 한 번에 유턴합니다. (회전 반경 3.29m) |

### 주차

| 기능 | 왼쪽 (미적용) | 오른쪽 (적용) |
| --- | --- | --- |
| 주차장 연동 발렛 | 차량들이 예약 없이 이동하다가 경로가 겹치면 멈춰 섭니다. | 주차장 관제에 경로를 예약한 뒤 이동해, 서로 막지 않고 후진 주차까지 마칩니다. |
| 스마트키 원격 주차 | 좁은 주차면에서 운전석 문이 옆 차에 막혀 열리다 멈추고 "탑승 불가"가 표시됩니다. | 스마트키의 [전진] [후진]으로 차를 주차면 밖으로 빼낸 뒤 [탑승]하면 문이 열리고 운전석 시점으로 바뀝니다. |

스마트키 장면을 제외한 장면에서는 운전석 시점과 위에서 내려다보는 시점을 오갈 수 있습니다. 위에서 보는 시점에서는 센서 범위, 회전 궤적, 예약된 경로 같은 동작 근거가 함께 표시됩니다.

시나리오 차량은 사용자가 운전하지 않고 정해진 경로를 자동으로 주행합니다. 가격 계산, 주문·결제, 실차 데이터 연동은 범위에 포함하지 않습니다.

## 실행 방법

### 실행 파일로 실행

Windows 10 이상과 독립 그래픽카드가 필요합니다.

1. 저장소 루트의 [`BuildApp_YEYU_CarConfigurator_v2.zip`](./BuildApp_YEYU_CarConfigurator_v2.zip)을 내려받습니다. GitHub에서는 파일 화면의 다운로드 버튼을 누릅니다.
2. 압축을 풉니다.
3. `BuildApp_YEYU_CarConfigurator_v2` 폴더 안의 `YEYU_Car_Configurator.exe`를 실행합니다.

처음 실행할 때 Windows가 "PC 보호" 경고를 띄우면 "추가 정보"를 누른 뒤 "실행"을 누릅니다. 조작은 마우스와 키보드로 합니다.

### 소스에서 실행

Unity `6000.6.0f1`과 Git LFS가 필요합니다.

```bash
git lfs install
git clone https://github.com/cuhote/YEYU_CarConfigurator_UnityProject.git
```

Unity Hub에서 프로젝트 폴더를 열고 `Assets/_Project/Scenes/MainScene.unity`를 실행합니다. 앱이 쓰는 씬은 이것 하나입니다.

기여하려면 클론한 뒤 `./scripts/setup-git-hooks.sh`를 한 번 실행해 커밋 규칙 훅을 켭니다.

## 기술 스택

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6 (`6000.6.0f1`) |
| 렌더링 | Universal Render Pipeline 17.6 |
| 입력 | Input System 1.20 |
| UI | uGUI, TextMesh Pro (Pretendard) |
| 차량 구동 | 자전거 모델 운동학 구동 (뒷바퀴 조향 포함). PhysX는 충돌 판정과 가상 센서에만 사용 |
| 주차 관제 | 격자 셀·시간 구간 예약 테이블과 웨이포인트 경로 |

비교하는 두 차는 같은 운전자 모델이 같은 경로와 속도로 운전하고, 오른쪽 차만 기능을 켭니다. 그래서 화면에 보이는 차이는 모두 기능에서 나옵니다.

## 폴더 구조

```
├── Assets/
│   ├── _Project/          ← 팀이 만든 파일 전부
│   │   ├── Scenes/        ← MainScene.unity
│   │   ├── Scripts/       ← Core · Vehicle · Camera · Scenario · UI · Driving · Parking · Utils
│   │   ├── Prefabs/       ← 영역 프리팹 3개 (옵션 선택 · 주행 · 주차)와 공통 프리팹
│   │   └── Data/          ← CarConfig.asset (선택한 옵션)
│   └── ThirdParty/        ← 에셋 스토어 에셋 (수정 금지)
├── docs/                  ← 요구사항 · 설계 · 일정 문서
├── scripts/               ← Git 훅 설정, .meta 검사
├── BuildApp_YEYU_CarConfigurator_v2.zip   ← Windows 실행 파일 (Git LFS)
├── COMMIT_RULES.md
└── CONVENTIONS.md
```

전체 구조와 파일별 담당자는 [`docs/FOLDER_STRUCTURE.md`](./docs/FOLDER_STRUCTURE.md)에 있습니다.

## 문서

| 문서 | 내용 |
| --- | --- |
| [`docs/SRS.md`](./docs/SRS.md) | 요구사항 명세서 (v1.0). 기능·비기능 요구사항, 화면, 테스트 케이스 |
| [`docs/CLASS_DESIGN.md`](./docs/CLASS_DESIGN.md) | 클래스 설계서와 클래스 다이어그램 |
| [`docs/FOLDER_STRUCTURE.md`](./docs/FOLDER_STRUCTURE.md) | 프로젝트 파일 구조와 담당 |
| [`docs/WBS.md`](./docs/WBS.md) | 작업 분해와 일정 |
| [`docs/TroubleShooting/`](./docs/TroubleShooting) | 개발 중 발견한 문제의 원인과 해결 기록 |
| [`COMMIT_RULES.md`](./COMMIT_RULES.md) | 브랜치 전략, 커밋 메시지, PR 규칙 |
| [`CONVENTIONS.md`](./CONVENTIONS.md) | 네이밍, 파일 관리, 폴더 담당 규칙 |

## 팀

팀 YEYU

| 이름 | 담당 |
| --- | --- |
| 김예은 | 주행 시나리오 (나이트 비전, 후륜 조향) |
| 강유나 | 주차 시나리오 (주차장 연동 발렛, 스마트키 원격 주차) |

영역 전환은 두 사람이 함께 맡았습니다.
