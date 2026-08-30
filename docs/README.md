# Penguin Run — documentation

Unity WebGL learning game. Children build a track for a penguin across three levels, learning that
gravity pulls things down, friction slows them, and height stores energy.

| Document                                 | Covers                                                                                             |
| ---------------------------------------- | -------------------------------------------------------------------------------------------------- |
| [level-progress.md](./level-progress.md) | How level completion is tracked and reported to the website. Read this before renumbering a level. |
| [guidance.md](./guidance.md)             | How the game teaches its controls on screen, and why it is built that way.                         |

## Setup

Unity Hub with Unity 6 (6000.3.1f1), Git, and Git LFS. Run `git lfs install` once after installing
LFS. Full requirements are in the [README](../README.md) at the root.

## Related

- `docs/handbook.md` in `kids-first-initiative-site` — how the whole platform fits together
- `docs/game-progress-bridge.md` there — the website's side of the progress contract
- `Docs/game-experience-redesign-proposal.md` in `kids-first-initiative-states-of-matter` — the
  design principles both games follow

## A note on CHANGELOG.md

The changelog at the repository root records an April save-system implementation built around
`EventService.cs`. That component is still attached in the menu scene but nothing invokes it;
progress now flows through `PenguinProgressWebBridge`. Read the changelog as history, and
`level-progress.md` for what runs today.
