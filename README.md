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

Run once after installing Git LFS:

```sh
git lfs install
```

## Documentation

Start with [`docs/`](docs/):

- [level-progress.md](docs/level-progress.md) — how level completion is tracked and reported to the
  website. Read before renumbering a level.
- [guidance.md](docs/guidance.md) — how the game teaches its controls on screen, and why.

`CHANGELOG.md` is a historical record of the April 2026 save implementation; see the note at its top
before relying on it.
