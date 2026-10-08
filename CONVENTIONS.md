# Naming & Code Conventions

> 이 문서는 팀 YEYU가 합의한 네이밍·구조 규칙이다. 별도의 외부 규정 문서는 없다. Git/커밋 규칙은 [`COMMIT_RULES.md`](./COMMIT_RULES.md)를 본다. 규칙을 바꾸려면 [`COMMIT_RULES.md`](./COMMIT_RULES.md) 6절 절차를 따른다.

## 1. 항목 표기

| 표기 | 의미 |
|---|---|
| `[확정]` | 이미 리포에 반영돼 있거나 팀이 결정한 규칙 |
| `[제안]` | 아직 코드에 반영되지 않은 초안. 해당 부분 구현 착수 전에 합의해 `[확정]`으로 올린다 |

## 2. 공통 표기 원칙 [확정]

| 대상 | 표기 | 예시 |
|---|---|---|
| 스크립트·클래스·인터페이스 | `PascalCase`, 파일명 = 클래스명 (인터페이스는 `I` 접두) | `NightVisionSensor.cs`, `ICarFeature` |
| 공개 메서드·프로퍼티 | `PascalCase` | `ResetArea()`, `FeatureId` |
| private 필드 | `_camelCase` | `_detectRange` |
| Inspector 노출 필드 | `[SerializeField] private` + `_camelCase` | `[SerializeField] private float _ttcWarn = 4f;` |
| 씬·프리팹 | `PascalCase` | `DrivingArea.unity`, `NightRoadCourse.prefab` |
| 머티리얼 | `M_` 접두사 | `M_Body_Red`, `M_Cone_IR` |
| 네임스페이스 | `Yeyu.<폴더>` | `Yeyu.Core`, `Yeyu.Driving` |
| 레이어·태그 | `PascalCase` | `Vehicle`, `NightVisionOnly`, `CourseA` |
| 브랜치명 | 소문자 + 하이픈 | `feature/yuna-night-vision` |

**한글 금지**: 폴더, 파일명, 클래스·변수명, 씬 안의 오브젝트 이름, 레이어·태그, 브랜치명, 커밋 제목(`type`/`scope`)에 한글을 쓰지 않는다. 한글은 주석, 문서, 화면에 표시되는 UI 문구에만 쓴다. (단, 커밋 `subject`는 한글 — [`COMMIT_RULES.md`](./COMMIT_RULES.md) 참고)

## 3. 파일 관리 [확정]

### 3.1 커밋 대상과 제외 대상

| 구분 | 대상 | 이유 |
|---|---|---|
| 커밋 | `Assets/` 전체와 모든 `.meta` | 에셋 간 참조(GUID)가 `.meta`에 들어 있다 |
| 커밋 | `Packages/`, `ProjectSettings/` | 패키지 버전, 레이어·태그, 빌드 씬 순서의 유일한 기록 |
| 커밋 | 3D 모델·텍스처·오디오·폰트 (Git LFS) | 재현에 필요한 산출물. 용량이 커서 LFS로 관리 |
| 커밋 | `docs/` 문서와 이미지 (LFS 제외) | GitHub에서 바로 열려야 한다 |
| 제외 | `Library/`, `Temp/`, `Obj/`, `Logs/`, `MemoryCaptures/`, `Recordings/` | Unity가 재생성하며 충돌을 만든다 |
| 제외 | `UserSettings/`, `.vs/`, `.vscode/`, `.idea/`, `*.csproj`, `*.sln` | 개인 환경 설정, IDE가 재생성 |
| 제외 | `Build/`, `Builds/`, `*.apk`, `*.unitypackage` | 빌드 산출물. 용량이 크고 빌드로 재생성된다 |
| 제외 | `~$*` (Office 잠금 파일), `.DS_Store`, `Thumbs.db` | OS·Office가 만드는 임시 파일 |

(위 표는 리포 루트 `.gitignore`/`.gitattributes`와 동일하게 유지한다. 항목을 바꾸면 두 곳을 같이 고친다.)

### 3.2 Git LFS

