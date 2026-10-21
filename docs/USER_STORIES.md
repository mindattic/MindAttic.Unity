---
codex: 1
project: Unity.Shared
code: MAU
layer: stories
status: living
updated: 2026-10-19
---
<!-- GENERATED from MindAttic.Tickets/data/Tickets.db by `tickets db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the tickets CLI. -->

# Unity.Shared — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Priority backlog
_Nothing recorded yet._

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| MAU-T1 | 2026-10-19 | docs | closed | Start the permanent ticket log in the workflow database | — |
| MAU-T2 | 2026-10-19 | feature | closed | Create the MindAttic.Unity package (owner 2026-10-19): multiple player profiles, each with its own settings file as in GridGame2026, in ONE common format defined here and reused by every MindAttic Unity game (first users GridGameGaiden and UnitySpace), plus a home for shared Unity helpers. Package: package.json name com.mindattic.unity, consumed by games as "file:../../MindAttic.Unity" in Packages/manifest.json (owner chose local package). Two assemblies: (1) MindAttic.Unity.Core, ENGINE-FREE (noEngineReferences, compiles under plain dotnet netstandard2.1) so games' engine-free Core/Meta layers and their dotnet test projects can use it; (2) MindAttic.Unity.Runtime, thin, supplies Application.persistentDataPath as the root. THE STANDARD, written in README as a versioned spec: <persistentDataPath>/Profiles/current.txt names the profile in use; Profiles/<Name>/settings.json is that profile's ONE settings file (owner chose single JSON over UnitySpace's file-per-setting); Profiles/<Name>/Saves/ holds the game's own save files. settings.json = {"format":"mindattic.settings","version":1,"common":{musicVolume,sfxVolume,muteMusic,muteSfx,reduceMotion,language},"game":{...game-owned...}}. Unknown fields in common and game survive a load/save round-trip; missing file/keys fall back to defaults; damaged JSON is moved aside (settings.json.bad-<timestamp>) and defaults used, never a crash; writes are atomic (temp + replace). Profile API: list, current, select, create (name rules as UnitySpace's ProfileStore.IsValidName: one folder on every platform, <=64 chars), rename, delete = move to Profiles/.trash (HOUSE-LAW-2 soft-disable), root override for tests/automation. JSON: do not ship a second Newtonsoft (GridGameGaiden bundles its own Newtonsoft.Json.dll; check UnitySpace) - choose an approach that works in both (e.g. an injected codec interface) and document it. Port the good parts of UnitySpace's uncommitted Assets/UnitySpace/Core/Persistence/Profiles.cs and GridGame2026's ProfileHelper/FolderHelper (archived repo mindattic/GridGame2026); do NOT edit UnitySpace or GridGameGaiden from this ticket. Repo basics: AGENTS.md/CLAUDE.md forwarders like sibling repos, README per codex-standard/README-STYLE.md, MIT LICENSE, a dotnet NUnit test project for the format (round-trip incl. unknown keys, defaults, damaged file, atomic write, name rules, current/select/create/rename/delete-to-trash, root override), a v1.0.0 tag once green. | tests/Core.Tests (89 passing: JsonTests, SettingsTests.RoundTripKeepsUnknownFieldsInCommonGameAndTopLevel/DamagedFileIsMovedAsideAndDefaultsUsed/SaveIsAtomicAndLeavesNoTempFile/FailedWriteLeavesTheOldFileIntact, ProfileStoreTests, ProfilesFacadeTests.TheOneLineIdentityApiReadsAndWritesNameAndColour); Unity 6000.4.3f1 batchmode smoke; c8b90c0; tag v1.0.0 |
| MAU-T3 | 2026-10-19 | chore | closed | Convert tracked source files to LF line endings via .gitattributes, per HOUSE-LAW-13 (low priority) | git ls-files --eol all i/lf; b57b99a |
| MAU-T4 | 2026-10-20 | docs | open | Publish MindAttic.Unity docs to GitHub Pages via the shared Toolkit DocFX pipeline: run adopt-docs-site.ps1 (README.md as home page, docs/*.md as articles, API reference only where the code builds in CI), enable Pages with build_type=workflow, set the repo homepage URL, retire tools/build-readme.ps1 and README.htm, confirm the docs Actions run is green and https://mindattic.github.io/MindAttic.Unity/ returns 200. Depends on MAT-T8. | — |
| MAU-T5 | 2026-10-20 | chore | closed | Rename MindAttic.Unity repo/folder/namespace to MindAttic.Unity.Shared: its README already describes it as the shared Unity package (player profiles, common helpers) every MindAttic game depends on, distinct from an actual game named just 'Unity'. Update: GitHub repo rename, local folder, git remote, settings.json/Launcher roster Project entries (fixes a duplicate 'MindAttic.Unity' row in the Open Project Tab menu), MindAttic.Unity.Multiplayer's project reference to it, and the README title itself. | — |
| MAU-T6 | 2026-10-20 | chore | closed | Rename MindAttic.Unity.Shared to Unity.Shared: Unity-fleet naming pass applies the same convention as Unity.Multiplayer (a shared Unity library, not a MindAttic-specific project, so it takes the Unity. prefix). Update GitHub repo rename, local folder, git remote, namespace/assembly names, references from Unity.Multiplayer and other consumers, Launcher roster entries, docs/workspace-repos.json and command-catalog.json, and the README title. | — |
| MAU-T7 | 2026-10-21 | chore | closed | Add docs | — |
