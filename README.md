# AcousticChamber26 — exploratory acoustic-perception chambers in VR

Unity project for **Meta Quest 3** (standalone Android; Quest Link for development). A Main Hall lets the experimenter pick a **chamber**: one experiment design or experience (hearing loss on one or both ears, moving sound sources, finding an invisible source, realistic rooms, outdoor scenes, ...). Features are built as portable modules (`Assets/GPL`) that can be copied into other projects.

Status: setup and architecture (proposed). See [docs/Architecture.md](docs/Architecture.md) and the open questions in [docs/Questions.md](docs/Questions.md).

## Requirements
- Unity **6000.6.6f1** (URP, OpenXR) with Android Build Support
- Meta XR Core SDK 207.0.1, Meta XR Audio SDK 85.0.0, Input System (see `Packages/manifest.json`)
- Quest 3, Meta Quest Developer Hub; Windows or macOS
- Git LFS: run `git lfs install` once, then `git lfs pull`
- Windows: enable **Developer Mode** (Settings > System > For developers) before cloning so `AGENTS.md` is checked out as a symlink

## Docs
- [CLAUDE.md](CLAUDE.md) — rules for AI agents (also `AGENTS.md`)
- [docs/Architecture.md](docs/Architecture.md), [docs/decisions/](docs/decisions/)
- [docs/ProductBacklog.md](docs/ProductBacklog.md), [docs/SprintBoard.md](docs/SprintBoard.md), [docs/PromptLog.md](docs/PromptLog.md)
- [docs/references/](docs/references/) — INSTRUCTIONS (process), HLSIM guidelines, study-brief example
- [Assets/Audio/README.md](Assets/Audio/README.md) — audio preparation (-23 LUFS mono)
