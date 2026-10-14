# 기능 중심 차량 컨피규레이터

이름만으로는 이해하기 어려운 차량 고급 옵션을 "선택 안 함 vs 선택함" 분할 화면으로 비교해 보여 주는 Unity 컨피규레이터입니다.

<!-- TODO: 시연 GIF 또는 스크린샷. docs/images/에 넣고 아래 줄의 주석을 푼다 -->
<!-- ![시연 화면](./docs/images/demo.gif) -->

차체 색상과 휠을 고른 뒤 보고 싶은 기능을 선택하면, 왼쪽에는 기능이 없는 차, 오른쪽에는 기능이 있는 차가 같은 경로를 같은 속도로 달립니다. 모든 화면 전환은 로딩 없이 하나의 씬 안에서 이루어집니다.

## 주요 기능

| 구분 | 기능 | 보여 주는 장면 |
| --- | --- | --- |
| 외관 | 색상·휠 선택 | 고른 차체 색상과 휠이 차량에 즉시 반영됩니다. |
| 주행 | 나이트 비전 | 야간 외곽 도로에서 헤드램프 범위 밖의 보행자를 열화상으로 먼저 감지합니다. |
| 주행 | 후륜 조향 | 같은 2차로 도로에서 유턴할 때 회전 반경이 4.12m에서 3.29m로 줄어듭니다. |
| 주차 | 주차장 연동 발렛 | 주차장 관제에서 경로를 허가받아 배정된 주차면까지 스스로 이동하고 후진 주차합니다. |
| 주차 | 스마트키 원격 주차 | 문을 열 공간이 없는 좁은 주차면에서 스마트키로 차를 빼낸 뒤 탑승합니다. |

진행 순서는 외관 선택 → 기능 선택 → 시나리오 비교입니다. 주행·주차 탭에서 기능을 하나 이상 고르면 시작할 수 있고, 끈 기능의 장면은 건너뜁니다. 스마트키 장면을 제외한 장면에서는 운전석 시점과 버드뷰를 오갈 수 있습니다.

시나리오 차량은 사용자가 운전하지 않고 정해진 경로를 자동으로 주행합니다. 가격 계산, 주문·결제, 실차 데이터 연동은 범위에 포함하지 않습니다.

## 실행 방법

필요한 것은 Unity `6000.6.0f1`, Git LFS, Windows 10 이상과 독립 그래픽카드입니다.

```bash
git lfs install
git clone https://github.com/cuhote/YEYU_CarConfigurator_UnityProject.git
cd YEYU_CarConfigurator_UnityProject
./scripts/setup-git-hooks.sh   # 기여할 때만. 커밋 규칙 훅을 켠다
```

Unity Hub에서 프로젝트 폴더를 열고 `Assets/_Project/Scenes/MainScene.unity`를 실행합니다. 앱이 쓰는 씬은 이것 하나입니다. 조작은 마우스와 키보드로 합니다.

## 기술 스택

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6 (`6000.6.0f1`) |
| 렌더링 | Universal Render Pipeline 17.6 |
| 입력 | Input System 1.20 |
| UI | uGUI, TextMesh Pro (Pretendard) |
| 차량 구동 | 자전거 모델 운동학 구동 (뒷바퀴 조향 포함). PhysX는 충돌 판정과 가상 센서에만 사용 |
| 주차 관제 | 격자 셀·시간 구간 예약 테이블과 웨이포인트 경로 |

## 폴더 구조

```
├── Assets/
│   ├── _Project/          ← 팀이 만든 파일 전부
│   │   ├── Scenes/        ← MainScene.unity
│   │   ├── Scripts/       ← Core · Vehicle · Camera · Scenario · UI · Driving · Parking
│   │   ├── Prefabs/       ← 영역 프리팹 3개 (옵션 선택 · 주행 · 주차)와 공통 프리팹
│   │   └── Data/          ← CarConfig.asset (선택한 옵션)
│   └── ThirdParty/        ← 에셋 스토어 에셋 (수정 금지)
├── docs/                  ← 요구사항 · 설계 · 일정 문서
├── scripts/               ← Git 훅 설정, .meta 검사
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
