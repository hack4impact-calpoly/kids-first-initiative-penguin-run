# Penguin Run

A Unity WebGL learning game for the Kids First Initiative. Children build a track for a penguin
across three levels, learning that gravity pulls things down, friction slows them, and height stores
energy.

The game is embedded in the Kids First Initiative website, which brackets it with a pre-quiz and a
post-quiz so a teacher can see what was learned.

## Requirements

- Unity Hub + Unity 6 (6000.3.1f1)
- Git
- Git LFS (https://git-lfs.com)

Install the editor's **WebGL Build Support** module. Then clone and fetch the large assets:

```sh
git lfs install
git clone https://github.com/hack4impact-calpoly/kids-first-initiative-penguin-run.git
cd kids-first-initiative-penguin-run
git lfs pull
```

Add this folder in Unity Hub and open it with the version in `ProjectSettings/ProjectVersion.txt`.
After import finishes, open `Assets/Scenes/Penguin Run First Menu.unity` and press Play. Check the
enabled scenes in `ProjectSettings/EditorBuildSettings.asset`; older/test scenes are not the release path.
Work from `main`, and preserve Unity `.meta` files with their assets.

## Verify and release

Run `PenguinLevelProgressServiceTests` in Unity's EditMode Test Runner. Play all three levels,
including failure/retry, and check the Console. Verify the visible track-placement guides and the
potential-energy drag rail; a successful compile does not prove either is visible.

Progress is owned by `PenguinLevelProgressService` and sent through `PenguinProgressWebBridge`.
Keep level numbers stable. Local `PlayerPrefs` and website records are separate: verify reloads and
shared-device turnover before promising saved-game recovery for a new learner or device.

Publish through the **website repository's** `build-unity-webgl` workflow, selecting `penguin-run`
and the reviewed source commit. Review its artifact PR and play the actual WebGL build on the
target device before release. A source merge alone does not update the live website.

## Documentation

Start with [`docs/`](docs/):

- [level-progress.md](docs/level-progress.md) — how level completion is tracked and reported to the
  website. Read before renumbering a level.
- [guidance.md](docs/guidance.md) — how the game teaches its controls on screen, and why.

`CHANGELOG.md` is a historical record of the April 2026 save implementation; see the note at its top
before relying on it.

Platform guides: [partner use](https://github.com/hack4impact-calpoly/kids-first-initiative-site/blob/develop/docs/partner-guide.md),
[developer setup](https://github.com/hack4impact-calpoly/kids-first-initiative-site/blob/develop/docs/handbook.md),
[releases](https://github.com/hack4impact-calpoly/kids-first-initiative-site/blob/develop/docs/releases.md),
[ownership and sign-off](https://github.com/hack4impact-calpoly/kids-first-initiative-site/blob/develop/docs/handoff.md).
