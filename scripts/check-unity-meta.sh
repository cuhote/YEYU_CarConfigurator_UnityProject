#!/usr/bin/env bash
# Assets/ 아래 에셋·폴더와 .meta 파일의 짝이 맞는지 Git 인덱스 기준으로 검사한다.
# (CONVENTIONS.md §3.3)
#   - 에셋 파일·폴더에는 같은 이름의 .meta가 함께 커밋돼 있어야 한다.
#   - .meta만 남고 원본 에셋·폴더가 없으면 안 된다.
# .githooks/pre-commit 과 .github/workflows/meta-check.yml 이 같은 스크립트를 쓴다.

set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

# Unity 프로젝트를 만들기 전(폴더 뼈대와 .gitkeep만 있는 상태)에는 .meta가 하나도
# 없는 것이 정상이므로 검사하지 않는다.
if [[ ! -f ProjectSettings/ProjectVersion.txt ]]; then
  echo "check-unity-meta: ProjectSettings/ProjectVersion.txt가 없어 건너뜁니다 (Unity 프로젝트 생성 전)."
  exit 0
fi

# Unity는 이름이 '.'으로 시작하거나 '~'로 끝나는 파일·폴더를 임포트하지 않는다
# (.gitkeep 등). 이런 경로에는 .meta가 생기지 않는다.
is_ignored() {
  local p="/$1"
  [[ "$p" == */.* || "$p" == *~ || "$p" == *~/* ]]
}

declare -A tracked=()
declare -A dirs=()

while IFS= read -r -d '' f; do
  tracked["$f"]=1
  d="${f%/*}"
  while [[ "$d" == Assets/* ]]; do
    [[ -n "${dirs[$d]:-}" ]] && break
    dirs["$d"]=1
    d="${d%/*}"
  done
done < <(git ls-files -z -- Assets)

missing=()
orphan=()

for f in "${!tracked[@]}"; do
  is_ignored "$f" && continue
  if [[ "$f" == *.meta ]]; then
    target="${f%.meta}"
    if [[ -z "${tracked[$target]:-}" && -z "${dirs[$target]:-}" ]]; then
      orphan+=("$f")
    fi
  elif [[ -z "${tracked[$f.meta]:-}" ]]; then
    missing+=("$f.meta")
  fi
done

for d in "${!dirs[@]}"; do
  is_ignored "$d" && continue
  if [[ -z "${tracked[$d.meta]:-}" ]]; then
    missing+=("$d.meta")
  fi
done

if [[ ${#missing[@]} -eq 0 && ${#orphan[@]} -eq 0 ]]; then
  echo "check-unity-meta: 에셋과 .meta 짝이 모두 맞습니다."
  exit 0
fi

if [[ ${#missing[@]} -gt 0 ]]; then
  echo "check-unity-meta: 커밋되지 않은 .meta (${#missing[@]}개)" >&2
  printf '%s\n' "${missing[@]}" | sort | sed 's/^/  + /' >&2
fi
if [[ ${#orphan[@]} -gt 0 ]]; then
  echo "check-unity-meta: 원본 에셋·폴더가 없는 .meta (${#orphan[@]}개)" >&2
  printf '%s\n' "${orphan[@]}" | sort | sed 's/^/  - /' >&2
fi
exit 1
