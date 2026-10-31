#!/usr/bin/env bash
# One command to check the whole project without opening Unity.
#   bash Tools/run_all.sh
# Runs: rules tests, the button-level flow check, and the compile against real Unity assemblies.
# Prints one PASS/FAIL line per step and exits non-zero if any step failed.
set -u
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/.." && pwd)"
cd "$REPO"
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
failed=0

echo "== 1/3 rules tests (Tools/LogicTests) =="
out="$(dotnet run --project Tools/LogicTests -c Release 2>&1)"
echo "$out" | grep -E "PASS [0-9]+ +FAIL [0-9]+" || echo "$out" | tail -5
echo "$out" | grep -qE "FAIL 0 " && echo "PASS: rules tests" || { echo "FAIL: rules tests"; failed=1; }

echo "== 2/3 flow check (Tools/FlowCheck): desk, patrol, hunt =="
out="$(dotnet run --project Tools/FlowCheck -c Release 2>&1)"
bad="$(echo "$out" | grep -c '^  BAD')"
ok="$(echo "$out" | grep -c '^  OK')"
echo "$ok OK, $bad BAD"
if echo "$out" | grep -q "FLOW CHAIN COMPLETE" && [ "$bad" = "0" ]; then echo "PASS: flow check"; else echo "FAIL: flow check"; echo "$out" | grep '^  BAD' | head -10; failed=1; fi

echo "== 3/3 compile against real Unity assemblies (Tools/Verify, downloads once) =="
if bash Tools/Verify/verify.sh 2>&1 | tail -25 | grep -q "0 Error(s)"; then echo "PASS: Unity compile"; else echo "FAIL (or could not download): Unity compile - run 'bash Tools/Verify/verify.sh' to see why"; failed=1; fi

[ "$failed" = "0" ] && echo "ALL CHECKS PASSED" || echo "SOMETHING FAILED (see above)"
exit "$failed"
