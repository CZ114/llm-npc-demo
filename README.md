# Unity + LLM NPC quest demo

An unfinished but playable Unity 6 prototype: explore a small scene, talk to three NPCs, follow clues to a lost watch, pick it up, and return to complete the quest. The project demonstrates a controlled hand-off between language-generated dialogue and game-authoritative state.

## What the MVP contains

`PlayerInteractor` selects a nearby NPC and sends its identity, the player's message, quest state, and world facts to a local FastAPI gateway. The gateway can call the external Agent framework to produce in-character dialogue and propose an `update_quest_state` tool call. Unity's `QuestStateManager` validates each proposed transition; the LLM cannot directly change the game state. `WatchItem` supplies a physical pickup and state-gated interaction. If the Agent framework or API is unavailable, the gateway returns scripted dialogue and the same tool-call-shaped transition for an offline demonstration.

This is a **minimum demo, not a completed game or evaluated HCI study**. There is no user study, robust persistence, broad quest authoring system, or portable bundled Agent runtime. The repository documents a working local prototype; a fresh-machine Unity playtest has not yet been completed for this public release.

## Run locally

1. Open the repository in **Unity 6000.3.20f1**. Open `Assets/Scenes/NPCDemo.unity`.
2. Create a Python environment in `gateway/.venv`, install `gateway/requirements.txt`, then run `gateway/run_gateway.ps1` on Windows (or start `uvicorn npc_agent_gateway:app --app-dir gateway --host 127.0.0.1 --port 8787` in your environment).
3. Check `http://127.0.0.1:8787/health`, then press Play in Unity. The client uses `POST /npc/chat`.
4. Move with the included third-person controller. Approach an NPC and press **E** to interact; the work log describes the watch pickup and quest flow. The scripted fallback works without an API key.

For Agent-backed dialogue, provide a compatible installation of the separate Agent framework on Python's import path, or set `AGENT_FRAMEWORK_ROOT` to its project directory, and configure the framework's own model credentials locally. Never commit credentials. The external framework is **not** bundled here.

## Repository map and provenance

- `Assets/Scripts/`: project-specific Unity interaction, dialogue, quest and item code.
- `Assets/Scenes/NPCDemo.unity`: prototype scene.
- `gateway/`: FastAPI adapter and NPC/world/quest context.
- `docs/WORKLOG.md`: dated implementation and manual-playtest record, including what was written by the author and what was AI-assisted.
- `Assets/Starter Assets/`: Unity Technologies' character-controller package, used under the [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license). It is not original work by this repository's author.

The demo was developed with AI assistance. The work log distinguishes manual implementation and testing from assisted scaffolding and scene setup. No quantitative HCI or gameplay outcome is claimed.