- LFS 대상 확장자는 `.gitattributes`에 있다: 모델(`fbx`, `obj`, `blend`), 이미지(`png`, `jpg`, `tga`, `psd`, `exr`, `hdr`), 오디오·영상(`wav`, `mp3`, `ogg`, `mp4`), 폰트(`ttf`, `otf`), 문서(`xlsx`, `pdf`, `pptx`).
- `docs/` 아래 파일은 같은 확장자라도 LFS에 넣지 않는다. README 이미지와 WBS 엑셀이 GitHub에서 바로 보여야 하기 때문이다.
- 새 바이너리 형식을 처음 추가할 때는 **파일을 커밋하기 전에** `.gitattributes`에 확장자를 먼저 넣는다. 일반 Git으로 한 번 들어간 대용량 파일은 이력에서 빼기 어렵다.
- 클론 전에 `git lfs install`을 한 번 실행한다.

### 3.3 `.meta` 파일

| 규칙 | 내용 |
|---|---|
| 함께 커밋 | 에셋·폴더를 추가하면 Unity가 만든 `.meta`를 같은 커밋에 넣는다 |
| 함께 삭제 | 에셋·폴더를 지우면 `.meta`도 같이 지운다 |
| 이동·이름 변경 | 탐색기가 아니라 **Unity Project 창 안에서** 한다. 밖에서 옮기면 `.meta`가 따라오지 않아 참조가 끊어진다 |
| 직접 수정 금지 | `.meta`의 `guid`를 손으로 고치거나 다른 파일의 `.meta`를 복사하지 않는다 |
| 빈 폴더 | Git은 빈 폴더를 저장하지 않는다. 폴더만 미리 잡아 둘 때는 `.gitkeep`을 넣는다 (Unity는 `.`으로 시작하는 파일을 무시한다) |

`.meta` 짝이 맞지 않으면 `pre-commit` 훅과 GitHub Actions `meta-check`가 막는다 ([`COMMIT_RULES.md`](./COMMIT_RULES.md) 5절).

### 3.4 Unity 프로젝트 설정

| 항목 | 값 | 이유 |
|---|---|---|
| Version Control Mode | Visible Meta Files | `.meta`를 Git이 추적할 수 있게 한다 |
| Asset Serialization Mode | Force Text | 씬·프리팹을 텍스트로 저장해 diff·병합이 가능하게 한다 |
| Unity Editor 버전 | 두 PC가 동일 (`ProjectSettings/ProjectVersion.txt`) | 버전이 다르면 열 때마다 에셋이 재직렬화돼 불필요한 변경이 생긴다 |
| Build Settings 씬 순서 | `MainScene`(0) → `ConfiguratorArea` → `DrivingArea` → `ParkingArea` | `ModeManager`가 이 순서로 Additive 로드한다 |

레이어·태그는 `ProjectSettings/TagManager.asset`에 저장되므로 한 사람이 처음에 한 번에 추가하고 커밋한다. 목록은 [`docs/FOLDER_STRUCTURE.md`](./docs/FOLDER_STRUCTURE.md) 7장을 본다.

**값 하드코딩 금지**: 거리·시간·속도 임계값(감지 거리, TTC 경고 시간, 조향각 등)은 스크립트에 숫자로 박지 않고 `[SerializeField]` 필드나 ScriptableObject로 노출한다. SRS의 수치는 초기 설정값이며 바뀔 수 있다.

## 4. 폴더 담당 [확정]

씬과 프리팹은 텍스트로 저장해도 두 사람이 같은 파일을 동시에 고치면 병합이 어렵다. 그래서 파일마다 고치는 사람을 정한다 (SRS NFR-09).

| 구분 | 대상 | 수정하는 사람 |
|---|---|---|
| 주행 전용 | `Scripts/Driving/`, `Prefabs/Driving/`, `DrivingArea.unity` | 김예은 |
| 주차 전용 | `Scripts/Parking/`, `Prefabs/Parking/`, `ParkingArea.unity` | 강유나 |
| 공통 | `Scripts/Core·Vehicle·Camera·Scenario·UI·Utils/`, `MainScene.unity`, `ConfiguratorArea.unity`, 공통 프리팹, `ProjectSettings/` | 담당자를 정한 뒤 그 사람만. 수정 전에 상대에게 알리고, 수정 후 바로 `dev`에 병합 |
| 외부 에셋 | `Assets/ThirdParty/` | 수정 금지. 바꿀 땐 `_Project/Prefabs`로 복사해서 수정 |

