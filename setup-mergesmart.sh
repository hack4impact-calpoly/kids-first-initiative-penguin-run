#!/usr/bin/env bash
set -euo pipefail

# Try common Unity Hub locations
CANDIDATES=(
  "/Applications/Unity/Hub/Editor"                 # macOS
  "$HOME/Unity/Hub/Editor"                        # Linux (common)
  "$HOME/Applications/Unity/Hub/Editor"           # sometimes
)

FOUND=""

for base in "${CANDIDATES[@]}"; do
  if [[ -d "$base" ]]; then
    # Look for UnityYAMLMerge inside any installed version
    FOUND="$(find "$base" -maxdepth 5 -type f -name UnityYAMLMerge 2>/dev/null | head -n 1 || true)"
    [[ -n "$FOUND" ]] && break
    FOUND="$(find "$base" -maxdepth 8 -type f -iname "*yaml*merge*" 2>/dev/null | head -n 1 || true)"
    [[ -n "$FOUND" ]] && break
  fi
done

if [[ -z "$FOUND" ]]; then
  echo "Could not find UnityYAMLMerge. Install Unity via Unity Hub or locate the binary manually."
  exit 1
fi

echo "Found UnityYAMLMerge at: $FOUND"

git config merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
git config merge.unityyamlmerge.driver "\"$FOUND\" merge -p %O %B %A %A"

echo "Configured Smart Merge for this repo."

