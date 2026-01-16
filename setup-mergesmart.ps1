$ErrorActionPreference = "Stop"

$roots = @(
  "C:\Program Files\Unity\Hub\Editor",
  "$env:LOCALAPPDATA\Unity\Hub\Editor"
)

$found = $null

foreach ($root in $roots) {
  if (Test-Path $root) {
    $found = Get-ChildItem -Path $root -Recurse -Filter "UnityYAMLMerge.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { break }
  }
}

if (-not $found) {
  Write-Host "Could not find UnityYAMLMerge.exe. Install Unity via Unity Hub or locate it manually."
  exit 1
}

$path = $found.FullName
Write-Host "Found UnityYAMLMerge at: $path"

git config merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
git config merge.unityyamlmerge.driver "`"$path`" merge -p %O %B %A %A"

Write-Host "Configured Smart Merge for this repo."

