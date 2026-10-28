# assets/

## unity-standard.ico

The standard MindAttic Unity app icon (a Unity-cube/star mark), as a proper multi-resolution
`.ico` with all nine sizes the Windows build normally embeds: 16, 24, 32, 48, 64, 96, 128, 192
and 256 px, each 32-bit.

### Why this file exists

Until now this icon only ever existed as a byte-identical side effect of how certain Unity
Editor projects were built — Unity.Eve, Unity.Cursory, Unity.SpaceGod, Unity.GridGame,
TownWalker (`GridGameCaravan.exe`) and UnityRonin all ship an exe with the exact same
icon resource (SHA256 `5be81b3fa7df7c08cadee6f79d6ff3fe95481420f166ba8127390ea669c8b12c`), but no
repo in the workspace had a checked-in `.ico` asset for it. Nothing enforced the standard, so a
project built a different way — Unity.Chain — shipped with a blank icon instead.

This file was extracted directly from `Unity.Eve.exe`'s `RT_GROUP_ICON`/`RT_ICON` PE resources
(all 9 embedded resolutions, not just the single size a quick `ExtractAssociatedIcon` call would
grab) and reassembled into a standalone `.ico`, so the icon now has one canonical, versioned home.

### How to use it

In each Unity game's Editor: **Edit > Project Settings > Player > Icon** (and the per-platform
icon list, if the target platform splits them out), set this file
(`Unity.Shared/assets/unity-standard.ico`, or a local copy of it) as the icon, unless the game is
intentionally using its own distinct icon.

- **New or rebuilt ports** should set this as their Player Settings icon from the start.
- **Existing games whose icon has drifted** (e.g. Unity.Chain, which currently ships a blank
  icon) should be pointed at this file so they match the rest of the family.

Ticket: MAU-T14.
