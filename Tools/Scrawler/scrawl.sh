#!/usr/bin/env bash
# Runs the Scrawler player in the Unity Editor that has this project open, and prints its log.
# See README.md.
#
#   Tools/Scrawler/scrawl.sh official [--wpm 85|120]
#   Tools/Scrawler/scrawl.sh practice [--wpm 85|120] [--level N] [--health N] [--damage N] [--reach N] [--sight N] [--speed X] [--difficulty 1|2|3]

set -u

here="$(cd "$(dirname "$0")" && pwd)"
project="$(cd "$here/../.." && pwd)"

mode="${1:-official}"
[ $# -gt 0 ] && shift
wpm=120 level=1 health=5 damage=1 reach=1 sight=3 speed=1 difficulty=3
while [ $# -gt 0 ]; do
  case "$1" in
    --wpm) wpm="$2"; shift 2 ;;
    --level) level="$2"; shift 2 ;;
    --health) health="$2"; shift 2 ;;
    --damage) damage="$2"; shift 2 ;;
    --reach) reach="$2"; shift 2 ;;
    --sight) sight="$2"; shift 2 ;;
    --speed) speed="$2"; shift 2 ;;
    --difficulty) difficulty="$2"; shift 2 ;;
    *) echo "unknown option: $1"; exit 2 ;;
  esac
done

u() { unity command --project-path "$project" "$@"; }

case "$mode" in
  official)
    scene="Assets/Scenes/SplashScreen.unity"
    entry="Scrawler.Official"
    args="[\"$wpm\"]"
    ;;
  practice)
    scene="Assets/Scenes/Game.unity"
    entry="Scrawler.Practice"
    args="[\"$speed\",\"$level\",\"$health\",\"$damage\",\"$reach\",\"$sight\",\"$wpm\",\"$difficulty\"]"
    ;;
  *)
    echo "usage: $0 official|practice [options]"
    exit 2
    ;;
esac

u editor.playmode stop > /dev/null 2>&1
sleep 1
if ! u scene open --path "$scene" > /dev/null 2>&1; then
  echo "Could not open $scene. Save or discard changes in the open scene and try again."
  exit 1
fi

u console clear > /dev/null 2>&1
u editor.playmode start > /dev/null 2>&1

out=""
for _ in $(seq 1 30); do
  sleep 1
  out="$(u script run --file "$here/Scrawler.cs" --entry "$entry" --args "$args" --result-only 2>&1)"
  echo "$out" | grep -q '"success": *true' && break
done

if ! echo "$out" | grep -q '"success": *true'; then
  echo "Could not start the player:"
  echo "$out"
  exit 1
fi

echo "$out" | grep -o '"result": *"[^"]*' | sed 's/"result": *"//'

# Follow the player's log until the run ends.
cursor=0
while :; do
  sleep 2
  out="$(u console get --since "$cursor" --result-only 2>/dev/null)" || continue
  echo "$out" | grep -o '"message": *"\[SCRAWL\][^"]*' | sed 's/"message": *"\[SCRAWL\] //'
  next="$(echo "$out" | grep -o '"cursor": *[0-9]*' | grep -o '[0-9]*$')"
  [ -n "$next" ] && cursor="$next"
  echo "$out" | grep -q 'game won\|DIED on level' && break
done
