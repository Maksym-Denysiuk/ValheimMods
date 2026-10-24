#!/bin/bash
# usage: bash build-cmd.sh <name> [extra.cs ...]
# Compiles Cmd_<name>.cs (+ extras) against the core HomeCheat.dll into a uniquely named command DLL and
# stages it in BepInEx/homecheat-commands/. In game: op=reload loads it (no restart). Own DLL per command,
# so sessions never overwrite each other. Run from this folder.
set -e
NAME="$1"; shift || true
HERE="$(cd "$(dirname "$0")" && pwd)"
G="${VALHEIM_DIR:-/d/steam/steamapps/common/Valheim}"   # set VALHEIM_DIR (Git Bash path) for another install
M=$G/valheim_Data/Managed
B=$G/BepInEx/core
OUT="HomeCheat.Cmd.${NAME}.v$(date +%H%M%S).dll"
/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe -nologo -noconfig -nostdlib -target:library \
  -out:"$HERE/$OUT" \
  -r:$M/mscorlib.dll -r:$M/netstandard.dll -r:$M/System.dll -r:$M/System.Core.dll \
  -r:$B/BepInEx.dll -r:$B/0Harmony.dll -r:$M/assembly_valheim.dll -r:$M/SoftReferenceableAssets.dll \
  -r:$M/UnityEngine.dll -r:$M/UnityEngine.CoreModule.dll -r:$M/UnityEngine.PhysicsModule.dll \
  -r:"$HERE/HomeCheat.dll" \
  "$HERE/Cmd_${NAME}.cs" "$@" 2>&1 | grep -v CS0618 || true
test -f "$HERE/$OUT" && cp "$HERE/$OUT" $G/BepInEx/homecheat-commands/ && echo "built and staged: $OUT"
