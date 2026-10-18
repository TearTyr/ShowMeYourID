#!/usr/bin/env bash
# One-shot replication of the Unity editor's compile + the static audit.
# Usage:  bash Tools/Verify/verify.sh [path/to/Assets]
set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
GAME_SRC="${1:-$REPO/Assets}"
LIBS_ALL="${UNITY_LIBS_ALL:-/tmp/unitylibs}"
LIBS_MIN="${UNITY_LIBS_MIN:-/tmp/unitylibs_min}"

echo "======================================================================"
echo " Show Me Your ID! — verification harness"
echo "   project : $REPO"
echo "   sources : $GAME_SRC"
echo "======================================================================"

# ---------------------------------------------------------------- 1. compiler
DOTNET="${DOTNET:-$(command -v dotnet || true)}"
if [ -z "$DOTNET" ]; then
  cat <<'MSG'
[1/4] .NET SDK not found.

    The harness needs Roslyn (any .NET 6/8/9 SDK works — it is only a compiler,
    it does NOT replace Unity). Install it once:

      # Linux/macOS
      mkdir -p /tmp/dotnet && curl -L -o /tmp/sdk.tgz \
        https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.404/dotnet-sdk-8.0.404-linux-x64.tar.gz
      tar xzf /tmp/sdk.tgz -C /tmp/dotnet
      export DOTNET_ROOT=/tmp/dotnet PATH=/tmp/dotnet:$PATH

      # Windows (PowerShell)
      #   winget install Microsoft.DotNet.SDK.8

    then re-run:  DOTNET=/path/to/dotnet bash Tools/Verify/verify.sh
MSG
  exit 2
fi
echo "[1/4] compiler: $("$DOTNET" --version 2>/dev/null || echo present)"

# ------------------------------------------------------- 2. real Unity assemblies
if [ ! -f "$LIBS_MIN/UnityEngine.UI.dll" ]; then
  echo "[2/4] fetching real Unity 2022.3.62f1 assemblies ..."
  python3 "$HERE/fetch_unity_libs.py" "$LIBS_MIN" >/dev/null || { echo "  fetch failed"; exit 2; }
else
  echo "[2/4] Unity assemblies cached in $LIBS_MIN"
fi
if [ ! -f "$LIBS_ALL/UnityEngine.UI.dll" ]; then
  python3 "$HERE/fetch_unity_libs.py" "$LIBS_ALL" --all >/dev/null || true
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

build () { # $1 = label, $2... = extra msbuild props
  local label="$1"; shift
  echo
  echo "--- $label ---"
  "$DOTNET" build "$HERE/Assembly-CSharp.csproj" -v q --nologo -p:GameSrc="$GAME_SRC" "$@" 2>&1 \
    | grep -E "error|warning CS|Build succeeded|Error\(s\)|Warning\(s\)" \
    | sed -e "s|$GAME_SRC/||" -e 's| \[.*csproj\]||'
  return 0
}

# ---------------------------------------------------------- 3. compile (two ways)
build "[3/4] compile vs MINIMAL reference set (default Unity modules + uGUI only)" \
      -p:UnityLibs="$LIBS_MIN"
build "      compile vs FULL Unity assembly set (76 assemblies, widest coverage)" \
      -p:UnityLibs="$LIBS_ALL"

# ------------------------------------------------------------------ 4. audit
echo
echo "[4/4] static audit"
python3 "$HERE/audit.py" "$GAME_SRC"
AUDIT=$?

echo
echo "======================================================================"
if [ $AUDIT -eq 0 ]; then
  echo " RESULT: compile clean + audit clean -> this code compiles in the Unity editor."
else
  echo " RESULT: audit reported findings above (see [ERROR-RISK] lines)."
fi
echo "======================================================================"
exit $AUDIT
