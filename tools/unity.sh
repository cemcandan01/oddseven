#!/usr/bin/env bash
# Batch-mode wrapper for the WeirdLand Unity project.
# Usage: tools/unity.sh <setup|prefabs|tests [editmode|playmode|all]>
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_EXE="${UNITY_EXE:-/c/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe}"

winpath() {
    if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else echo "$1"; fi
}

PROJECT="$ROOT/unity"
LOGS="$PROJECT/Logs"

usage() {
    echo "Usage: tools/unity.sh <setup|prefabs|tests [editmode|playmode|all]>" >&2
    exit 64
}

preflight() {
    if [ ! -f "$PROJECT/Assets/Plugins/WeirdLand/WeirdLand.Core.dll" ]; then
        echo "WeirdLand.Core.dll missing, building core..."
        dotnet build "$ROOT/core" || exit 1
    fi
    if tasklist 2>/dev/null | grep -qi "^Unity.exe" && [ -f "$PROJECT/Temp/UnityLockfile" ]; then
        echo "Unity editörü bu projeyi açık tutuyor, kapatıp tekrar dene."
        exit 1
    fi
    mkdir -p "$LOGS"
}

cmd_setup() {
    # No -quit: ProjectSetup.Run exits the editor itself once the TMP package import finishes.
    "$UNITY_EXE" -batchmode -projectPath "$(winpath "$PROJECT")" \
        -executeMethod WeirdLand.Unity.EditorTools.ProjectSetup.Run \
        -logFile "$(winpath "$LOGS/setup.log")"
    local code=$?
    if [ $code -ne 0 ]; then
        echo "setup failed (exit $code). Last 40 log lines:"
        tail -n 40 "$LOGS/setup.log"
    fi
    return $code
}

cmd_prefabs() {
    "$UNITY_EXE" -batchmode -projectPath "$(winpath "$PROJECT")" \
        -executeMethod WeirdLand.Unity.EditorTools.PrefabBuilder.BuildAll -quit \
        -logFile "$(winpath "$LOGS/prefabs.log")"
    local code=$?
    if [ $code -ne 0 ]; then
        echo "prefabs failed (exit $code). Last 40 log lines:"
        tail -n 40 "$LOGS/prefabs.log"
    else
        echo "prefabs: built (log: $LOGS/prefabs.log)"
    fi
    return $code
}

# Reads one attribute from the root <test-run> element of a results file.
run_attr() {
    grep -m1 -o "<test-run [^>]*" "$1" | grep -o " $2=\"[^\"]*\"" | head -n1 | sed 's/.*="\(.*\)"/\1/'
}

run_platform() {
    local platform="$1"
    local lower
    lower="$(echo "$platform" | tr '[:upper:]' '[:lower:]')"
    local xml="$LOGS/tests-$lower.xml"
    local log="$LOGS/tests-$lower.log"
    rm -f "$xml"
    "$UNITY_EXE" -batchmode -projectPath "$(winpath "$PROJECT")" -runTests -testPlatform "$platform" \
        -testResults "$(winpath "$xml")" -logFile "$(winpath "$log")"
    local code=$?
    # Unity: 0 all passed, 2 some tests failed, 3 run error. Anything else
    # (e.g. 1 for "Scripts have compiler errors") is reported as a run error.
    if [ $code -ne 0 ] && [ $code -ne 2 ]; then code=3; fi
    if [ -f "$xml" ]; then
        echo "$platform: $(run_attr "$xml" passed) passed, $(run_attr "$xml" failed) failed, $(run_attr "$xml" skipped) skipped"
        grep -o "<test-case [^>]*result=\"Failed\"[^>]*" "$xml" \
            | grep -o " fullname=\"[^\"]*\"" | sed 's/ fullname="\(.*\)"/  FAILED: \1/'
    else
        echo "$platform: no test results written"
        [ $code -eq 0 ] && code=3
    fi
    if [ $code -eq 3 ]; then
        echo "Run error. Last 40 lines of $log:"
        tail -n 40 "$log"
    fi
    return $code
}

cmd_tests() {
    local which="${1:-all}"
    local platforms
    case "$which" in
        editmode) platforms="EditMode" ;;
        playmode) platforms="PlayMode" ;;
        all) platforms="EditMode PlayMode" ;;
        *) usage ;;
    esac
    local result=0
    for p in $platforms; do
        run_platform "$p"
        local code=$?
        if [ $code -ne 0 ] && [ $result -eq 0 ]; then result=$code; fi
    done
    return $result
}

[ $# -ge 1 ] || usage
command="$1"; shift
case "$command" in
    setup) preflight; cmd_setup; exit $? ;;
    prefabs) preflight; cmd_prefabs; exit $? ;;
    tests) preflight; cmd_tests "$@"; exit $? ;;
    *) usage ;;
esac
