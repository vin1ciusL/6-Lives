# AGENTS.md — 6 Lives (Unity)

## Shared work (multiple agents)
- `PROJECT_STATE.md` is a local coordination file, intentionally not versioned. When present, read it at task start, before editing files or operating the Editor, and before handoff. If absent, create it before concurrent agent work.
- Re-read target files before patching; another agent may have changed them.
- Follow its claims/handoff protocol. Reserve exact files and Editor use before work; one Editor operator at a time. Do not overwrite another agent's changes or interrupt their testing.
- Update `PROJECT_STATE.md` with changed behavior, validation, pending work, and released claims at milestones/end. Keep it short and factual; preserve other agents' entries.

## Editor operations
- Use `unity-cli` (the `unity` command) for all Unity Editor operations: scenes, GameObjects, assets, builds, tests.
- Before editing any scene/GameObject/asset, run `unity status` to confirm a connected Editor. If one is reachable, drive it live — never hand-edit `.unity`/`.prefab`/`.asset` YAML while an Editor is connected.
- Run commands from the repository root. Editor version: `6000.3.23f1` (see `ProjectSettings/ProjectVersion.txt`). Render pipeline: URP (`com.unity.render-pipelines.universal`). `com.unity.pipeline` package is installed.

## Workflow style
- For small changes: build what's asked, name smaller options, no unrequested scope. Follow `ponytail` at lite level when that local skill is available.
- Keep responses concise (caveman-style terseness where it doesn't hurt clarity).
- Default to one agent/session for a task — don't fan out subagents for trivial edits.
- No long upfront planning for small/trivial changes. Reserve plans for multi-step, non-trivial work.

## Before claiming done
- Validate changes before saying a task is complete: check compile state (`unity pipeline list` / `unity status`), re-query affected scene/asset state, or run relevant tests.
- Never claim success without evidence.
