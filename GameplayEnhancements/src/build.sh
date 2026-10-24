#!/bin/bash
# Builds HomeCheat with the .NET Framework 4 compiler that ships with Windows (C# 5). Git Bash, run from anywhere.
#   bash build.sh core        Core/**/*.cs -> <game>/BepInEx/plugins/HomeCheat.dll   (the game must be closed)
#   bash build.sh <feature>   Features/<Feature>/*.cs -> <game>/BepInEx/homecheat-commands/HomeCheat.Cmd.<feature>.v<yyyyMMddHHmmss>.dll
#                             (in game: op=reload loads it, the older build of the feature is unloaded)
#   bash build.sh all         core (when the game is closed), then every feature
#   bash build.sh selftest    Core/Pure/*.cs + SelfTest/*.cs -> a console exe that runs the logic tests without the game
# Extra references of a feature: one Managed/<name>.dll per line in Features/<Feature>/refs.txt.
# VALHEIM_DIR (Git Bash path) points at another install; default /d/steam/steamapps/common/Valheim.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
G="${VALHEIM_DIR:-/d/steam/steamapps/common/Valheim}"
M="$G/valheim_Data/Managed"
B="$G/BepInEx/core"
CSC=/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe
OUT="${TMPDIR:-/tmp}/homecheat-build"
mkdir -p "$OUT"

BASE_REFS=(-r:"$M/mscorlib.dll" -r:"$M/netstandard.dll" -r:"$M/System.dll" -r:"$M/System.Core.dll"
  -r:"$B/BepInEx.dll" -r:"$B/0Harmony.dll" -r:"$M/assembly_valheim.dll" -r:"$M/SoftReferenceableAssets.dll"
  -r:"$M/UnityEngine.dll" -r:"$M/UnityEngine.CoreModule.dll" -r:"$M/UnityEngine.PhysicsModule.dll")

# csc prints CS0618 (obsolete member) for game APIs; everything else is shown. Exit code is csc's.
compile() {
  local log="$OUT/csc.log"
  set +e
  "$CSC" -nologo -noconfig -nostdlib "$@" > "$log" 2>&1
  local rc=$?
  set -e
  grep -v "CS0618" "$log" || true
  return $rc
}

game_running() { tasklist 2>/dev/null | grep -qi "^valheim.exe"; }

build_core() {
  if game_running; then echo "core: valheim.exe is running; close the game first (the plugin DLL is locked and only loads at start)"; return 1; fi
  local dll="$OUT/HomeCheat.dll"
  rm -f "$dll"
  mapfile -t files < <(find "$HERE/Core" -name '*.cs' | sort | cygpath -w -f -)
  compile -target:library -out:"$dll" "${BASE_REFS[@]}" "${files[@]}"
  mkdir -p "$G/BepInEx/plugins"
  cp "$dll" "$G/BepInEx/plugins/HomeCheat.dll"
  echo "core: built and installed BepInEx/plugins/HomeCheat.dll (${#files[@]} files)"
}

build_feature() {
  local dir=""
  for d in "$HERE"/Features/*/; do
    local n; n="$(basename "$d")"
    if [ "${n,,}" = "${1,,}" ]; then dir="${d%/}"; fi
  done
  if [ -z "$dir" ]; then echo "no feature folder Features/$1 (have: $(ls "$HERE/Features" | tr '\n' ' '))"; return 1; fi
  local name; name="$(basename "$dir")"; name="${name,,}"
  local core="$G/BepInEx/plugins/HomeCheat.dll"
  if [ ! -f "$core" ]; then echo "$name: no core at $core (bash build.sh core first)"; return 1; fi
  local extra=()
  if [ -f "$dir/refs.txt" ]; then
    while IFS= read -r r; do r="${r%$'\r'}"; [ -n "$r" ] && [ "${r:0:1}" != "#" ] && extra+=(-r:"$M/$r"); done < "$dir/refs.txt"
  fi
  local stamp; stamp="$(date +%Y%m%d%H%M%S)"
  local file="HomeCheat.Cmd.$name.v$stamp.dll"
  rm -f "$OUT/$file"
  mapfile -t files < <(find "$dir" -name '*.cs' | sort | cygpath -w -f -)
  compile -target:library -out:"$OUT/$file" "${BASE_REFS[@]}" "${extra[@]}" -r:"$core" "${files[@]}"
  mkdir -p "$G/BepInEx/homecheat-commands"
  cp "$OUT/$file" "$G/BepInEx/homecheat-commands/"
  echo "$name: built and staged $file (op=reload in game)"
}

build_selftest() {
  local exe="$OUT/HomeCheat.SelfTest.exe"
  rm -f "$exe"
  mapfile -t files < <(find "$HERE/Core/Pure" "$HERE/SelfTest" -name '*.cs' | sort | cygpath -w -f -)
  set +e
  "$CSC" -nologo -target:exe -out:"$exe" "${files[@]}"
  local rc=$?
  set -e
  [ $rc -eq 0 ] || return $rc
  "$exe"
}

case "$1" in
  core) build_core ;;
  selftest) build_selftest ;;
  all)
    if game_running; then echo "core: skipped, the game is running (features build against the installed core)"; else build_core; fi
    for d in "$HERE"/Features/*/; do build_feature "$(basename "$d")"; done ;;
  "") echo "usage: bash build.sh core | <feature> | all | selftest"; exit 2 ;;
  *) build_feature "$1" ;;
esac
