# Commit & Branch Rules

> 이 문서는 팀 YEYU가 합의한 Git 작업 규칙이다. 별도의 외부 규정 문서(PDF 등)는 없으며, 이 문서 자체가 원본이다. 규칙을 바꾸려면 6절 변경 절차를 따른다.
> 네이밍·파일 관리·폴더 담당은 [`CONVENTIONS.md`](./CONVENTIONS.md), 리포 레이아웃과 파일별 역할은 [`docs/FOLDER_STRUCTURE.md`](./docs/FOLDER_STRUCTURE.md)를 본다.

## 1. 브랜치 전략 [확정]

| 브랜치 | 머지 방향 | 용도 |
|---|---|---|
| `main` | ← `dev` | 발표·제출용 안정 버전. 동결 시점에 태그를 붙인다 |
| `dev` | ← `feature/*` 등 | 통합 브랜치. 매일 1회 이상 빌드 성공 상태를 유지한다 (SRS NFR-08) |
| `feature/<기능명>` | → `dev` | 기능 개발 (`feature/night-vision`, `feature/valet-parking`) |
| `fix/<내용>` | → `dev` | 버그 수정 (`fix/split-screen-ratio`) |
| `docs/<문서명>` | → `dev` | 문서 작업 (`docs/srs-v0-3`) |
| `chore/<내용>` | → `dev` | 빌드·설정 변경 (`chore/gitignore-update`) |

**절대 규칙: `main`에 직접 커밋하지 않는다.** 항상 `feature/`, `fix/`, `docs/`, `chore/` 중 하나로 브랜치를 만들고, `dev`를 거쳐 PR로만 `main`에 반영한다.

브랜치명 형식: 소문자 + 하이픈 (`feature/night-vision`). 브랜치명 자체에는 한글을 쓰지 않는다.

작업 흐름: `dev`에서 브랜치 생성 → 작업 → `dev`로 Pull Request → 상대 확인 후 병합.

## 2. 커밋 메시지 [확정]

형식:

```
<type>(<scope>): <subject>
```

- `type`, `scope`는 영문으로 쓴다.
- `scope`는 선택이며 아래 목록 중 하나만 쓴다. 리포 구조(`Assets/_Project/Scripts/` 하위 폴더)와 1:1로 대응한다.
- 테스트 결과 기록 커밋은 `scope`에 SRS 7장의 테스트 케이스 ID(`TC-` + 두 자리, 예: `TC-07`)를 넣는다.
- `subject`는 **한글로**, 명령형·서술형으로 간결하게 쓰고 마침표를 붙이지 않는다.

| type | 설명 | 예시 |
|---|---|---|
| `feat` | 새 기능 추가 | `feat(driving): 나이트 비전 TTC 감속 로직 추가` |
| `fix` | 버그 수정 | `fix(camera): 분할 화면 카메라 비율 오류 수정` |
| `docs` | 문서 변경 | `docs: SRS FR-21 인수 기준 수정` |
| `test` | 테스트 수행·결과 기록 | `test(TC-07): 미적용 차량 급제동 정지 거리 결과 기록` |
| `refactor` | 동작 변화 없는 구조 변경 | `refactor(core): CarConfig 필드 이름 정리` |
| `chore` | 빌드·설정·의존성 | `chore(settings): 레이어·태그 추가` |

### scope 목록

| scope | 대상 |
|---|---|
| `core` | `Scripts/Core`, `MainScene.unity` (영역 전환, `CarConfig`) |
| `vehicle` | `Scripts/Vehicle`, `Prefabs/Vehicle` (차량 프리팹, `CarController`, `ICarFeature`) |
| `camera` | `Scripts/Camera` (분할 화면, 시점 전환, 쇼룸 카메라) |
| `scenario` | `Scripts/Scenario` (시나리오 공통 틀) |
| `ui` | `Scripts/UI`, `Prefabs/UI`, `_Project/UI` (공통 Canvas 패널, 아이콘, 폰트) |
| `utils` | `Scripts/Utils` |
| `driving` | `Scripts/Driving`, `Prefabs/Driving`, `DrivingArea.unity` (나이트 비전, 후륜 조향) |
| `parking` | `Scripts/Parking`, `Prefabs/Parking`, `ParkingArea.unity` (발렛, 원격 주차) |
| `configurator` | `ConfiguratorArea.unity` (쇼룸, 외관·기능 선택 영역) |
| `assets` | `Data`, `Materials`, `Shaders`, `RenderTextures`, `Audio`, `Assets/ThirdParty` |
| `settings` | `ProjectSettings/`, `Packages/` (레이어·태그, 빌드 설정, 패키지) |
| `docs` | `docs/` 문서 작업 (이때는 `type`도 보통 `docs`) |
| `repo` | `.gitignore`, `.gitattributes`, `.githooks/`, `.github/`, `scripts/` |

## 3. PR 및 머지 [확정]

| 구간 | 조건 |
|---|---|
| `feature/*` 등 → `dev` | PR 생성 필수. Unity에서 ▶ Play로 실행 확인. PR 본문에 작업 내용과 확인 방법 기재. 상대 확인 후 병합 |
| `dev` → `main` | PR 생성 필수. 해당 동결 기준 충족, Windows 빌드 성공. 머지 직후 동결 태그 부착 |

