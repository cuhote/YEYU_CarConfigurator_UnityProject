#!/usr/bin/env bash
# One-time setup: point Git at the versioned hooks in .githooks/
# so commit-msg/pre-commit rules from COMMIT_RULES.md are enforced locally.
# Windows에서는 Git Bash에서 실행한다 (Git for Windows에 포함).

set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
chmod +x "$repo_root/.githooks/"* "$repo_root/scripts/"*.sh
git config core.hooksPath .githooks

echo "Git hooks 활성화 완료 (core.hooksPath=.githooks)"
