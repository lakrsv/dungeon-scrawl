#!/usr/bin/env bash
# Runs a Probe method in the playing Editor and prints its result.
#   Tools/Probe/probe.sh <Method> [args...]
# e.g. Tools/Probe/probe.sh Type "kick" 0.12
here="$(cd "$(dirname "$0")" && pwd)"
project="$(cd "$here/../.." && pwd)"
method="$1"
shift
args=()
if [ $# -gt 0 ]; then
  args=(--args "$(python -c 'import json,sys; print(json.dumps(sys.argv[1:]))' "$@")")
fi
unity command script run --file "$here/Probe.cs" --entry "Probe.$method" "${args[@]}" \
  --project-path "$project" --caller plugin --skill unity-cli --no-banner --result-only 2>&1 \
  | python -c "import json,sys; raw=sys.stdin.read(); d=json.loads(raw) if raw.strip().startswith('{') else None; print(d.get('result') if d and d.get('success') else raw[:3000])"
