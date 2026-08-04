# LLM NPC Gateway

This folder contains a project-local FastAPI environment for the Unity NPC demo.

## Location

- Virtual environment: `gateway/.venv`
- Pip cache: `gateway/.pip-cache`
- Temporary install files: `gateway/.tmp`

## Run

```powershell
cd gateway
.\run_gateway.ps1
```

The Unity client calls:

```text
POST http://127.0.0.1:8787/npc/chat
```

Health check:

```text
GET http://127.0.0.1:8787/health
```
