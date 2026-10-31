#!/usr/bin/env bash
# Saves the Editor's Game view at 1920x1080 to a PNG.
#   Tools/Probe/capture.sh out.png
here="$(cd "$(dirname "$0")" && pwd)"
project="$(cd "$here/../.." && pwd)"
unity command capture game_view --width 1920 --height 1080 --project-path "$project" --caller plugin --skill unity-cli --result-only 2>/dev/null \
  | python -c "import json,sys,base64; d=json.load(sys.stdin); open(sys.argv[1],'wb').write(base64.b64decode(d['base64'])); print(sys.argv[1], d['width'], d['height'])" "$1"