- 공통 파일의 담당자는 [`docs/FOLDER_STRUCTURE.md`](./docs/FOLDER_STRUCTURE.md) 6장 표에서 정한다.
- 주행·주차 기능은 서로를 참조하지 않는다. 공통 파일만 참조한다.
- 우리 팀이 만든 파일은 전부 `Assets/_Project/` 아래에 둔다. 에셋 스토어에서 받은 파일은 `Assets/ThirdParty/`에 둔다.

## 5. 리포 구조 [확정]

리포 루트는 Unity 프로젝트 루트와 같다. 최상위는 다음으로 고정한다. 전체 트리와 파일 단위 설계는 [`docs/FOLDER_STRUCTURE.md`](./docs/FOLDER_STRUCTURE.md) 참고.

| 경로 | 내용 |
|---|---|
| `Assets/` | `_Project/`(우리 팀 파일), `ThirdParty/`(외부 에셋) |
| `Packages/` | 패키지 목록 (`manifest.json`, `packages-lock.json`) — Unity 프로젝트 생성 시 추가 |
| `ProjectSettings/` | 프로젝트 설정 — Unity 프로젝트 생성 시 추가 |
| `docs/` | SRS, WBS, 폴더 구조, 트러블슈팅 기록, 이미지 |
| `scripts/` | 저장소 관리용 셸 스크립트 (Unity C# 스크립트는 `Assets/_Project/Scripts/`) |
| `.githooks/`, `.github/` | Git 훅, GitHub Actions |

**최상위 신설 금지**: 새 산출물이 생기면 위 폴더 중 가장 맞는 곳 하위에 넣는다. 최상위를 늘리면 이 표가 실제 리포와 어긋난다.

## 6. 기록 규칙 [제안]

### 6.1 요구사항·테스트 ID

- ID 체계는 SRS 1.5절을 따른다: 기능 요구사항 `FR-NN`, 비기능 `NFR-NN`, 화면 `UI-NN`, 테스트 케이스 `TC-NN`. 한 번 부여한 번호는 재사용하지 않는다.
- 테스트 결과는 [`README.md`](./README.md) 7.2절 결과 열에 기록하고, 커밋은 `test(TC-NN): ...` 형식으로 남긴다 ([`COMMIT_RULES.md`](./COMMIT_RULES.md) 2절).
- 측정 원본(fps 로그, 캡처)을 남길 때는 파일명을 테스트 케이스 ID로 시작한다. 예: `docs/images/TC-17_fps.png`.

### 6.2 트러블슈팅 기록

- 개발 중 발견한 문제는 `docs/TroubleShooting/YYYY-MM-DD-<영역>-<주제>.md`로 기록한다. [`docs/TroubleShooting/_TEMPLATE.md`](./docs/TroubleShooting/_TEMPLATE.md)를 복사해서 쓴다.
- 영역은 `CORE · VEHICLE · CAMERA · SCENARIO · UI · DRIVING · PARKING · ASSETS · SETTINGS · REPO`로 고정한다 (커밋 scope와 같다).
- 심각도는 `BLOCKER / MAJOR / MINOR`, 상태는 `OPEN / INVESTIGATING / RESOLVED / WORKAROUND / WONTFIX`로만 쓴다.
- 종결하려면 원인란에 "확인됨" 또는 "미확인"을 명시하고, 무엇을 관측해 해결로 판단했는지 적는다. "정상 동작 확인"만으로는 종결하지 않는다.
- `BLOCKER`/`MAJOR`가 미해결로 남아 있으면 `dev` → `main` 머지를 하지 않는다.

### 6.3 README 규칙

- 위치: 레포 루트에 하나만 둔다. 폴더별 README는 만들지 않는다.
- 내용: 현재 루트 `README.md`는 SRS(요구사항 명세서) 본문이다. 리포 구조는 [`docs/FOLDER_STRUCTURE.md`](./docs/FOLDER_STRUCTURE.md), 협업 규칙은 [`COMMIT_RULES.md`](./COMMIT_RULES.md)와 이 문서에 둔다.
- 갱신 시점: 동결 시점마다 갱신한다. 요구사항이 바뀌면 SRS 8장 변경 관리 절차에 따라 개정 이력과 함께 고친다.
