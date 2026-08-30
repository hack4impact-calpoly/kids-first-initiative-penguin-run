# Level Progress

`PenguinLevelProgressService` is the runtime owner of level progress. It starts automatically before
the first scene, persists to `PlayerPrefs` under `KFI.PenguinRun.LevelProgress.v1`, and sends
progress snapshots to the website through the active WebGL template.

## Levels

Three levels, identified by scene name. `PenguinLevelIds.TryGetLevelNumber` maps a scene to its
number and tolerates the older scene names, so a rename does not silently stop recording progress:

| Number | Scene                 | Teaches                                     |
| ------ | --------------------- | ------------------------------------------- |
| 1      | `Penguin Run Level 1` | Building a track; gravity pulls things down |
| 2      | `Level2_Friction`     | Friction slows things down                  |
| 3      | `Level3_PE`           | Height stores potential energy              |

Do not renumber a level when a scene or title changes. The website stores completed level numbers,
so renumbering rewrites the meaning of records already saved.

## Website payload

`PenguinProgressWebBridge.Post` calls the WebGL template's `window.postUnityProgress(payload)`. The
template owns the same-origin `unity-progress` message envelope — Unity does not post a second
envelope itself.

Progress is reported as **completed level numbers**. This differs from States of Matter, which
reports named stage IDs, and the website supports both: `GamePlayer` reads `completedLevels` and
`completedStageIds` independently. That difference is why each game needs its own browser-test
coverage rather than one standing in for the other.

Finishing the last level sends a snapshot with `gameCompleted: true`, which is what routes the child
into the post-game quiz. Ordinary progress snapshots never finish the game.

The website's side of this contract is `docs/game-progress-bridge.md` in
`kids-first-initiative-site`.

## Known gap

`Assets/Scripts/EventService.cs` posts level completions directly to `/api/events`. It predates
`PenguinProgressWebBridge` and no longer does anything, but it is **not** simply dead code: the
component is attached to a GameObject in `Penguin Run First Menu.unity` and is enabled. Nothing
invokes its only public method — there is no C# caller and no UnityEvent wiring in that scene — so
it sits there inert.

Removing it means editing that scene as well as deleting the script. Deleting the script alone would
leave a "Missing (Mono Script)" component behind. Worth doing, but as a change someone can open the
editor and verify.

The 362-line `CHANGELOG.md` at the repository root documents this component in detail and never
mentions the bridge that replaced it, which made the repository's most prominent document describe a
path that no longer carries anything. Treat that changelog as history; this file describes what
actually runs.
