#!/bin/bash
# Inspect the index only. Keep this independent of ignored local test logs.
set -eu
failed=0
while IFS= read -r -d '' path; do
    case "$path" in
        Assets/Art/Environment/VarcoDetailBaked/*|ArtSource/VarcoDetail20261014/*|Assets/Art/ModelQuality20261014/*|ArtSource/ModelQuality20261014/*|Assets/Art/Environment/BlockoutReplacement20261014/*|ArtSource/BlockoutReplacement20261014/*)
            bytes=$(git cat-file -s ":$path")
            if (( bytes > 52428800 )); then
                printf 'Commit blocked: refined prop exceeds 50 MiB ordinary Git budget: %s\n' "$path" >&2
                failed=1
            fi
            if (( bytes < 1024 )) && git show ":$path" | head -n 1 | grep -q '^version https://git-lfs.github.com/spec/v1'; then
                printf 'Commit blocked: new refined props must not use LFS: %s\n' "$path" >&2
                failed=1
            fi ;;
    esac
    case "$path" in
        ArtSource/HandRefinement20261013/Output/*|ArtSource/HandRefinement20261013/Preview/*|ArtSource/EnvironmentRefresh20261013/Downloads/*|ArtSource/EnvironmentRefresh20261013/preview.png|ArtSource/CharacterDetail20261013/Downloads/*|ArtSource/CharacterDetail20261013/Output/*|ArtSource/CharacterDetail20261013/Preview/*|*.blend[0-9]*|Logs/*|Library/*|Temp/*|Builds/*|PlaytestLogs/*|*/__pycache__/*|ArtSource/CommandDesk20261013/*-preview.png|ArtSource/RoleActivities20261013/Input/*|ArtSource/RoleActivities20261013/Output/*|ArtSource/RoleActivities20261013/Preview/*)
            printf 'Commit blocked: intermediate or backup file: %s\n' "$path" >&2
            failed=1 ;;
    esac
    if [[ "$path" == Assets/* && "$path" != *.meta ]]; then
        if ! git cat-file -e ":$path.meta" 2>/dev/null; then
            printf 'Commit blocked: stage the Unity .meta too: %s\n' "$path" >&2
            failed=1
        fi
    fi
done < <(git diff --cached --name-only --diff-filter=ACMR -z)
exit "$failed"