- 하나의 PR은 하나의 기능 또는 하나의 수정만 담는다.
- 공통 파일(`MainScene.unity`, `ConfiguratorArea.unity`, 공통 스크립트·프리팹, `ProjectSettings/`)을 고친 PR은 **수정 전에 상대에게 알리고**, 수정 후 바로 `dev`에 병합한다 (SRS 8장 5항). 오래 열어 두면 씬·프리팹 충돌이 난다.
- 되돌리기: 머지 후 동작이 깨지면 추가 수정보다 revert를 우선한다. `dev`가 빌드 성공 상태를 유지하는 것이 최우선이다.

## 4. 동결과 태그 [제안]

아래 표는 [`docs/WBS.md`](./docs/WBS.md) 마일스톤을 기준으로 한 초안이다. 팀 회의에서 확정하고 `[확정]`으로 바꾼다.

| 시점 | 마일스톤 | 동결 대상 | 태그 |
|---|---|---|---|
| 1차 (10/7) | M2 공통 기반 완성 | `CarConfig` 필드, `AreaBase`·`ICarFeature`·`CarController` 공개 메서드 이름, 영역 좌표, 레이어·태그 | `v0.1` |
| 2차 (10/11) | M4 기능 4종 완료 | 기능 컴포넌트 구성, 시나리오 트리거 위치 | `v0.2` |
| 3차 (10/12) | M5 시나리오 완성 | 시나리오 흐름, UI 화면 구성 | `v0.3` |
| 최종 (10/13) | M6 최종 빌드 | 전체 (제출본) | `v1.0` |

동결 이후 변경 시 6절 변경 절차를 따르고, 영향 받는 파일과 테스트 케이스 ID를 커밋 메시지에 함께 기록한다.

## 5. 강제(Git Hooks) 적용 방법

이 저장소에는 위 규칙을 강제하는 훅이 `.githooks/`에 들어 있다. 훅은 저장소 기본 `.git/hooks/`가 아니라 버전 관리되는 `.githooks/`에 있으므로, **클론 후 한 번** 아래 명령으로 활성화해야 한다. Windows에서는 Git Bash에서 실행한다.

```bash
./scripts/setup-git-hooks.sh
# 또는 직접
git config core.hooksPath .githooks
```

활성화하면:

- **`commit-msg`**: 커밋 메시지가 `<type>(<scope>): <subject>` 형식과 위 규칙(type 목록, scope 목록, subject 한글·마침표 금지)을 지키지 않으면 커밋을 거부한다.
- **`pre-commit`**: 현재 브랜치가 `main`이면 커밋을 거부하고, `feature/fix/docs/chore` 브랜치를 만들라는 안내를 출력한다. 브랜치명이 `feature/`, `fix/`, `docs/`, `chore/` 접두어 형식(소문자+하이픈)을 벗어나면 경고한다. 또한 `Assets/` 아래 파일이 스테이징돼 있으면 `scripts/check-unity-meta.sh`를 돌려, 에셋·폴더에 `.meta`가 빠졌거나 원본 없이 `.meta`만 남아 있으면 커밋을 거부한다 ([`CONVENTIONS.md`](./CONVENTIONS.md) 3.3절).

로컬 훅은 스크립트를 실행한 PC에서만 동작한다. 같은 `.meta` 검사를 GitHub Actions `meta-check`(`.github/workflows/meta-check.yml`)가 `dev`·`main`으로 가는 push와 PR마다 다시 돌린다. `pull_request` 트리거는 PR 브랜치와 대상 브랜치를 합친 상태를 검사하므로, 각자 브랜치에서는 정상이지만 합치면 어긋나는 경우도 병합 전에 걸린다.

`.meta` 검사는 Unity 프로젝트를 만들기 전(`ProjectSettings/ProjectVersion.txt`가 없는 상태)에는 건너뛴다.

훅 스크립트를 수정해야 하면 `.githooks/commit-msg`, `.githooks/pre-commit`, `scripts/check-unity-meta.sh`, `.github/workflows/meta-check.yml`을 고치고, 변경 사유를 6절 변경 절차에 따라 기록한다.

## 6. 규칙 변경 절차

1. **제안**: 무엇을 어떤 이름으로 바꾸는지, 왜 지금 바꿔야 하는지 한 줄로 적는다. 취향 차이는 사유가 되지 않는다.
2. **확인**: 해당 이름/scope를 쓰는 스크립트, 씬, 프리팹, 인스펙터 참조, 훅 스크립트를 모두 찾는다. 하나라도 빠지면 실행 시점에 깨진다.
3. **반영**: 코드와 이 문서, [`CONVENTIONS.md`](./CONVENTIONS.md), [`docs/FOLDER_STRUCTURE.md`](./docs/FOLDER_STRUCTURE.md)를 같은 PR에서 함께 고친다. 문서를 나중에 고치면 반드시 어긋난다.
4. **기록**: 커밋 메시지에 영향 받는 범위와 관련 테스트 케이스 ID를 적는다.

요구사항(SRS) 자체의 변경은 [`README.md`](./README.md) 8장 변경 관리를 따른다.
