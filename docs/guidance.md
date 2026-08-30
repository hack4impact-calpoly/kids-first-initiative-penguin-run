# On-screen guidance

The audience is young children, many still learning to read. Guidance therefore has to be shown
rather than explained, and it has to arrive at the moment it is needed rather than as a wall of
instruction up front.

Two systems do this, and both are deliberately quiet: they appear when a child has something to do,
and retire once the child has done it.

## AttentionHighlight

A soft pulsing glow attached to anything a child can interact with. Ported unchanged from the States
of Matter wiring puzzle, so a child moving between the two games meets one consistent idea: **a
thing that glows is a thing you can use.**

It builds its own radial sprite at runtime, so it needs no art asset, and works on both world
`SpriteRenderer`s and UI `Graphic`s. It sizes itself from the bounds of the renderer it is attached
to — which means a bare `Transform` with no sprite produces no glow, and needs an invisible one to
measure against.

## TrackPlacementGuidance

Building a track has two steps a child has to discover unaided: that pieces are dragged out of the
tray, and that they connect at specific points. Neither was visible — the tray looked like
decoration, and snap points existed only as an editor gizmo a player never sees.

| State                    | What glows                       | Why                           |
| ------------------------ | -------------------------------- | ----------------------------- |
| Nothing placed yet       | The tray                         | That is where a track starts  |
| A piece is being dragged | Connection points it could reach | The step a child cannot guess |
| A piece has connected    | Nothing                          | They have understood it       |

Two details are deliberate. Reachable points start glowing a little **beyond** the true snap radius,
so a child sees where to aim before they are already on top of it — glowing only at the exact snap
distance would confirm what is about to happen rather than teach where to go. And only the nearest
few glow at once, because lighting up every connection in the level is noise rather than guidance.

The finished state persists per scene, the same way `SlideLessonCard` remembers its dismissal, so
guidance does not nag on a repeat visit. Guidance also stands down entirely while a dialogue is
open — that already has the child's attention.

## The Potential Energy drag rail

`Level3_PE` asks a child to drag Pip up a ramp to set his height, and height _is_ the potential
energy the level teaches. The rail Pip snaps to is an `EdgeCollider2D` with no renderer, so nothing
showed it existed.

`PipLauncher` now draws a dashed line along that exact segment. The line is split at Pip — brighter
above him, dimmer below — and that gradient is information rather than decoration: it shows how much
height, and therefore how much potential energy, is still available. Dashes drift up the ramp to
suggest the drag direction and hold still while the child is dragging, so the guide does not fight
their own movement. It fades out on launch, when the rail is no longer actionable.

## Where the approach comes from

The design principles behind all of this are written up in
`Docs/game-experience-redesign-proposal.md` in the States of Matter repository — notably "show the
goal within five seconds", "teach one new interaction at a time", and "use shape, symbols, animation,
and text in addition to colour".

## Not verified on screen

All three were added with a batchmode compile as the only check. Whether the glow reads clearly, and
whether the pulse rate and preview margins feel right to a child, needs someone to play the levels.
It is part of the device QA pass in `docs/accessibility-qa.md` in the website repository.
